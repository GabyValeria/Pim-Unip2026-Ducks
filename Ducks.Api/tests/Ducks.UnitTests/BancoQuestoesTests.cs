using Ducks.Application.DTOs;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Ducks.UnitTests;

public sealed class BancoQuestoesTests
{
    internal static QuestaoRequestDto Questao(int? aulaId = null, int? trilhaId = null, string dificuldade = "MEDIO", params string[] tags) => new()
    {
        Enunciado = "Quanto é 2 + 2? " + Guid.NewGuid().ToString("N")[..6],
        Tipo = "MULTIPLA_ESCOLHA",
        Dificuldade = dificuldade,
        AulaID = aulaId,
        TrilhaID = trilhaId,
        Tags = tags.ToList(),
        Explicacao = "Soma simples.",
        Alternativas =
        [
            new AlternativaRequestDto { Texto = "3", Explicacao = "Faltou 1." },
            new AlternativaRequestDto { Texto = "4", Correta = true, Explicacao = "2 + 2 = 4." },
            new AlternativaRequestDto { Texto = "5", Explicacao = "Passou 1." }
        ]
    };

    /// <summary>Cria e aprova (direto no banco) N questões ligadas à trilha.</summary>
    internal static async Task<List<int>> CriarQuestoesAprovadasAsync(DucksDbContext context, BancoQuestoesService banco, int trilhaId, int quantidade, string dificuldade = "MEDIO")
    {
        var ids = new List<int>();
        for (var i = 0; i < quantidade; i++)
            ids.Add((await banco.CriarQuestaoAsync(Questao(trilhaId: trilhaId, dificuldade: dificuldade), TestesBase.CuradorId)).QuestaoID);
        foreach (var q in await context.Questoes.Where(q => ids.Contains(q.QuestaoID)).ToListAsync()) q.Status = StatusConteudo.Aprovado;
        await context.SaveChangesAsync();
        return ids;
    }

    [Fact]
    public async Task Questao_Valida_Gabarito_E_Completa_Vinculos_Pela_Aula()
    {
        await using var context = TestesBase.CriarContexto();
        var (trilha, modulo, aula, _) = TestesBase.CriarTrilhaAprovada(context);
        var banco = new BancoQuestoesService(context);

        var duasCorretas = Questao(aulaId: aula.AulaID);
        duasCorretas.Alternativas[0].Correta = true;
        await Assert.ThrowsAsync<BusinessRuleException>(() => banco.CriarQuestaoAsync(duasCorretas, TestesBase.CuradorId));
        var vf = Questao(aulaId: aula.AulaID);
        vf.Tipo = "VERDADEIRO_FALSO";
        await Assert.ThrowsAsync<BusinessRuleException>(() => banco.CriarQuestaoAsync(vf, TestesBase.CuradorId));
        var semVinculo = Questao();
        await Assert.ThrowsAsync<BusinessRuleException>(() => banco.CriarQuestaoAsync(semVinculo, TestesBase.CuradorId));

        var criada = await banco.CriarQuestaoAsync(Questao(aula.AulaID, null, "FACIL", "Matemática", "soma"), TestesBase.CuradorId);
        Assert.Equal(modulo.ModuloID, criada.ModuloID);
        Assert.Equal(trilha.TrilhaID, criada.TrilhaID);
        Assert.Equal(trilha.MateriaID, criada.MateriaID);
        Assert.Equal(StatusConteudo.Rascunho, criada.Status);
        Assert.Equal(["matemática", "soma"], criada.Tags);
        Assert.Equal("2 + 2 = 4.", criada.Alternativas.Single(a => a.Correta).Explicacao);

        var porTag = await banco.ListarQuestoesAsync(new FiltroQuestoesDto { Tag = "SOMA" });
        Assert.Equal(1, porTag.Total);
        Assert.Equal(0, (await banco.ListarQuestoesAsync(new FiltroQuestoesDto { Tag = "som" })).Total); // tag exata
        Assert.Equal(1, (await banco.ListarQuestoesAsync(new FiltroQuestoesDto { Dificuldade = "facil" })).Total);
    }

