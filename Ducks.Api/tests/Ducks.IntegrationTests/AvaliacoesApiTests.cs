using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Ducks.IntegrationTests;

[Collection("api")]
public sealed class AvaliacoesApiTests(ApiFixture api)
{
    private static object Questao(int trilhaId, int n) => new
    {
        Enunciado = $"Questão {n}: qual é a resposta certa?",
        Tipo = "MULTIPLA_ESCOLHA",
        Dificuldade = n % 2 == 0 ? "FACIL" : "MEDIO",
        TrilhaID = trilhaId,
        Tags = new[] { "logica" },
        Explicacao = "Explicação geral.",
        Alternativas = new object[]
        {
            new { Texto = "Errada A", Correta = false, Explicacao = "A está errada porque..." },
            new { Texto = "Certa", Correta = true, Explicacao = "Esta é a correta porque..." },
            new { Texto = "Errada B", Correta = false }
        }
    };

    [FactSql]
    public async Task Curador_Cria_Questoes_E_Simulado_Moderador_Aprova_E_Estudante_Faz_A_Prova()
    {
        var curador = api.Cliente(await api.CriarUsuarioAsync("Curador"), "Curador");
        var moderador = api.Cliente(await api.CriarUsuarioAsync("Moderador"), "Moderador");
        var alunoId = await api.CriarUsuarioAsync("Estudante");
        var aluno = api.Cliente(alunoId, "Estudante");
        var trilhaId = await ConteudoEModeracaoApiTests.CriarTrilhaAprovadaAsync(curador, moderador, "Trilha Prova " + Guid.NewGuid().ToString("N")[..4]);

        // Questões: criar, enviar e aprovar.
        await curador.PostJsonAsync("/api/admin/content/questoes", new { Enunciado = "Sem vínculo nenhum", Tipo = "MULTIPLA_ESCOLHA", Alternativas = new[] { new { Texto = "a", Correta = true }, new { Texto = "b", Correta = false } } }, HttpStatusCode.BadRequest);
        var questoes = new List<int>();
        for (var i = 1; i <= 3; i++)
        {
            var q = await curador.PostJsonAsync("/api/admin/content/questoes", Questao(trilhaId, i), HttpStatusCode.Created);
            questoes.Add(q.Int("questaoID"));
            await curador.PostJsonAsync($"/api/admin/content/questoes/{questoes[^1]}/enviar-revisao", null);
            await moderador.PostJsonAsync($"/api/moderacao/questao/{questoes[^1]}/aprovar", null);
        }
        var filtradas = await curador.GetJsonAsync($"/api/admin/content/questoes?trilhaId={trilhaId}&tag=logica&status=APROVADO");
        Assert.Equal(3, filtradas.Int("total"));

        // Simulado com sorteio e regras.
        var simulado = await curador.PostJsonAsync("/api/admin/content/simulados", new { Titulo = "Simulado integração", TrilhaID = trilhaId, NumeroQuestoes = 3, MaxTentativas = 1, TempoLimiteMinutos = 30, RecompensaXP = 30, NotaMinimaAprovacao = 60 }, HttpStatusCode.Created);
        var provaId = simulado.Int("provaID");
        Assert.Equal("ESPECIFICA", simulado.Str("tipoProva"));
        var sorteado = await curador.PostJsonAsync($"/api/admin/content/simulados/{provaId}/questoes/sortear", new { });
        Assert.Equal(3, sorteado.GetProperty("questoes").GetArrayLength());
        await curador.PostJsonAsync($"/api/admin/content/simulados/{provaId}/enviar-revisao", null);
        await aluno.GetJsonAsync($"/api/educacional/simulados/{provaId}", HttpStatusCode.NotFound); // ainda em revisão
        await moderador.PostJsonAsync($"/api/moderacao/simulado/{provaId}/aprovar", null);

        // Estudante: consulta sem gabarito.
        var disponivel = await aluno.GetJsonAsync($"/api/educacional/simulados/{provaId}");
        Assert.Equal(3, disponivel.Int("numeroQuestoes"));
        Assert.Equal(1, disponivel.Int("tentativasRestantes"));
        var tentativa = await aluno.PostJsonAsync($"/api/educacional/simulados/{provaId}/tentativas", null, HttpStatusCode.Created);
        var texto = tentativa.GetRawText();
        Assert.DoesNotContain("correta", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("explicacao", texto, StringComparison.OrdinalIgnoreCase);
        var tentativaId = tentativa.Int("provaUsuarioID");

        // Responde: 2 certas e 1 errada.
        var respostas = new List<object>();
        var i2 = 0;
        foreach (var q in tentativa.GetProperty("questoes").EnumerateArray())
        {
            var alternativas = q.GetProperty("alternativas").EnumerateArray().ToList();
            var escolhida = alternativas.First(a => (i2 < 2) == (a.Str("texto") == "Certa"));
            respostas.Add(new { QuestaoID = q.Int("questaoID"), AlternativaID = escolhida.Int("alternativaID") });
            i2++;
        }
        var registro = await aluno.PutJsonAsync($"/api/educacional/tentativas/{tentativaId}/respostas", new { Respostas = respostas });
        Assert.Equal(3, registro.Int("respondidas"));

        var outroAluno = api.Cliente(await api.CriarUsuarioAsync("Estudante"), "Estudante");
        await outroAluno.GetJsonAsync($"/api/educacional/tentativas/{tentativaId}", HttpStatusCode.NotFound);

        var final = await aluno.PostJsonAsync($"/api/educacional/tentativas/{tentativaId}/finalizar", null);
        var resultado = final.GetProperty("resultado");
        Assert.Equal(2, resultado.Int("acertos"));
        Assert.Equal(66.67m, resultado.GetProperty("notaPercentual").GetDecimal());
        Assert.True(resultado.GetProperty("aprovado").GetBoolean());
        Assert.Equal(20, resultado.Int("xpGanho"));
        var erro = resultado.GetProperty("feedback").EnumerateArray().Single(f => !f.GetProperty("correta").GetBoolean());
        Assert.Equal("A está errada porque...", erro.Str("explicacaoAlternativaSelecionada"));
        Assert.Equal("Esta é a correta porque...", erro.Str("explicacaoAlternativaCorreta"));

        // Sem reenvio e sem nova tentativa (limite 1).
        await aluno.PostJsonAsync($"/api/educacional/tentativas/{tentativaId}/finalizar", null, HttpStatusCode.Conflict);
        await aluno.PutJsonAsync($"/api/educacional/tentativas/{tentativaId}/respostas", new { Respostas = respostas }, HttpStatusCode.Conflict);
        await aluno.PostJsonAsync($"/api/educacional/simulados/{provaId}/tentativas", null, HttpStatusCode.Conflict);

        var historico = await aluno.GetJsonAsync("/api/educacional/tentativas");
        Assert.Equal(1, historico.Int("total"));
        Assert.Equal(20, (await aluno.GetJsonAsync("/api/educacional/xp/meu")).Int("totalXP"));

        // Questão respondida não muda de gabarito nem é excluída.
        await (await curador.DeleteAsync($"/api/admin/content/questoes/{questoes[0]}")).JsonAsync(HttpStatusCode.Conflict);
        await (await curador.DeleteAsync($"/api/admin/content/simulados/{provaId}")).JsonAsync(HttpStatusCode.Conflict);
        Assert.Equal(1, Convert.ToInt32(await api.ConsultarAsync($"SELECT COUNT(*) FROM dbo.ProvasUsuario WHERE UsuarioID = {alunoId} AND Status = 'CONCLUIDA'")));
    }
}
