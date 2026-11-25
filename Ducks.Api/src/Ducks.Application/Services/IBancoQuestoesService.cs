using Ducks.Application.DTOs;

namespace Ducks.Application.Services;

/// <summary>Banco de questões e simulados (gestão pelo Curador/Admin; consulta do estudante sem gabarito).</summary>
public interface IBancoQuestoesService
{
    Task<ResultadoPaginado<QuestaoDto>> ListarQuestoesAsync(FiltroQuestoesDto filtro, CancellationToken ct = default);
    Task<QuestaoDto> ObterQuestaoAsync(int questaoId, CancellationToken ct = default);
    Task<QuestaoDto> CriarQuestaoAsync(QuestaoRequestDto dto, int usuarioId, CancellationToken ct = default);
    Task<QuestaoDto> AtualizarQuestaoAsync(int questaoId, QuestaoRequestDto dto, int usuarioId, CancellationToken ct = default);
    Task ExcluirQuestaoAsync(int questaoId, int usuarioId, CancellationToken ct = default);

    Task<ResultadoPaginado<SimuladoDto>> ListarSimuladosAsync(FiltroSimuladosDto filtro, CancellationToken ct = default);
    Task<SimuladoDto> ObterSimuladoAsync(int provaId, CancellationToken ct = default);
    Task<SimuladoDto> CriarSimuladoAsync(SimuladoRequestDto dto, int usuarioId, CancellationToken ct = default);
    Task<SimuladoDto> AtualizarSimuladoAsync(int provaId, SimuladoRequestDto dto, int usuarioId, CancellationToken ct = default);
    Task ExcluirSimuladoAsync(int provaId, int usuarioId, CancellationToken ct = default);
    Task<SimuladoDto> DefinirQuestoesAsync(int provaId, IReadOnlyList<int> questaoIds, int usuarioId, CancellationToken ct = default);
    Task<SimuladoDto> SortearQuestoesAsync(int provaId, SortearQuestoesDto dto, int usuarioId, CancellationToken ct = default);

    Task<ResultadoPaginado<SimuladoEstudanteDto>> ListarSimuladosDisponiveisAsync(int usuarioId, FiltroSimuladosDto filtro, CancellationToken ct = default);
    Task<SimuladoEstudanteDto> ObterSimuladoDisponivelAsync(int usuarioId, int provaId, CancellationToken ct = default);
}
