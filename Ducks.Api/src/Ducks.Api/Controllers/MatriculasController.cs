using Ducks.Api.Extensions;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

/// <summary>Matrícula, conclusão de aulas, progresso, histórico e dashboard do usuário autenticado.</summary>
[Authorize]
[ApiController]
[Route("api/educacional")]
public sealed class MatriculasController(IMatriculaService matriculas) : ControllerBase
{
    /// <summary>Matricula na trilha aprovada. 201 na primeira vez; 200 se já estava matriculado (não duplica).</summary>
    [HttpPost("trilhas/{trilhaId:int}/matricula")]
    public async Task<IActionResult> Matricular(int trilhaId, CancellationToken ct)
    {
        var resultado = await matriculas.MatricularAsync(User.ObterUsuarioId(), trilhaId, ct);
        return resultado.Criada ? StatusCode(StatusCodes.Status201Created, resultado.Matricula) : Ok(resultado.Matricula);
    }

    [HttpDelete("trilhas/{trilhaId:int}/matricula")]
    public async Task<IActionResult> Cancelar(int trilhaId, CancellationToken ct)
    {
        await matriculas.CancelarAsync(User.ObterUsuarioId(), trilhaId, ct);
        return NoContent();
    }

    /// <summary>Minhas matrículas. Filtro status; ordenarPor: inscritoEm (padrão, mais recente primeiro), nome, progresso.</summary>
    [HttpGet("matriculas")]
    public async Task<IActionResult> Listar([FromQuery] FiltroMatriculasDto filtro, CancellationToken ct) =>
        Ok(await matriculas.ListarAsync(User.ObterUsuarioId(), filtro, ct));

    /// <summary>Registra a conclusão da aula (idempotente: repetir não gera XP de novo).</summary>
    [HttpPost("aulas/{aulaId:int}/concluir")]
    public async Task<IActionResult> ConcluirAula(int aulaId, CancellationToken ct) =>
        Ok(await matriculas.ConcluirAulaAsync(User.ObterUsuarioId(), aulaId, ct));

    /// <summary>Percentual da trilha e de cada módulo, com as aulas e simulados concluídos.</summary>
    [HttpGet("trilhas/{trilhaId:int}/progresso")]
    public async Task<IActionResult> Progresso(int trilhaId, CancellationToken ct) =>
        Ok(await matriculas.ObterProgressoAsync(User.ObterUsuarioId(), trilhaId, ct));

    /// <summary>Histórico de atividades. Filtros: tipo, de, ate; ordem asc|desc (padrão desc).</summary>
    [HttpGet("historico")]
    public async Task<IActionResult> Historico([FromQuery] FiltroHistoricoDto filtro, CancellationToken ct) =>
        Ok(await matriculas.ListarHistoricoAsync(User.ObterUsuarioId(), filtro, ct));

    /// <summary>Resumo para o dashboard do estudante, com recomendações.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct) =>
        Ok(await matriculas.ObterDashboardAsync(User.ObterUsuarioId(), ct));
}
