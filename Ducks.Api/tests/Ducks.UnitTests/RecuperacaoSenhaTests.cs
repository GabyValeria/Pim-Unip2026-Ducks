using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ducks.UnitTests;

public sealed class RecuperacaoSenhaTests
{
    private sealed class FilaFalsa : IFilaEmail
    {
        public List<MensagemEmail> Mensagens { get; } = [];
        public void Enfileirar(MensagemEmail mensagem) => Mensagens.Add(mensagem);
    }

    private static (RecuperacaoSenhaService Service, FilaFalsa Fila, PasswordHasher<Usuario> Hasher) Criar(DucksDbContext context)
    {
        var fila = new FilaFalsa();
        var hasher = new PasswordHasher<Usuario>();
        var config = new ConfiguracaoFalsa(new() { ["RecuperacaoSenha:UrlRedefinicao"] = "https://ducks.local/redefinir" });
        return (new RecuperacaoSenhaService(context, hasher, fila, config), fila, hasher);
    }

    /// <summary>IConfiguration mínima (o projeto de testes não referencia os pacotes de configuração).</summary>
    private sealed class ConfiguracaoFalsa(Dictionary<string, string?> valores) : IConfiguration
    {
        public string? this[string key] { get => valores.GetValueOrDefault(key); set => valores[key] = value; }
        public IEnumerable<IConfigurationSection> GetChildren() => [];
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => throw new NotSupportedException();
        public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
    }

    private static string TokenDoEmail(MensagemEmail m) => m.CorpoTexto.Split("token=")[1].Split('\n')[0].Trim();

    [Fact]
    public async Task Email_Desconhecido_Nao_Gera_Nada_E_Nao_Da_Erro()
    {
        await using var context = TestesBase.CriarContexto();
        var (service, fila, _) = Criar(context);
        await service.SolicitarAsync("ninguem@teste.com");
        Assert.Empty(fila.Mensagens);
        Assert.False(await context.TokensRecuperacaoSenha.AnyAsync());
    }

    [Fact]
    public async Task Token_Guarda_So_Hash_Troca_Senha_Uma_Vez_E_Encerra_Sessoes()
    {
        await using var context = TestesBase.CriarContexto();
        var (service, fila, hasher) = Criar(context);
        context.RefreshTokens.Add(new RefreshToken { Id = Guid.NewGuid(), UsuarioID = TestesBase.AlunoId, TokenHash = new string('A', 64), TokenFamilyId = Guid.NewGuid(), CriadoEm = DateTime.UtcNow, ExpiraEm = DateTime.UtcNow.AddDays(1) });
        await context.SaveChangesAsync();

        await service.SolicitarAsync(" aluno@teste.com ");
        var mensagem = Assert.Single(fila.Mensagens);
        Assert.Equal("aluno@teste.com", mensagem.Para);
        var token = TokenDoEmail(mensagem);
        var salvo = await context.TokensRecuperacaoSenha.SingleAsync();
        Assert.NotEqual(token, salvo.TokenHash);
        Assert.Equal(64, salvo.TokenHash.Length);
        Assert.True(salvo.ExpiraEm <= DateTime.UtcNow.AddMinutes(31));

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.RedefinirAsync("token-errado-com-mais-de-20-caracteres", "NovaSenha123"));
        await service.RedefinirAsync(token, "NovaSenha123");
        var usuario = await context.Usuarios.SingleAsync(u => u.UsuarioID == TestesBase.AlunoId);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, "NovaSenha123"));
        Assert.NotNull(usuario.SenhaAlteradaEm);
        Assert.All(await context.RefreshTokens.ToListAsync(), r => Assert.NotNull(r.RevogadoEm));
        Assert.True(await context.LogsAuditoria.AnyAsync(l => l.Acao == "SENHA_REDEFINIDA"));

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.RedefinirAsync(token, "OutraSenha123")); // uso único
    }

    [Fact]
    public async Task Novo_Pedido_Invalida_O_Anterior_Expirado_Nao_Vale_E_Ha_Limite_Por_Usuario()
    {
        await using var context = TestesBase.CriarContexto();
        var (service, fila, _) = Criar(context);
        await service.SolicitarAsync("aluno@teste.com");
        await service.SolicitarAsync("aluno@teste.com");
        var primeiro = TokenDoEmail(fila.Mensagens[0]);
        var segundo = TokenDoEmail(fila.Mensagens[1]);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.RedefinirAsync(primeiro, "NovaSenha123"));

        (await context.TokensRecuperacaoSenha.SingleAsync(t => t.UsadoEm == null)).ExpiraEm = DateTime.UtcNow.AddSeconds(-1);
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.RedefinirAsync(segundo, "NovaSenha123"));

        await service.SolicitarAsync("aluno@teste.com");
        await service.SolicitarAsync("aluno@teste.com"); // 4º pedido em 15 minutos: ignorado em silêncio
        Assert.Equal(3, fila.Mensagens.Count);
    }
}
