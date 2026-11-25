using Ducks.Api.Extensions;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

/// <summary>
/// Gestão de trilhas (detalhe/edição), módulos e aulas pelo Curador e pelo Admin.
/// Mostra itens em qualquer estado. Itens criados ou editados ficam em RASCUNHO até serem
/// enviados para revisão (POST .../enviar-revisao) e aprovados pela moderação.
/// </summary>
[Authorize(Policy = "ContentManagement")]
[ApiController]
[Route("api/admin/content")]
public sealed class AdminModulosAulasController(IConteudoService conteudo, IModeracaoService moderacao) : ControllerBase
{
    [HttpGet("trilhas/{trilhaId:int}")]
    public async Task<IActionResult> ObterTrilha(int trilhaId, CancellationToken ct) =>
        Ok(await conteudo.ObterTrilhaAsync(trilhaId, somenteAprovado: false, ct));

    [HttpPut("trilhas/{trilhaId:int}")]
    public async Task<IActionResult> AtualizarTrilha(int trilhaId, [FromBody] TrilhaRequestDto dto, CancellationToken ct)
    {
        await conteudo.AtualizarTrilhaAsync(trilhaId, dto, User.ObterUsuarioId(), ct);
        return NoContent();
    }

    [HttpPost("trilhas/{trilhaId:int}/enviar-revisao")]
    public async Task<IActionResult> EnviarTrilha(int trilhaId, CancellationToken ct) =>
        Ok(await moderacao.EnviarParaRevisaoAsync("TRILHA", trilhaId, User.ObterUsuarioId(), ct));

    // ------------------------------------------------------------ Módulos

    /// <summary>Lista os módulos da trilha. ordenarPor: indiceOrdem (padrão), titulo, status, atualizadoEm.</summary>
    [HttpGet("trilhas/{trilhaId:int}/modulos")]
    public async Task<IActionResult> ListarModulos(int trilhaId, [FromQuery] FiltroConteudoDto filtro, CancellationToken ct) =>
        Ok(await conteudo.ListarModulosAsync(trilhaId, filtro, somenteAprovado: false, ct));

    [HttpPost("trilhas/{trilhaId:int}/modulos")]
    public async Task<IActionResult> CriarModulo(int trilhaId, [FromBody] ModuloRequestDto dto, CancellationToken ct)
    {
        var modulo = await conteudo.CriarModuloAsync(trilhaId, dto, User.ObterUsuarioId(), ct);
        return CreatedAtAction(nameof(ObterModulo), new { moduloId = modulo.ModuloID }, modulo);
    }

    [HttpPut("trilhas/{trilhaId:int}/modulos/ordem")]
    public async Task<IActionResult> ReordenarModulos(int trilhaId, [FromBody] ReordenarDto dto, CancellationToken ct)
    {
        await conteudo.ReordenarModulosAsync(trilhaId, dto.Ids, User.ObterUsuarioId(), ct);
        return NoContent();
    }

    [HttpGet("modulos/{moduloId:int}")]
    public async Task<IActionResult> ObterModulo(int moduloId, CancellationToken ct) =>
        Ok(await conteudo.ObterModuloAsync(moduloId, somenteAprovado: false, ct));

    [HttpPut("modulos/{moduloId:int}")]
    public async Task<IActionResult> AtualizarModulo(int moduloId, [FromBody] ModuloRequestDto dto, CancellationToken ct) =>
        Ok(await conteudo.AtualizarModuloAsync(moduloId, dto, User.ObterUsuarioId(), ct));

    [HttpDelete("modulos/{moduloId:int}")]
    public async Task<IActionResult> ExcluirModulo(int moduloId, CancellationToken ct)
    {
        await conteudo.ExcluirModuloAsync(moduloId, User.ObterUsuarioId(), ct);
        return NoContent();
    }

    [HttpPost("modulos/{moduloId:int}/enviar-revisao")]
    public async Task<IActionResult> EnviarModulo(int moduloId, CancellationToken ct) =>
        Ok(await moderacao.EnviarParaRevisaoAsync("MODULO", moduloId, User.ObterUsuarioId(), ct));

    // ------------------------------------------------------------ Aulas

    /// <summary>Lista as aulas do módulo. ordenarPor: indiceOrdem (padrão), titulo, status, atualizadoEm.</summary>
    [HttpGet("modulos/{moduloId:int}/aulas")]
    public async Task<IActionResult> ListarAulas(int moduloId, [FromQuery] FiltroConteudoDto filtro, CancellationToken ct) =>
        Ok(await conteudo.ListarAulasAsync(moduloId, filtro, somenteAprovado: false, ct));

    [HttpPost("modulos/{moduloId:int}/aulas")]
    public async Task<IActionResult> CriarAula(int moduloId, [FromBody] AulaRequestDto dto, CancellationToken ct)
    {
        var aula = await conteudo.CriarAulaAsync(moduloId, dto, User.ObterUsuarioId(), ct);
        return CreatedAtAction(nameof(ObterAula), new { aulaId = aula.AulaID }, aula);
    }

    [HttpPut("modulos/{moduloId:int}/aulas/ordem")]
    public async Task<IActionResult> ReordenarAulas(int moduloId, [FromBody] ReordenarDto dto, CancellationToken ct)
    {
        await conteudo.ReordenarAulasAsync(moduloId, dto.Ids, User.ObterUsuarioId(), ct);
        return NoContent();
    }

    [HttpGet("aulas/{aulaId:int}")]
    public async Task<IActionResult> ObterAula(int aulaId, CancellationToken ct) =>
        Ok(await conteudo.ObterAulaAsync(aulaId, somenteAprovado: false, ct));

    [HttpPut("aulas/{aulaId:int}")]
    public async Task<IActionResult> AtualizarAula(int aulaId, [FromBody] AulaRequestDto dto, CancellationToken ct) =>
        Ok(await conteudo.AtualizarAulaAsync(aulaId, dto, User.ObterUsuarioId(), ct));

    [HttpDelete("aulas/{aulaId:int}")]
    public async Task<IActionResult> ExcluirAula(int aulaId, CancellationToken ct)
    {
        await conteudo.ExcluirAulaAsync(aulaId, User.ObterUsuarioId(), ct);
        return NoContent();
    }

    [HttpPost("aulas/{aulaId:int}/enviar-revisao")]
    public async Task<IActionResult> EnviarAula(int aulaId, CancellationToken ct) =>
        Ok(await moderacao.EnviarParaRevisaoAsync("AULA", aulaId, User.ObterUsuarioId(), ct));
}
