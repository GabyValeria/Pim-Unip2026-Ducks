using Ducks.Application.DTOs;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Ducks.UnitTests;

public sealed class MatriculaTests
{
    private static MatriculaService Servico(Ducks.Infrastructure.Persistence.DucksDbContext c) => new(c, new EducacionalService(c));

    [Fact]
    public async Task Matricula_E_Idempotente_E_So_Aceita_Trilha_Aprovada()
    {
        await using var context = TestesBase.CriarContexto();
        var (trilha, _, _, _) = TestesBase.CriarTrilhaAprovada(context);
        var pendente = new TrilhaAprendizagem { NomeTrilha = "Pendente", MateriaID = 1, Status = StatusConteudo.EmRevisao };
        context.TrilhasAprendizagem.Add(pendente);
        await context.SaveChangesAsync();
        var service = Servico(context);

        Assert.True((await service.MatricularAsync(TestesBase.AlunoId, trilha.TrilhaID)).Criada);
        Assert.False((await service.MatricularAsync(TestesBase.AlunoId, trilha.TrilhaID)).Criada);
        Assert.Equal(1, await context.UsuarioTrilhas.CountAsync());
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.MatricularAsync(TestesBase.AlunoId, pendente.TrilhaID));

        await service.CancelarAsync(TestesBase.AlunoId, trilha.TrilhaID);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.CancelarAsync(TestesBase.AlunoId, trilha.TrilhaID));
        Assert.True((await service.MatricularAsync(TestesBase.AlunoId, trilha.TrilhaID)).Criada); // reativa a mesma matrícula
        Assert.Equal(1, await context.UsuarioTrilhas.CountAsync());
    }

    [Fact]
    public async Task Concluir_Aulas_Calcula_Progresso_E_Conclui_Trilha_Com_XP_Uma_Vez()
    {
        await using var context = TestesBase.CriarContexto();
        var (trilha, modulo, a1, a2) = TestesBase.CriarTrilhaAprovada(context);
        context.Conquistas.Add(new Conquista { Nome = "Primeiros passos", Descricao = "100 XP", XPNecessario = 100, Ativa = true });
        await context.SaveChangesAsync();
        var service = Servico(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ConcluirAulaAsync(TestesBase.AlunoId, a1.AulaID)); // sem matrícula
        await service.MatricularAsync(TestesBase.AlunoId, trilha.TrilhaID);

        var r1 = await service.ConcluirAulaAsync(TestesBase.AlunoId, a1.AulaID);
        Assert.Equal(10, r1.XPGanho);
        Assert.Equal(50m, r1.ProgressoTrilha);
        Assert.False(r1.TrilhaConcluida);

        var repetida = await service.ConcluirAulaAsync(TestesBase.AlunoId, a1.AulaID);
        Assert.True(repetida.JaConcluida);
        Assert.Equal(0, repetida.XPGanho);

        var r2 = await service.ConcluirAulaAsync(TestesBase.AlunoId, a2.AulaID);
        Assert.True(r2.TrilhaConcluida);
        Assert.Equal(100, r2.XPConclusaoTrilha);
        Assert.Equal(100m, r2.ProgressoModulo);

        var xp = await context.XPUsuarios.SingleAsync(x => x.UsuarioID == TestesBase.AlunoId);
        Assert.Equal(120, xp.TotalXP); // 10 + 10 + 100
        Assert.True(await context.UsuariosConquistas.AnyAsync(u => u.UsuarioID == TestesBase.AlunoId)); // medalha por marco de XP
        var matricula = await context.UsuarioTrilhas.SingleAsync();
        Assert.Equal(StatusMatricula.Concluida, matricula.Status);

        // Nova aula aprovada depois da conclusão: concluir de novo não paga a trilha outra vez.
        var a3 = new Aula { ModuloID = modulo.ModuloID, Titulo = "Aula 3", UrlConteudo = "u", IndiceOrdem = 3, Status = StatusConteudo.Aprovado };
        context.Aulas.Add(a3);
        await context.SaveChangesAsync();
        var r3 = await service.ConcluirAulaAsync(TestesBase.AlunoId, a3.AulaID);
        Assert.Equal(0, r3.XPConclusaoTrilha);
        Assert.Equal(130, (await context.XPUsuarios.AsNoTracking().SingleAsync(x => x.UsuarioID == TestesBase.AlunoId)).TotalXP);
        Assert.Equal(1, await context.RecompensasConcedidas.CountAsync(r => r.TipoOrigem == OrigemRecompensa.Trilha));
    }

    [Fact]
    public async Task Progresso_Ignora_Conteudo_Nao_Aprovado_E_Dashboard_Recomenda_Proxima_Aula()
    {
        await using var context = TestesBase.CriarContexto();
        var (trilha, modulo, a1, a2) = TestesBase.CriarTrilhaAprovada(context);
        context.Aulas.Add(new Aula { ModuloID = modulo.ModuloID, Titulo = "Rascunho", UrlConteudo = "u", IndiceOrdem = 3, Status = StatusConteudo.Rascunho });
        await context.SaveChangesAsync();
        var service = Servico(context);
        await service.MatricularAsync(TestesBase.AlunoId, trilha.TrilhaID);
        await service.ConcluirAulaAsync(TestesBase.AlunoId, a1.AulaID);

        var progresso = await service.ObterProgressoAsync(TestesBase.AlunoId, trilha.TrilhaID);
        Assert.Equal(2, progresso.TotalItens);
        Assert.Equal(50m, progresso.Percentual);

        var dashboard = await service.ObterDashboardAsync(TestesBase.AlunoId);
        Assert.Equal(1, dashboard.TrilhasEmAndamento);
        Assert.Equal(1, dashboard.AulasConcluidas);
        Assert.Contains(dashboard.Recomendacoes, r => r.Tipo == "AULA" && r.ReferenciaID == a2.AulaID);
        Assert.Single((await service.ListarHistoricoAsync(TestesBase.AlunoId, new FiltroHistoricoDto())).Items);
    }
}
