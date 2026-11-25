using Ducks.Application.DTOs;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Ducks.UnitTests;

public sealed class TentativaTests
{
    private sealed record Cenario(DucksDbContext Context, TentativaService Tentativas, BancoQuestoesService Banco, MatriculaService Matriculas,
        TrilhaAprendizagem Trilha, int ProvaId, List<int> Questoes);

    /// <summary>Simulado aprovado com 4 questões aprovadas, ligado ao módulo da trilha.</summary>
    private static async Task<Cenario> CriarAsync(int? maxTentativas = null, int? tempo = null, int recompensa = 40)
    {
        var context = TestesBase.CriarContexto();
        var (trilha, modulo, _, _) = TestesBase.CriarTrilhaAprovada(context);
        var banco = new BancoQuestoesService(context);
        var questoes = await BancoQuestoesTests.CriarQuestoesAprovadasAsync(context, banco, trilha.TrilhaID, 4);
        var simulado = await banco.CriarSimuladoAsync(new SimuladoRequestDto
        {
            Titulo = "Simulado", ModuloID = modulo.ModuloID, MaxTentativas = maxTentativas, TempoLimiteMinutos = tempo,
            RecompensaXP = recompensa, NotaMinimaAprovacao = 75
        }, TestesBase.CuradorId);
        await banco.DefinirQuestoesAsync(simulado.ProvaID, questoes, TestesBase.CuradorId);
        (await context.Provas.SingleAsync()).Status = StatusConteudo.Aprovado;
        await context.SaveChangesAsync();
        var educacional = new EducacionalService(context);
        var matriculas = new MatriculaService(context, educacional);
        return new Cenario(context, new TentativaService(context, educacional, matriculas), banco, matriculas, trilha, simulado.ProvaID, questoes);
    }

    private static async Task<List<RespostaItemDto>> RespostasAsync(DucksDbContext context, IEnumerable<int> questoes, int corretas)
    {
        var lista = new List<RespostaItemDto>();
        var i = 0;
        foreach (var q in questoes)
        {
            var deveAcertar = i++ < corretas;
            var alt = await context.AlternativasQuestao.Where(a => a.QuestaoID == q && a.EhCorreta == deveAcertar).OrderBy(a => a.AlternativaID).Select(a => a.AlternativaID).FirstAsync();
            lista.Add(new RespostaItemDto { QuestaoID = q, AlternativaID = alt });
        }
        return lista;
    }

