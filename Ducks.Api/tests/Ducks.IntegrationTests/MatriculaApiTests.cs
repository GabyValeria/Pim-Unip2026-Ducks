using System.Net;

namespace Ducks.IntegrationTests;

[Collection("api")]
public sealed class MatriculaApiTests(ApiFixture api)
{
    [FactSql]
    public async Task Estudante_Se_Matricula_Conclui_Aulas_E_Ganha_XP_Uma_Vez()
    {
        var curador = api.Cliente(await api.CriarUsuarioAsync("Curador"), "Curador");
        var moderador = api.Cliente(await api.CriarUsuarioAsync("Moderador"), "Moderador");
        var alunoId = await api.CriarUsuarioAsync("Estudante");
        var aluno = api.Cliente(alunoId, "Estudante");
        var trilhaId = await ConteudoEModeracaoApiTests.CriarTrilhaAprovadaAsync(curador, moderador, "Trilha Matrícula " + Guid.NewGuid().ToString("N")[..4]);
        var (_, aulas) = await ConteudoEModeracaoApiTests.CriarModuloAprovadoAsync(curador, moderador, trilhaId, 2);

        await aluno.PostJsonAsync($"/api/educacional/aulas/{aulas[0]}/concluir", null, HttpStatusCode.BadRequest); // sem matrícula
        var matricula = await aluno.PostJsonAsync($"/api/educacional/trilhas/{trilhaId}/matricula", null, HttpStatusCode.Created);
        Assert.Equal("EM_ANDAMENTO", matricula.Str("status"));
        await aluno.PostJsonAsync($"/api/educacional/trilhas/{trilhaId}/matricula", null, HttpStatusCode.OK); // idempotente

        var r1 = await aluno.PostJsonAsync($"/api/educacional/aulas/{aulas[0]}/concluir", null);
        Assert.Equal(10, r1.Int("xpGanho"));
        Assert.Equal(50m, r1.GetProperty("progressoTrilha").GetDecimal());
        var repetida = await aluno.PostJsonAsync($"/api/educacional/aulas/{aulas[0]}/concluir", null);
        Assert.True(repetida.GetProperty("jaConcluida").GetBoolean());
        var r2 = await aluno.PostJsonAsync($"/api/educacional/aulas/{aulas[1]}/concluir", null);
        Assert.True(r2.GetProperty("trilhaConcluida").GetBoolean());

        var xp = await aluno.GetJsonAsync("/api/educacional/xp/meu");
        Assert.Equal(120, xp.Int("totalXP"));
        var progresso = await aluno.GetJsonAsync($"/api/educacional/trilhas/{trilhaId}/progresso");
        Assert.Equal("CONCLUIDA", progresso.Str("statusMatricula"));
        Assert.Equal(100m, progresso.GetProperty("modulos")[0].GetProperty("percentual").GetDecimal());

        var lista = await aluno.GetJsonAsync("/api/educacional/matriculas?status=CONCLUIDA");
        Assert.Equal(1, lista.Int("total"));
        var historico = await aluno.GetJsonAsync("/api/educacional/historico?pageSize=10");
        Assert.Equal(3, historico.Int("total")); // 2 aulas + trilha
        var dashboard = await aluno.GetJsonAsync("/api/educacional/dashboard");
        Assert.Equal(1, dashboard.Int("trilhasConcluidas"));
        Assert.Equal(120, dashboard.Int("totalXP"));

        await (await aluno.DeleteAsync($"/api/educacional/trilhas/{trilhaId}/matricula")).JsonAsync(HttpStatusCode.Conflict);
        Assert.Equal(2, Convert.ToInt32(await api.ConsultarAsync($"SELECT COUNT(*) FROM dbo.HistoricoXP WHERE UsuarioID = {alunoId} AND QuantidadeXP = 10")));
        Assert.Equal(1, Convert.ToInt32(await api.ConsultarAsync($"SELECT COUNT(*) FROM dbo.LogsAuditoria WHERE UsuarioID = {alunoId} AND Acao = 'TRILHA_CONCLUIDA'")));
    }

    [FactSql]
    public async Task Cancelar_Matricula_E_Trilha_Nao_Aprovada()
    {
        var curador = api.Cliente(await api.CriarUsuarioAsync("Curador"), "Curador");
        var moderador = api.Cliente(await api.CriarUsuarioAsync("Moderador"), "Moderador");
        var aluno = api.Cliente(await api.CriarUsuarioAsync("Estudante"), "Estudante");
        var trilhaId = await ConteudoEModeracaoApiTests.CriarTrilhaAprovadaAsync(curador, moderador, "Trilha Cancelar " + Guid.NewGuid().ToString("N")[..4]);
        await ConteudoEModeracaoApiTests.CriarModuloAprovadoAsync(curador, moderador, trilhaId, 1);

        await aluno.PostJsonAsync($"/api/educacional/trilhas/{trilhaId}/matricula", null, HttpStatusCode.Created);
        await (await aluno.DeleteAsync($"/api/educacional/trilhas/{trilhaId}/matricula")).JsonAsync(HttpStatusCode.NoContent);
        await (await aluno.DeleteAsync($"/api/educacional/trilhas/{trilhaId}/matricula")).JsonAsync(HttpStatusCode.NotFound);
        var canceladas = await aluno.GetJsonAsync("/api/educacional/matriculas?status=CANCELADA");
        Assert.Equal(1, canceladas.Int("total"));

        await curador.PostJsonAsync("/api/admin/content/trilhas", new { NomeTrilha = "Trilha pendente", Descricao = "x", MateriaID = 1 }, HttpStatusCode.Created);
        var pendenteId = Convert.ToInt32(await api.ConsultarAsync("SELECT MAX(TrilhaID) FROM dbo.TrilhasAprendizagem WHERE Status = 'PENDENTE'"));
        await aluno.PostJsonAsync($"/api/educacional/trilhas/{pendenteId}/matricula", null, HttpStatusCode.NotFound);
    }
}
