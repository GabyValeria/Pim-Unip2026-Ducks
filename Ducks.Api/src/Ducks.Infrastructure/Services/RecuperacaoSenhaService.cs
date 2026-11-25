using System.Security.Cryptography;
using System.Text;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services.Comum;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ducks.Infrastructure.Services;

/// <summary>
/// Token aleatório de 32 bytes, enviado só por e-mail; no banco fica apenas o SHA-256 (como nos refresh tokens).
/// Validade curta (padrão 30 min), uso único, e cada novo pedido invalida os anteriores.
/// </summary>
public class RecuperacaoSenhaService(
    DucksDbContext context,
    IPasswordHasher<Usuario> passwordHasher,
    IFilaEmail filaEmail,
    IConfiguration configuration) : IRecuperacaoSenhaService
{
    /// <summary>Máximo de pedidos por usuário a cada 15 minutos (além do limite por IP na rota).</summary>
    private const int PedidosPorJanela = 3;

    public async Task SolicitarAsync(string email, CancellationToken ct = default)
    {
        var normalizado = email.Trim();
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.Email == normalizado && u.EstaAtivo, ct);
        if (usuario is null) return; // mesma resposta para quem chamou

        var agora = DateTime.UtcNow;
        var recentes = await context.TokensRecuperacaoSenha.CountAsync(t => t.UsuarioID == usuario.UsuarioID && t.CriadoEm > agora.AddMinutes(-15), ct);
        if (recentes >= PedidosPorJanela) return;

        foreach (var antigo in await context.TokensRecuperacaoSenha.Where(t => t.UsuarioID == usuario.UsuarioID && t.UsadoEm == null).ToListAsync(ct))
            antigo.UsadoEm = agora;

        var tokenBruto = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var minutos = Math.Clamp(configuration.LerInt("RecuperacaoSenha:ExpiracaoMinutos", 30), 5, 120);
        context.TokensRecuperacaoSenha.Add(new TokenRecuperacaoSenha
        {
            UsuarioID = usuario.UsuarioID, TokenHash = Hash(tokenBruto), CriadoEm = agora, ExpiraEm = agora.AddMinutes(minutos)
        });
        context.RegistrarAuditoria(usuario.UsuarioID, "SENHA_RECUPERACAO_SOLICITADA", "Pedido de recuperação de senha");
        await context.SaveChangesAsync(ct);

        var url = configuration["RecuperacaoSenha:UrlRedefinicao"] ?? "http://localhost:3000/redefinir-senha";
        var link = $"{url}{(url.Contains('?') ? '&' : '?')}token={Uri.EscapeDataString(tokenBruto)}";
        filaEmail.Enfileirar(new MensagemEmail(usuario.Email, "Ducks - redefinição de senha",
            $"Olá, {usuario.NomeUsuario}!\n\nRecebemos um pedido para redefinir a sua senha no Ducks.\n" +
            $"Use o link abaixo em até {minutos} minutos (ele só funciona uma vez):\n{link}\n\n" +
            "Se não foi você, ignore este e-mail: a sua senha continua a mesma."));
    }

    public async Task RedefinirAsync(string token, string novaSenha, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new BusinessRuleException("Token inválido ou expirado.");
        if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length is < 8 or > 128)
            throw new BusinessRuleException("A nova senha deve ter de 8 a 128 caracteres.");
        var agora = DateTime.UtcNow;
        var hash = Hash(token.Trim());
        var registro = await context.TokensRecuperacaoSenha.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (registro is null || registro.UsadoEm is not null || registro.ExpiraEm <= agora)
            throw new BusinessRuleException("Token inválido ou expirado. Solicite uma nova recuperação de senha.");
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioID == registro.UsuarioID && u.EstaAtivo, ct)
            ?? throw new BusinessRuleException("Token inválido ou expirado. Solicite uma nova recuperação de senha.");

        usuario.SenhaHash = passwordHasher.HashPassword(usuario, novaSenha);
        usuario.AtualizadoEm = agora;
        // Tokens de acesso emitidos antes deste instante deixam de ser aceitos (ver OnTokenValidated no Program.cs).
        usuario.SenhaAlteradaEm = new DateTime(agora.Ticks - agora.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

        foreach (var t in await context.TokensRecuperacaoSenha.Where(t => t.UsuarioID == usuario.UsuarioID && t.UsadoEm == null).ToListAsync(ct))
            t.UsadoEm = agora;
        foreach (var refresh in await context.RefreshTokens.Where(r => r.UsuarioID == usuario.UsuarioID && r.RevogadoEm == null).ToListAsync(ct))
            refresh.RevogadoEm = agora;
        context.RegistrarAuditoria(usuario.UsuarioID, "SENHA_REDEFINIDA", "Senha redefinida por token; sessões anteriores encerradas");
        await context.SaveChangesAsync(ct);
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
