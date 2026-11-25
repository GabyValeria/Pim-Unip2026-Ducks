using Ducks.Application.DTOs;

namespace Ducks.Application.Services;

/// <summary>
/// Tentativas de simulado: iniciar, responder, finalizar (correção automática), histórico.
/// Gabarito e explicações só aparecem depois de finalizar. Ao passar do prazo, a tentativa é finalizada sozinha.
/// </summary>
public interface ITentativaService
{
    Task<InicioTentativaDto> IniciarAsync(int usuarioId, int provaId, CancellationToken ct = default);
    Task<TentativaDto> ObterAsync(int usuarioId, int tentativaId, CancellationToken ct = default);
    Task<RespostaRegistradaDto> ResponderAsync(int usuarioId, int tentativaId, IReadOnlyList<RespostaItemDto> respostas, CancellationToken ct = default);
    Task<TentativaDto> FinalizarAsync(int usuarioId, int tentativaId, CancellationToken ct = default);
    Task<ResultadoPaginado<TentativaResumoDto>> ListarAsync(int usuarioId, FiltroTentativasDto filtro, CancellationToken ct = default);
}
