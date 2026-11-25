using Ducks.Application.DTOs;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Ducks.UnitTests;

public sealed class DesafioTests
{
    [Fact]
    public async Task Participar_Uma_Vez_Concluir_Com_Requisito_E_Ganhar_XP_E_Medalha()
    {
        await using var context = TestesBase.CriarContexto();
        var (trilha, _, a1, a2) = TestesBase.CriarTrilhaAprovada(context);
        var medalha = new Conquista { Nome = "Maratonista", Descricao = "Concluiu o desafio", XPNecessario = 99999, Ativa = true };
        context.Conquistas.Add(medalha);
        await context.SaveChangesAsync();
        var desafio = new Desafio { Nome = "Duas aulas", Descricao = "Conclua 2 aulas", RecompensaXP = 50, Ativo = true,
            TipoRequisito = TiposRequisitoDesafio.AulasConcluidas, MetaRequisito = 2, TrilhaID = trilha.TrilhaID, ConquistaID = medalha.ConquistaID,
            InicioEm = DateTime.UtcNow.AddDays(-1), FimEm = DateTime.UtcNow.AddDays(7) };
        context.Desafios.Add(desafio);
        await context.SaveChangesAsync();
        var educacional = new EducacionalService(context);
        var service = new DesafioService(context, educacional);
        var matriculas = new MatriculaService(context, educacional);

        Assert.Equal(1, (await service.ListarAsync(TestesBase.AlunoId, new FiltroDesafiosDto())).Total);
        var progresso = await service.ParticiparAsync(TestesBase.AlunoId, desafio.DesafioID);
        Assert.Equal(0, progresso.Atual);
        await Assert.ThrowsAsync<ConflictException>(() => service.ParticiparAsync(TestesBase.AlunoId, desafio.DesafioID));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ConcluirAsync(TestesBase.AlunoId, desafio.DesafioID));

        await matriculas.MatricularAsync(TestesBase.AlunoId, trilha.TrilhaID);
        await matriculas.ConcluirAulaAsync(TestesBase.AlunoId, a1.AulaID);
        Assert.Equal(50m, (await service.ObterProgressoAsync(TestesBase.AlunoId, desafio.DesafioID)).Percentual);
        await matriculas.ConcluirAulaAsync(TestesBase.AlunoId, a2.AulaID); // também conclui a trilha (+100)
        var xpAntes = (await context.XPUsuarios.AsNoTracking().SingleAsync(x => x.UsuarioID == TestesBase.AlunoId)).TotalXP;

        var conclusao = await service.ConcluirAsync(TestesBase.AlunoId, desafio.DesafioID);
        Assert.Equal(50, conclusao.XPGanho);
        Assert.Equal("Maratonista", conclusao.NomeConquista);
        Assert.Equal(xpAntes + 50, (await context.XPUsuarios.AsNoTracking().SingleAsync(x => x.UsuarioID == TestesBase.AlunoId)).TotalXP);
        Assert.True(await context.UsuariosConquistas.AnyAsync(u => u.UsuarioID == TestesBase.AlunoId && u.ConquistaID == medalha.ConquistaID));
        await Assert.ThrowsAsync<ConflictException>(() => service.ConcluirAsync(TestesBase.AlunoId, desafio.DesafioID));
        Assert.Equal(1, (await service.ListarAsync(TestesBase.AlunoId, new FiltroDesafiosDto { Situacao = "concluidos" })).Total);
    }

    [Fact]
    public async Task Fora_Do_Periodo_Ou_Inativo_Nao_Aceita_Participacao()
    {
        await using var context = TestesBase.CriarContexto();
        var encerrado = new Desafio { Nome = "Encerrado", Ativo = true, FimEm = DateTime.UtcNow.AddDays(-1) };
        var futuro = new Desafio { Nome = "Futuro", Ativo = true, InicioEm = DateTime.UtcNow.AddDays(1) };
        var inativo = new Desafio { Nome = "Inativo", Ativo = false };
        var livre = new Desafio { Nome = "Livre", Ativo = true, RecompensaXP = 5 };
        context.Desafios.AddRange(encerrado, futuro, inativo, livre);
        await context.SaveChangesAsync();
        var service = new DesafioService(context, new EducacionalService(context));

        foreach (var d in new[] { encerrado, futuro, inativo })
            await Assert.ThrowsAsync<BusinessRuleException>(() => service.ParticiparAsync(TestesBase.AlunoId, d.DesafioID));
        Assert.Equal(1, (await service.ListarAsync(TestesBase.AlunoId, new FiltroDesafiosDto())).Total);

        // Sem requisito: pode concluir logo depois de entrar.
        await service.ParticiparAsync(TestesBase.AlunoId, livre.DesafioID);
        Assert.Equal(5, (await service.ConcluirAsync(TestesBase.AlunoId, livre.DesafioID)).XPGanho);
    }
}
