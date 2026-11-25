using Ducks.Api.Extensions;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

/// <summary>Tentativas de simulado do usuário autenticado.</summary>
[Authorize]
[ApiController]
[Route("api/educacional")]
public sealed class TentativasController(ITentativaService tentativas) : ControllerBase
{
    /// <summary>Inicia uma tentativa (201). Se já houver uma em andamento, devolve ela (200).</summary>
    [HttpPost("simulados/{provaId:int}/tentativas")]
    public async Task<IActionResult> Iniciar(int provaId, CancellationToken ct)
    {
        var inicio = await tentativas.IniciarAsync(User.ObterUsuarioId(), provaId, ct);
        return inicio.Criada
            ? CreatedAtAction(nameof(Obter), new { tentativaId = inicio.Tentativa.ProvaUsuarioID }, inicio.Tentativa)
            : Ok(inicio.Tentativa);
    }

    /// <summary>Histórico de tentativas e resultados. Filtros: provaId, status. ordenarPor: iniciadoEm (padrão, recentes primeiro), nota.</summary>
    [HttpGet("tentativas")]
    public async Task<IActionResult> Listar([FromQuery] FiltroTentativasDto filtro, CancellationToken ct) =>
        Ok(await tentativas.ListarAsync(User.ObterUsuarioId(), filtro, ct));

    /// <summary>Em andamento: questões sem gabarito. Finalizada: nota e feedback por questão.</summary>
    [HttpGet("tentativas/{tentativaId:int}")]
    public async Task<IActionResult> Obter(int tentativaId, CancellationToken ct) =>
        Ok(await tentativas.ObterAsync(User.ObterUsuarioId(), tentativaId, ct));

    [HttpPut("tentativas/{tentativaId:int}/respostas")]
    public async Task<IActionResult> Responder(int tentativaId, [FromBody] ResponderDto dto, CancellationToken ct) =>
        Ok(await tentativas.ResponderAsync(User.ObterUsuarioId(), tentativaId, dto.Respostas, ct));

    /// <summary>Finaliza e corrige. Depois disso a tentativa não aceita mais respostas (409).</summary>
    [HttpPost("tentativas/{tentativaId:int}/finalizar")]
    public async Task<IActionResult> Finalizar(int tentativaId, CancellationToken ct) =>
        Ok(await tentativas.FinalizarAsync(User.ObterUsuarioId(), tentativaId, ct));
}
