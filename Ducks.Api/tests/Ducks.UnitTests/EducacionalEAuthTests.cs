using Ducks.Domain.Entities;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ducks.UnitTests;

public class EducacionalEAuthTests
{
    private DucksDbContext ObterContextoEmMemoria()
    {
        var options = new DbContextOptionsBuilder<DucksDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new DucksDbContext(options);
    }

    [Fact]
    public void PasswordHasher_Deve_Gerar_Hash_Valido_E_Verificar_Corretamente()
    {
        // Arrange
        var usuario = new Usuario { UsuarioID = 1, Email = "teste@ducks.com", NomeUsuario = "Teste" };
        var passwordHasher = new PasswordHasher<Usuario>();
        var senhaPura = "SenhaForte123!";

        // Act
        var hash = passwordHasher.HashPassword(usuario, senhaPura);
        var resultado = passwordHasher.VerifyHashedPassword(usuario, hash, senhaPura);

        // Assert
        Assert.False(string.IsNullOrEmpty(hash));
        Assert.Equal(PasswordVerificationResult.Success, resultado);
    }

    [Fact]
    public async Task EducacionalService_Deve_Adicionar_XP_Corretamente_Ao_Usuario()
    {
        // Arrange
        using var context = ObterContextoEmMemoria();
        var servico = new EducacionalService(context);
        int usuarioId = 10;

        // Act
        await servico.AdicionarXPAsync(usuarioId, 50, "Conclusão da Aula 1");
        await servico.AdicionarXPAsync(usuarioId, 30, "Resolução de Prova");

        var xpUsuario = await servico.ObterXPUsuarioAsync(usuarioId);

        // Assert
        Assert.NotNull(xpUsuario);
        Assert.Equal(80, xpUsuario.TotalXP);
        
        var historicoCount = await context.HistoricoXPs.CountAsync(h => h.UsuarioID == usuarioId);
        Equal(2, historicoCount); // Confirma registros na tabela de histórico sem ranking
    }


    [Fact]
    public async Task EducacionalService_Deve_Rejeitar_XP_Zero()
    {
        using var context = ObterContextoEmMemoria();
        var servico = new EducacionalService(context);

        await Assert.ThrowsAsync<Ducks.Domain.Exceptions.BusinessRuleException>(
            () => servico.AdicionarXPAsync(10, 0, "Ajuste inválido"));
    }

    private static void Equal(int esperado, int atual) => Assert.Equal(esperado, atual);
}