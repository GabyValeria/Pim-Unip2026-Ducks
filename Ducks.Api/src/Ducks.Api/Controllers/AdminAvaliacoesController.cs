using Ducks.Api.Extensions;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

/// <summary>Banco de questões e simulados (Curador e Admin). Inclui gabarito e explicações.</summary>
[Authorize(Policy = "ContentManagement")]
[ApiController]
[Route("api/admin/content")]
public sealed class AdminAvaliacoesController(IBancoQuestoesService banco, IModeracaoService moderacao) : ControllerBase
{
    /// <summary>Filtros: status, dificuldade, tipo, tag, assunto, trilhaId, moduloId, aulaId, busca. ordenarPor: criadoEm (padrão, recentes primeiro), atualizadoEm, dificuldade, status, id.</summary>
    [HttpGet("questoes")]
    public async Task<IActionResult> ListarQuestoes([FromQuery] FiltroQuestoesDto filtro, CancellationToken ct) =>
        Ok(await banco.ListarQuestoesAsync(filtro, ct));

    [HttpGet("questoes/{questaoId:int}")]
    public async Task<IActionResult> ObterQuestao(int questaoId, CancellationToken ct) =>
        Ok(await banco.ObterQuestaoAsync(questaoId, ct));

    [HttpPost("questoes")]
    public async Task<IActionResult> CriarQuestao([FromBody] QuestaoRequestDto dto, CancellationToken ct)
    {
        var questao = await banco.CriarQuestaoAsync(dto, User.ObterUsuarioId(), ct);
        return CreatedAtAction(nameof(ObterQuestao), new { questaoId = questao.QuestaoID }, questao);
    }

    [HttpPut("questoes/{questaoId:int}")]
    public async Task<IActionResult> AtualizarQuestao(int questaoId, [FromBody] QuestaoRequestDto dto, CancellationToken ct) =>
        Ok(await banco.AtualizarQuestaoAsync(questaoId, dto, User.ObterUsuarioId(), ct));

    [HttpDelete("questoes/{questaoId:int}")]
    public async Task<IActionResult> ExcluirQuestao(int questaoId, CancellationToken ct)
    {
        await banco.ExcluirQuestaoAsync(questaoId, User.ObterUsuarioId(), ct);
        return NoContent();
    }

    [HttpPost("questoes/{questaoId:int}/enviar-revisao")]
    public async Task<IActionResult> EnviarQuestao(int questaoId, CancellationToken ct) =>
        Ok(await moderacao.EnviarParaRevisaoAsync("QUESTAO", questaoId, User.ObterUsuarioId(), ct));

    // ------------------------------------------------------------ Simulados

    /// <summary>Filtros: status, trilhaId, moduloId, busca. ordenarPor: criadoEm (padrão), titulo, atualizadoEm.</summary>
    [HttpGet("simulados")]
    public async Task<IActionResult> ListarSimulados([FromQuery] FiltroSimuladosDto filtro, CancellationToken ct) =>
        Ok(await banco.ListarSimuladosAsync(filtro, ct));

    [HttpGet("simulados/{provaId:int}")]
    public async Task<IActionResult> ObterSimulado(int provaId, CancellationToken ct) =>
        Ok(await banco.ObterSimuladoAsync(provaId, ct));

    [HttpPost("simulados")]
    public async Task<IActionResult> CriarSimulado([FromBody] SimuladoRequestDto dto, CancellationToken ct)
    {
        var simulado = await banco.CriarSimuladoAsync(dto, User.ObterUsuarioId(), ct);
        return CreatedAtAction(nameof(ObterSimulado), new { provaId = simulado.ProvaID }, simulado);
    }

    [HttpPut("simulados/{provaId:int}")]
    public async Task<IActionResult> AtualizarSimulado(int provaId, [FromBody] SimuladoRequestDto dto, CancellationToken ct) =>
        Ok(await banco.AtualizarSimuladoAsync(provaId, dto, User.ObterUsuarioId(), ct));

    [HttpDelete("simulados/{provaId:int}")]
    public async Task<IActionResult> ExcluirSimulado(int provaId, CancellationToken ct)
    {
        await banco.ExcluirSimuladoAsync(provaId, User.ObterUsuarioId(), ct);
        return NoContent();
    }

    /// <summary>Seleção manual: lista completa de questões, na ordem desejada.</summary>
    [HttpPut("simulados/{provaId:int}/questoes")]
    public async Task<IActionResult> DefinirQuestoes(int provaId, [FromBody] DefinirQuestoesDto dto, CancellationToken ct) =>
        Ok(await banco.DefinirQuestoesAsync(provaId, dto.QuestaoIds, User.ObterUsuarioId(), ct));

    /// <summary>Seleção aleatória entre questões APROVADAS, por filtros (dificuldade, tag, assunto, trilha/módulo).</summary>
    [HttpPost("simulados/{provaId:int}/questoes/sortear")]
    public async Task<IActionResult> SortearQuestoes(int provaId, [FromBody] SortearQuestoesDto dto, CancellationToken ct) =>
        Ok(await banco.SortearQuestoesAsync(provaId, dto, User.ObterUsuarioId(), ct));

    [HttpPost("simulados/{provaId:int}/enviar-revisao")]
    public async Task<IActionResult> EnviarSimulado(int provaId, CancellationToken ct) =>
        Ok(await moderacao.EnviarParaRevisaoAsync("SIMULADO", provaId, User.ObterUsuarioId(), ct));
}