    [Fact]
    public async Task Fluxo_Completo_Sem_Gabarito_Antes_E_Com_Feedback_Depois()
    {
        var c = await CriarAsync();
        await using var _ = c.Context;
        var inicio = await c.Tentativas.IniciarAsync(TestesBase.AlunoId, c.ProvaId);
        Assert.True(inicio.Criada);
        Assert.Null(inicio.Tentativa.Resultado);
        Assert.Equal(4, inicio.Tentativa.Questoes.Count);
        Assert.False((await c.Tentativas.IniciarAsync(TestesBase.AlunoId, c.ProvaId)).Criada); // devolve a mesma
        var id = inicio.Tentativa.ProvaUsuarioID;

        // Outro usuário não enxerga a tentativa.
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => c.Tentativas.ObterAsync(TestesBase.CuradorId, id));
        // Alternativa de outra questão é recusada.
        var outra = await c.Context.AlternativasQuestao.Where(a => a.QuestaoID == c.Questoes[1]).Select(a => a.AlternativaID).FirstAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => c.Tentativas.ResponderAsync(TestesBase.AlunoId, id, [new RespostaItemDto { QuestaoID = c.Questoes[0], AlternativaID = outra }]));

        // Responde 3 de 4 (2 certas, 1 errada), depois troca uma errada por certa.
        var respostas = await RespostasAsync(c.Context, c.Questoes.Take(3), 2);
        await c.Tentativas.ResponderAsync(TestesBase.AlunoId, id, respostas);
        var parcial = await c.Tentativas.ObterAsync(TestesBase.AlunoId, id);
        Assert.Equal(3, parcial.Respondidas);
        Assert.Null(parcial.Resultado);

        var resultado = (await c.Tentativas.FinalizarAsync(TestesBase.AlunoId, id)).Resultado!;
        Assert.Equal(2, resultado.Acertos);
        Assert.Equal(1, resultado.Erros);
        Assert.Equal(1, resultado.EmBranco);
        Assert.Equal(50m, resultado.NotaPercentual);
        Assert.False(resultado.Aprovado);
        Assert.Equal(20, resultado.XPGanho); // 50% de 40
        var errada = resultado.Feedback.Single(f => f.AlternativaSelecionadaID != null && !f.Correta);
        Assert.Equal("Faltou 1.", errada.ExplicacaoAlternativaSelecionada);
        Assert.Equal("2 + 2 = 4.", errada.ExplicacaoAlternativaCorreta);

        await Assert.ThrowsAsync<ConflictException>(() => c.Tentativas.FinalizarAsync(TestesBase.AlunoId, id));
        await Assert.ThrowsAsync<ConflictException>(() => c.Tentativas.ResponderAsync(TestesBase.AlunoId, id, respostas));
    }

    [Fact]
    public async Task Refazer_So_Rende_A_Diferenca_De_XP_E_Respeita_Limite_De_Tentativas()
    {
        var c = await CriarAsync(maxTentativas: 2, recompensa: 40);
        await using var _ = c.Context;
        var t1 = (await c.Tentativas.IniciarAsync(TestesBase.AlunoId, c.ProvaId)).Tentativa.ProvaUsuarioID;
        await c.Tentativas.ResponderAsync(TestesBase.AlunoId, t1, await RespostasAsync(c.Context, c.Questoes, 2));
        Assert.Equal(20, (await c.Tentativas.FinalizarAsync(TestesBase.AlunoId, t1)).Resultado!.XPGanho);

        var t2 = (await c.Tentativas.IniciarAsync(TestesBase.AlunoId, c.ProvaId)).Tentativa.ProvaUsuarioID;
        await c.Tentativas.ResponderAsync(TestesBase.AlunoId, t2, await RespostasAsync(c.Context, c.Questoes, 4));
        var r2 = (await c.Tentativas.FinalizarAsync(TestesBase.AlunoId, t2)).Resultado!;
        Assert.Equal(100m, r2.NotaPercentual);
        Assert.Equal(20, r2.XPGanho); // 40 no total, nunca mais que isso
        Assert.Equal(40, (await c.Context.XPUsuarios.AsNoTracking().SingleAsync(x => x.UsuarioID == TestesBase.AlunoId)).TotalXP);

        await Assert.ThrowsAsync<ConflictException>(() => c.Tentativas.IniciarAsync(TestesBase.AlunoId, c.ProvaId));
        var historico = await c.Tentativas.ListarAsync(TestesBase.AlunoId, new FiltroTentativasDto { ProvaID = c.ProvaId });
        Assert.Equal(2, historico.Total);
        Assert.True(historico.Items[0].Aprovado); // mais recente primeiro
    }

    [Fact]
    public async Task Tempo_Esgotado_Finaliza_Sozinho_E_Bloqueia_Respostas()
    {
        var c = await CriarAsync(tempo: 5);
        await using var _ = c.Context;
        var t = (await c.Tentativas.IniciarAsync(TestesBase.AlunoId, c.ProvaId)).Tentativa;
        Assert.NotNull(t.TempoRestanteSegundos);
        await c.Tentativas.ResponderAsync(TestesBase.AlunoId, t.ProvaUsuarioID, await RespostasAsync(c.Context, c.Questoes.Take(1), 1));

        var entidade = await c.Context.ProvasUsuario.SingleAsync();
        entidade.PrazoEm = DateTime.UtcNow.AddMinutes(-1);
        await c.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            c.Tentativas.ResponderAsync(TestesBase.AlunoId, t.ProvaUsuarioID, [new RespostaItemDto { QuestaoID = c.Questoes[1], AlternativaID = 1 }]));
        var final = await c.Tentativas.ObterAsync(TestesBase.AlunoId, t.ProvaUsuarioID);
        Assert.Equal(StatusTentativa.Finalizada, final.Status);
        Assert.True(final.Resultado!.TempoEsgotado);
        Assert.Equal(1, final.Resultado.Acertos);
    }

    [Fact]
    public async Task Simulado_Aprovado_Conta_No_Progresso_Da_Trilha()
    {
        var c = await CriarAsync();
        await using var _ = c.Context;
        await c.Matriculas.MatricularAsync(TestesBase.AlunoId, c.Trilha.TrilhaID);
        var progresso = await c.Matriculas.ObterProgressoAsync(TestesBase.AlunoId, c.Trilha.TrilhaID);
        Assert.Equal(3, progresso.TotalItens); // 2 aulas + 1 simulado do módulo

        var t = (await c.Tentativas.IniciarAsync(TestesBase.AlunoId, c.ProvaId)).Tentativa.ProvaUsuarioID;
        await c.Tentativas.ResponderAsync(TestesBase.AlunoId, t, await RespostasAsync(c.Context, c.Questoes, 4));
        await c.Tentativas.FinalizarAsync(TestesBase.AlunoId, t);
        progresso = await c.Matriculas.ObterProgressoAsync(TestesBase.AlunoId, c.Trilha.TrilhaID);
        Assert.Equal(33.33m, progresso.Percentual);
        Assert.True(progresso.Modulos[0].Simulados[0].Aprovado);
    }
}
