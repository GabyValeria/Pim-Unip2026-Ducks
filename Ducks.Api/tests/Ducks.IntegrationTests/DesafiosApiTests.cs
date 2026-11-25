using System.Net;

namespace Ducks.IntegrationTests;

[Collection("api")]
public sealed class DesafiosApiTests(ApiFixture api)
{
    [FactSql]
    public async Task Admin_Configura_Desafio_E_Estudante_Participa_E_Conclui()
    {
        var admin = api.Cliente(await api.CriarUsuarioAsync("Administrador"), "Administrador");
        var alunoId = await api.CriarUsuarioAsync("Estudante");
        var aluno = api.Cliente(alunoId, "Estudante");

        var medalha = await admin.PostJsonAsync("/api/admin/gamification/badges", new { Nome = "Desafiante " + Guid.NewGuid().ToString("N")[..4], Descricao = "Concluiu um desafio", XPNecessario = 999999, Ativa = true });
        await admin.PostJsonAsync("/api/admin/gamification/challenges", new { Nome = "Inválido", TipoRequisito = "QUALQUER", MetaRequisito = 1 }, HttpStatusCode.BadRequest);
        var desafio = await admin.PostJsonAsync("/api/admin/gamification/challenges", new
        {
            Nome = "Desafio da semana", Descricao = "Junte 10 XP", RecompensaXP = 25, Ativo = true,
            InicioEm = DateTime.UtcNow.AddHours(-1), FimEm = DateTime.UtcNow.AddDays(3),
            TipoRequisito = "XP_TOTAL", MetaRequisito = 10, ConquistaID = medalha.Int("conquistaID")
        });
        var id = desafio.Int("desafioID");

        var lista = await aluno.GetJsonAsync("/api/educacional/desafios");
        Assert.Contains(lista.GetProperty("items").EnumerateArray(), d => d.Int("desafioID") == id);
        await aluno.PostJsonAsync($"/api/educacional/desafios/{id}/participar", null, HttpStatusCode.Created);
        await aluno.PostJsonAsync($"/api/educacional/desafios/{id}/participar", null, HttpStatusCode.Conflict);
        await aluno.PostJsonAsync($"/api/educacional/desafios/{id}/concluir", null, HttpStatusCode.BadRequest); // 0 de 10 XP

        await admin.PostJsonAsync("/api/educacional/xp/adicionar", new { UsuarioID = alunoId, QuantidadeXP = 10, Motivo = "Bônus de teste" }, HttpStatusCode.NoContent);
        var progresso = await aluno.GetJsonAsync($"/api/educacional/desafios/{id}/progresso");
        Assert.True(progresso.GetProperty("podeConcluir").GetBoolean());
        var conclusao = await aluno.PostJsonAsync($"/api/educacional/desafios/{id}/concluir", null);
        Assert.Equal(25, conclusao.Int("xpGanho"));
        Assert.Equal(35, (await aluno.GetJsonAsync("/api/educacional/xp/meu")).Int("totalXP"));

        var medalhas = await admin.GetJsonAsync($"/api/admin/gamification/users/{alunoId}/badges");
        Assert.Contains(medalhas.EnumerateArray(), m => m.Int("conquistaID") == medalha.Int("conquistaID"));
        Assert.Equal(1, Convert.ToInt32(await api.ConsultarAsync($"SELECT COUNT(*) FROM dbo.LogsAuditoria WHERE UsuarioID = {alunoId} AND Acao = 'DESAFIO_CONCLUIDO'")));
    }
}
