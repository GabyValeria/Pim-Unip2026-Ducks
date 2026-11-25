using Ducks.Api.Extensions;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

/// <summary>Simulados disponíveis para o estudante (sem gabarito).</summary>
[Authorize]
[ApiController]
[Route("api/educacional")]
public sealed class ProvasController(IBancoQuestoesService banco) : ControllerBase
{
    /// <summary>Simulados aprovados. Filtros: trilhaId, moduloId, busca. ordenarPor: criadoEm (padrão), titulo.</summary>
    [HttpGet("simulados")]
    public async Task<IActionResult> ListarSimulados([FromQuery] FiltroSimuladosDto filtro, CancellationToken ct) =>
        Ok(await banco.ListarSimuladosDisponiveisAsync(User.ObterUsuarioId(), filtro, ct));

    [HttpGet("simulados/{provaId:int}")]
    public async Task<IActionResult> ObterSimulado(int provaId, CancellationToken ct) =>
        Ok(await banco.ObterSimuladoDisponivelAsync(User.ObterUsuarioId(), provaId, ct));
}
