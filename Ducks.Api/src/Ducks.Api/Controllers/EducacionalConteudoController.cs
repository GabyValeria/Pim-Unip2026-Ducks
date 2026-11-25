using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

/// <summary>
/// Catálogo para estudantes (e consulta de qualquer papel): só trilhas, módulos e aulas APROVADOS.
/// Item não aprovado responde 404, como se não existisse.
/// </summary>
[Authorize]
[ApiController]
[Route("api/educacional")]
public sealed class EducacionalConteudoController(IConteudoService conteudo) : ControllerBase
{
    [HttpGet("trilhas/{trilhaId:int}")]
    public async Task<IActionResult> ObterTrilha(int trilhaId, CancellationToken ct) =>
        Ok(await conteudo.ObterTrilhaAsync(trilhaId, somenteAprovado: true, ct));

    [HttpGet("trilhas/{trilhaId:int}/modulos")]
    public async Task<IActionResult> ListarModulos(int trilhaId, [FromQuery] FiltroConteudoDto filtro, CancellationToken ct) =>
        Ok(await conteudo.ListarModulosAsync(trilhaId, filtro, somenteAprovado: true, ct));

    [HttpGet("modulos/{moduloId:int}")]
    public async Task<IActionResult> ObterModulo(int moduloId, CancellationToken ct) =>
        Ok(await conteudo.ObterModuloAsync(moduloId, somenteAprovado: true, ct));

    [HttpGet("modulos/{moduloId:int}/aulas")]
    public async Task<IActionResult> ListarAulas(int moduloId, [FromQuery] FiltroConteudoDto filtro, CancellationToken ct) =>
        Ok(await conteudo.ListarAulasAsync(moduloId, filtro, somenteAprovado: true, ct));

    [HttpGet("aulas/{aulaId:int}")]
    public async Task<IActionResult> ObterAula(int aulaId, CancellationToken ct) =>
        Ok(await conteudo.ObterAulaAsync(aulaId, somenteAprovado: true, ct));
}
