using System.Net.Http.Json;
using System.Net;

namespace Ducks.IntegrationTests;

[Collection("api")]
public sealed class ConteudoEModeracaoApiTests(ApiFixture api)
{
    /// <summary>Cria uma trilha aprovada pelo caminho real: curador cria, envia e o moderador aprova.</summary>
    internal static async Task<int> CriarTrilhaAprovadaAsync(HttpClient curador, HttpClient moderador, string nome)
    {
        await curador.PostJsonAsync("/api/admin/content/trilhas", new { NomeTrilha = nome, Descricao = "Trilha de teste", MateriaID = 1 }, HttpStatusCode.Created);
        var trilhas = await curador.GetJsonAsync("/api/admin/content/trilhas");
        var trilhaId = trilhas.EnumerateArray().Last(t => t.Str("nomeTrilha") == nome).Int("trilhaID");
        // A rota antiga cria a trilha já PENDENTE (em revisão): o moderador aprova direto.
        await moderador.PostJsonAsync($"/api/moderacao/trilha/{trilhaId}/aprovar", null);
        return trilhaId;
    }

    internal static async Task<(int ModuloId, int[] AulaIds)> CriarModuloAprovadoAsync(HttpClient curador, HttpClient moderador, int trilhaId, int aulas)
    {
        var modulo = await curador.PostJsonAsync($"/api/admin/content/trilhas/{trilhaId}/modulos", new { Titulo = "Módulo " + Guid.NewGuid().ToString("N")[..4] }, HttpStatusCode.Created);
        var moduloId = modulo.Int("moduloID");
        var ids = new List<int>();
        for (var i = 1; i <= aulas; i++)
        {
            var aula = await curador.PostJsonAsync($"/api/admin/content/modulos/{moduloId}/aulas", new { Titulo = $"Aula {i}", UrlConteudo = $"https://ducks.local/aula/{i}" }, HttpStatusCode.Created);
            ids.Add(aula.Int("aulaID"));
            await curador.PostJsonAsync($"/api/admin/content/aulas/{ids[^1]}/enviar-revisao", null);
            await moderador.PostJsonAsync($"/api/moderacao/aula/{ids[^1]}/aprovar", null);
        }
        await curador.PostJsonAsync($"/api/admin/content/modulos/{moduloId}/enviar-revisao", null);
        await moderador.PostJsonAsync($"/api/moderacao/modulo/{moduloId}/aprovar", null);
        return (moduloId, ids.ToArray());
    }

    [FactSql]
    public async Task Curador_Gerencia_Modulos_E_Aulas_E_Estudante_So_Ve_Aprovado()
    {
        var curador = api.Cliente(await api.CriarUsuarioAsync("Curador"), "Curador");
        var moderador = api.Cliente(await api.CriarUsuarioAsync("Moderador"), "Moderador");
        var aluno = api.Cliente(await api.CriarUsuarioAsync("Estudante"), "Estudante");
        var trilhaId = await CriarTrilhaAprovadaAsync(curador, moderador, "Trilha Conteúdo " + Guid.NewGuid().ToString("N")[..4]);
        var (moduloId, aulas) = await CriarModuloAprovadoAsync(curador, moderador, trilhaId, 2);

        // Rascunho: o curador vê, o estudante não.
        var rascunho = await curador.PostJsonAsync($"/api/admin/content/modulos/{moduloId}/aulas", new { Titulo = "Aula rascunho", Conteudo = "texto", IndiceOrdem = 1 }, HttpStatusCode.Created);
        var listaCurador = await curador.GetJsonAsync($"/api/admin/content/modulos/{moduloId}/aulas?pageSize=10");
        Assert.Equal(3, listaCurador.Int("total"));
        Assert.Equal(rascunho.Int("aulaID"), listaCurador.GetProperty("items")[0].Int("aulaID")); // entrou na posição 1
        var listaAluno = await aluno.GetJsonAsync($"/api/educacional/modulos/{moduloId}/aulas");
        Assert.Equal(2, listaAluno.Int("total"));
        await aluno.GetJsonAsync($"/api/educacional/aulas/{rascunho.Int("aulaID")}", HttpStatusCode.NotFound);

        var trilha = await aluno.GetJsonAsync($"/api/educacional/trilhas/{trilhaId}");
        Assert.Equal(2, trilha.GetProperty("modulos")[0].GetProperty("aulas").GetArrayLength());

        // Reordenar e excluir.
        await (await curador.PutAsJsonAsyncNoBody($"/api/admin/content/modulos/{moduloId}/aulas/ordem", new { Ids = new[] { aulas[1], aulas[0], rascunho.Int("aulaID") } })).JsonAsync(HttpStatusCode.NoContent);
        var reordenada = await curador.GetJsonAsync($"/api/admin/content/modulos/{moduloId}");
        Assert.Equal(aulas[1], reordenada.GetProperty("aulas")[0].Int("aulaID"));
        await (await curador.DeleteAsync($"/api/admin/content/aulas/{rascunho.Int("aulaID")}")).JsonAsync(HttpStatusCode.NoContent);

        // Validação de entrada: título curto demais.
        await curador.PostJsonAsync($"/api/admin/content/trilhas/{trilhaId}/modulos", new { Titulo = "x" }, HttpStatusCode.BadRequest);
        Assert.True(Convert.ToInt32(await api.ConsultarAsync("SELECT COUNT(*) FROM dbo.LogsAuditoria WHERE Acao IN ('AULA_CRIADA','AULAS_REORDENADAS','AULA_EXCLUIDA')")) >= 3);
    }

