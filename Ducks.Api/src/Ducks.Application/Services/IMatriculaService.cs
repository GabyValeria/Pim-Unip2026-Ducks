using Ducks.Application.DTOs;

namespace Ducks.Application.Services;

/// <summary>Matrículas, conclusão de aulas, progresso, histórico e dashboard do estudante.</summary>
public interface IMatriculaService
{
    Task<ResultadoMatriculaDto> MatricularAsync(int usuarioId, int trilhaId, CancellationToken ct = default);
    Task CancelarAsync(int usuarioId, int trilhaId, CancellationToken ct = default);
    Task<ResultadoPaginado<MatriculaDto>> ListarAsync(int usuarioId, FiltroMatriculasDto filtro, CancellationToken ct = default);
    Task<ConclusaoAulaDto> ConcluirAulaAsync(int usuarioId, int aulaId, CancellationToken ct = default);
    Task<ProgressoTrilhaDto> ObterProgressoAsync(int usuarioId, int trilhaId, CancellationToken ct = default);
    /// <summary>Recalcula o percentual e, ao chegar em 100%, conclui a trilha e concede o XP (uma única vez).</summary>
    Task<AtualizacaoProgressoDto> AtualizarProgressoAsync(int usuarioId, int trilhaId, CancellationToken ct = default);
    Task<ResultadoPaginado<AtividadeHistoricoDto>> ListarHistoricoAsync(int usuarioId, FiltroHistoricoDto filtro, CancellationToken ct = default);
    Task<DashboardEstudanteDto> ObterDashboardAsync(int usuarioId, CancellationToken ct = default);
}
