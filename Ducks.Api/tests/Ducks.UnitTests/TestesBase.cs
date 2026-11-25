using Ducks.Domain.Entities;
using Ducks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ducks.UnitTests;

/// <summary>Banco em memória e dados mínimos usados pelos testes das funcionalidades novas.</summary>
internal static class TestesBase
{
    public const int CuradorId = 1;
    public const int ModeradorId = 2;
    public const int AlunoId = 3;

    public static DucksDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<DucksDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new DucksDbContext(options);
        context.Usuarios.AddRange(
            new Usuario { UsuarioID = CuradorId, NomeUsuario = "Curador", Email = "curador@teste.com", SenhaHash = "x" },
            new Usuario { UsuarioID = ModeradorId, NomeUsuario = "Moderador", Email = "moderador@teste.com", SenhaHash = "x" },
            new Usuario { UsuarioID = AlunoId, NomeUsuario = "Aluno", Email = "aluno@teste.com", SenhaHash = "x" });
        context.SaveChanges();
        return context;
    }

    /// <summary>Trilha aprovada com 1 módulo aprovado e 2 aulas aprovadas.</summary>
    public static (TrilhaAprendizagem Trilha, Modulo Modulo, Aula Aula1, Aula Aula2) CriarTrilhaAprovada(DucksDbContext context, string nome = "Lógica")
    {
        var trilha = new TrilhaAprendizagem { NomeTrilha = nome, Descricao = "d", MateriaID = 1, Status = StatusConteudo.Aprovado };
        context.TrilhasAprendizagem.Add(trilha);
        context.SaveChanges();
        var modulo = new Modulo { TrilhaID = trilha.TrilhaID, Titulo = "Módulo 1", IndiceOrdem = 1, Status = StatusConteudo.Aprovado };
        context.Modulos.Add(modulo);
        context.SaveChanges();
        var a1 = new Aula { ModuloID = modulo.ModuloID, Titulo = "Aula 1", UrlConteudo = "https://x/1", IndiceOrdem = 1, Status = StatusConteudo.Aprovado };
        var a2 = new Aula { ModuloID = modulo.ModuloID, Titulo = "Aula 2", UrlConteudo = "https://x/2", IndiceOrdem = 2, Status = StatusConteudo.Aprovado };
        context.Aulas.AddRange(a1, a2);
        context.SaveChanges();
        return (trilha, modulo, a1, a2);
    }
}
