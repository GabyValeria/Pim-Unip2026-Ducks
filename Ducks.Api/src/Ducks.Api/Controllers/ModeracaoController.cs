using Ducks.Api.Extensions;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

/// <summary>
/// Revisão de conteúdo pelo Moderador (ou Admin). {tipo}: trilha, modulo, aula, questao ou simulado.
/// </summary>
[Authorize(Policy = "Moderation")]
[ApiController]
[Route("api/moderacao")]
public sealed class ModeracaoController(IModeracaoService moderacao) : ControllerBase
{
    /// <summary>Fila de itens em revisão (estado PENDENTE). ordenarPor: enviadoEm (padrão), titulo, tipo.</summary>
    [HttpGet("pendentes")]
    public async Task<IActionResult> ListarPendentes([FromQuery] FiltroPendentesDto filtro, CancellationToken ct) =>
        Ok(await moderacao.ListarPendentesAsync(filtro, ct));

    /// <summary>Histórico de decisões (mais recente primeiro). Filtros: tipo, conteudoId, acao.</summary>
    [HttpGet("historico")]
    public async Task<IActionResult> ListarHistorico([FromQuery] FiltroHistoricoRevisaoDto filtro, CancellationToken ct) =>
        Ok(await moderacao.ListarHistoricoAsync(filtro, ct));

    /// <summary>Conteúdo completo para revisão (inclui gabarito de questões) e o histórico do item.</summary>
    [HttpGet("{tipo}/{id:int}")]
    public async Task<IActionResult> ObterItem(string tipo, int id, CancellationToken ct) =>
        Ok(await moderacao.ObterItemAsync(tipo, id, ct));

    [HttpPost("{tipo}/{id:int}/aprovar")]
    public async Task<IActionResult> Aprovar(string tipo, int id, [FromBody] AprovacaoDto? dto, CancellationToken ct) =>
        Ok(await moderacao.AprovarAsync(tipo, id, User.ObterUsuarioId(), dto?.Comentario, ct));

    [HttpPost("{tipo}/{id:int}/rejeitar")]
    public async Task<IActionResult> Rejeitar(string tipo, int id, [FromBody] JustificativaDto dto, CancellationToken ct) =>
        Ok(await moderacao.RejeitarAsync(tipo, id, User.ObterUsuarioId(), dto.Justificativa, ct));

    /// <summary>Remove conteúdo inadequado (fica com estado REMOVIDO e some para os estudantes).</summary>
    [HttpPost("{tipo}/{id:int}/remover")]
    public async Task<IActionResult> Remover(string tipo, int id, [FromBody] JustificativaDto dto, CancellationToken ct) =>
        Ok(await moderacao.RemoverAsync(tipo, id, User.ObterUsuarioId(), dto.Justificativa, ct));
}