    [Fact]
    public async Task Sorteio_Usa_So_Questoes_Aprovadas_E_Respeita_Filtros()
    {
        await using var context = TestesBase.CriarContexto();
        var (trilha, _, _, _) = TestesBase.CriarTrilhaAprovada(context);
        var banco = new BancoQuestoesService(context);
        var faceis = await CriarQuestoesAprovadasAsync(context, banco, trilha.TrilhaID, 4, "FACIL");
        await CriarQuestoesAprovadasAsync(context, banco, trilha.TrilhaID, 2, "DIFICIL");
        await banco.CriarQuestaoAsync(Questao(trilhaId: trilha.TrilhaID, dificuldade: "FACIL"), TestesBase.CuradorId); // rascunho: não entra

        var simulado = await banco.CriarSimuladoAsync(new SimuladoRequestDto { Titulo = "Simulado fácil", TrilhaID = trilha.TrilhaID, NumeroQuestoes = 3 }, TestesBase.CuradorId);
        Assert.Equal(TiposProva.Especifica, simulado.TipoProva);
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            banco.SortearQuestoesAsync(simulado.ProvaID, new SortearQuestoesDto { Quantidade = 5, Dificuldade = "FACIL" }, TestesBase.CuradorId));

        var sorteado = await banco.SortearQuestoesAsync(simulado.ProvaID, new SortearQuestoesDto { Dificuldade = "FACIL" }, TestesBase.CuradorId);
        Assert.Equal(3, sorteado.Questoes.Count);
        Assert.All(sorteado.Questoes, q => Assert.Contains(q.QuestaoID, faceis));
        Assert.Equal([1, 2, 3], sorteado.Questoes.Select(q => q.Ordem).ToArray());

        var manual = await banco.DefinirQuestoesAsync(simulado.ProvaID, [faceis[3], faceis[0]], TestesBase.CuradorId);
        Assert.Equal([faceis[3], faceis[0]], manual.Questoes.Select(q => q.QuestaoID).ToArray());
        Assert.Equal(2, manual.NumeroQuestoes);
    }

    [Fact]
    public async Task Simulado_So_E_Aprovado_Com_Questoes_Aprovadas_E_So_Entao_Aparece_Ao_Estudante()
    {
        await using var context = TestesBase.CriarContexto();
        var (trilha, _, _, _) = TestesBase.CriarTrilhaAprovada(context);
        var banco = new BancoQuestoesService(context);
        var moderacao = new ModeracaoService(context, new ConteudoService(context));
        var rascunho = await banco.CriarQuestaoAsync(Questao(trilhaId: trilha.TrilhaID), TestesBase.CuradorId);
        var simulado = await banco.CriarSimuladoAsync(new SimuladoRequestDto { Titulo = "Simulado 1", TrilhaID = trilha.TrilhaID }, TestesBase.CuradorId);

        await Assert.ThrowsAsync<BusinessRuleException>(() => moderacao.EnviarParaRevisaoAsync("simulado", simulado.ProvaID, TestesBase.CuradorId)); // sem questões
        await banco.DefinirQuestoesAsync(simulado.ProvaID, [rascunho.QuestaoID], TestesBase.CuradorId);
        await moderacao.EnviarParaRevisaoAsync("simulado", simulado.ProvaID, TestesBase.CuradorId);
        await Assert.ThrowsAsync<BusinessRuleException>(() => moderacao.AprovarAsync("simulado", simulado.ProvaID, TestesBase.ModeradorId, null));

        await moderacao.EnviarParaRevisaoAsync("questao", rascunho.QuestaoID, TestesBase.CuradorId);
        await moderacao.AprovarAsync("questao", rascunho.QuestaoID, TestesBase.ModeradorId, null);
        Assert.Equal(0, (await banco.ListarSimuladosDisponiveisAsync(TestesBase.AlunoId, new FiltroSimuladosDto())).Total);
        await moderacao.AprovarAsync("simulado", simulado.ProvaID, TestesBase.ModeradorId, null);

        var disponiveis = await banco.ListarSimuladosDisponiveisAsync(TestesBase.AlunoId, new FiltroSimuladosDto());
        Assert.Equal(1, disponiveis.Total);
        Assert.Equal(1, disponiveis.Items[0].NumeroQuestoes);
    }
}
