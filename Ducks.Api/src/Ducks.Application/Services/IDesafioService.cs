using Ducks.Application.DTOs;

namespace Ducks.Application.Services;

/// <summary>
/// Participação dos estudantes nos desafios cadastrados pelo Admin (api/admin/gamification/challenges).
/// O progresso conta o que o estudante fez depois de entrar no desafio (exceto XP_TOTAL, que usa o saldo atual).
/// </summary>
public interface IDesafioService
{
    Task<ResultadoPaginado<DesafioEstudanteDto>> ListarAsync(int usuarioId, FiltroDesafiosDto filtro, CancellationToken ct = default);
    Task<ProgressoDesafioDto> ParticiparAsync(int usuarioId, int desafioId, CancellationToken ct = default);
    Task<ProgressoDesafioDto> ObterProgressoAsync(int usuarioId, int desafioId, CancellationToken ct = default);
    Task<ConclusaoDesafioDto> ConcluirAsync(int usuarioId, int desafioId, CancellationToken ct = default);
}