    [FactSql]
    public async Task Permissoes_Por_Papel_Nas_Rotas_De_Conteudo_E_Moderacao()
    {
        var aluno = api.Cliente(await api.CriarUsuarioAsync("Estudante"), "Estudante");
        var curador = api.Cliente(await api.CriarUsuarioAsync("Curador"), "Curador");
        var moderador = api.Cliente(await api.CriarUsuarioAsync("Moderador"), "Moderador");
        var semToken = api.Factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await semToken.GetAsync("/api/educacional/trilhas/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await aluno.PostAsync("/api/admin/content/trilhas/1/modulos", JsonContent.Create(new { Titulo = "Teste" }))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await moderador.PostAsync("/api/admin/content/trilhas/1/modulos", JsonContent.Create(new { Titulo = "Teste" }))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await aluno.GetAsync("/api/moderacao/pendentes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await curador.GetAsync("/api/moderacao/pendentes")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await moderador.GetAsync("/api/moderacao/pendentes")).StatusCode);
    }

    [FactSql]
    public async Task Rejeicao_Exige_Justificativa_E_Fica_No_Historico_E_Na_Auditoria()
    {
        var curador = api.Cliente(await api.CriarUsuarioAsync("Curador"), "Curador");
        var moderadorId = await api.CriarUsuarioAsync("Moderador");
        var moderador = api.Cliente(moderadorId, "Moderador");
        var trilhaId = await CriarTrilhaAprovadaAsync(curador, moderador, "Trilha Revisão " + Guid.NewGuid().ToString("N")[..4]);
        var modulo = await curador.PostJsonAsync($"/api/admin/content/trilhas/{trilhaId}/modulos", new { Titulo = "Módulo para revisar" }, HttpStatusCode.Created);
        var id = modulo.Int("moduloID");

        await curador.PostJsonAsync($"/api/admin/content/modulos/{id}/enviar-revisao", null);
        var pendentes = await moderador.GetJsonAsync("/api/moderacao/pendentes?tipo=modulo&pageSize=100");
        Assert.Contains(pendentes.GetProperty("items").EnumerateArray(), i => i.Int("conteudoID") == id);

        await moderador.PostJsonAsync($"/api/moderacao/modulo/{id}/rejeitar", new { Justificativa = "" }, HttpStatusCode.BadRequest);
        var decisao = await moderador.PostJsonAsync($"/api/moderacao/modulo/{id}/rejeitar", new { Justificativa = "Descrição incompleta" });
        Assert.Equal("REJEITADO", decisao.Str("statusNovo"));
        await moderador.PostJsonAsync($"/api/moderacao/modulo/{id}/aprovar", null, HttpStatusCode.Conflict);

        var item = await moderador.GetJsonAsync($"/api/moderacao/modulo/{id}");
        Assert.Equal("REJEITADO", item.Str("status"));
        Assert.Equal("Descrição incompleta", item.GetProperty("historico")[0].Str("justificativa"));
        Assert.Equal(1, Convert.ToInt32(await api.ConsultarAsync($"SELECT COUNT(*) FROM dbo.LogsAuditoria WHERE Acao = 'CONTEUDO_REJEITADO' AND UsuarioID = {moderadorId}")));
    }
}

internal static class HttpPutExtensions
{
    public static Task<HttpResponseMessage> PutAsJsonAsyncNoBody(this HttpClient client, string url, object body) =>
        client.PutAsync(url, JsonContent.Create(body));
}
