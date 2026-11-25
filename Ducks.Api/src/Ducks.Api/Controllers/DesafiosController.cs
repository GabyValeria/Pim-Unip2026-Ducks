using Ducks.Api.Extensions;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

/// <summary>Desafios do ponto de vista do estudante (o cadastro continua em api/admin/gamification/challenges).</summary>
[Authorize]
[ApiController]
[Route("api/educacional/desafios")]
public sealed class DesafiosController(IDesafioService desafios) : ControllerBase
{
    /// <summary>situacao: disponiveis (padrão), participando, concluidos, todos. ordenarPor: fimEm (padrão), nome, recompensa.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] FiltroDesafiosDto filtro, CancellationToken ct) =>
        Ok(await desafios.ListarAsync(User.ObterUsuarioId(), filtro, ct));

    /// <summary>Inscreve no desafio (uma participação por usuário; repetir responde 409).</summary>
    [HttpPost("{desafioId:int}/participar")]
    public async Task<IActionResult> Participar(int desafioId, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await desafios.ParticiparAsync(User.ObterUsuarioId(), desafioId, ct));

    [HttpGet("{desafioId:int}/progresso")]
    public async Task<IActionResult> Progresso(int desafioId, CancellationToken ct) =>
        Ok(await desafios.ObterProgressoAsync(User.ObterUsuarioId(), desafioId, ct));

    /// <summary>Conclui quando o requisito foi cumprido e concede o XP e a medalha configurados pelo Admin.</summary>
    [HttpPost("{desafioId:int}/concluir")]
    public async Task<IActionResult> Concluir(int desafioId, CancellationToken ct) =>
        Ok(await desafios.ConcluirAsync(User.ObterUsuarioId(), desafioId, ct));
}
