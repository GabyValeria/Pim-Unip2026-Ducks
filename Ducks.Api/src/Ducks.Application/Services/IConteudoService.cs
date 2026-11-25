using Ducks.Application.DTOs;

namespace Ducks.Application.Services;

/// <summary>
/// Trilhas (detalhe e edição), módulos e aulas. Quando <c>somenteAprovado</c> é verdadeiro
/// (estudante e moderador consultando o catálogo), itens não aprovados respondem 404.
/// </summary>
public interface IConteudoService
{
    Task<TrilhaDetalheDto> ObterTrilhaAsync(int trilhaId, bool somenteAprovado, CancellationToken ct = default);
    Task AtualizarTrilhaAsync(int trilhaId, TrilhaRequestDto dto, int usuarioId, CancellationToken ct = default);

    Task<ResultadoPaginado<ModuloDto>> ListarModulosAsync(int trilhaId, FiltroConteudoDto filtro, bool somenteAprovado, CancellationToken ct = default);
    Task<ModuloDetalheDto> ObterModuloAsync(int moduloId, bool somenteAprovado, CancellationToken ct = default);
    Task<ModuloDto> CriarModuloAsync(int trilhaId, ModuloRequestDto dto, int usuarioId, CancellationToken ct = default);
    Task<ModuloDto> AtualizarModuloAsync(int moduloId, ModuloRequestDto dto, int usuarioId, CancellationToken ct = default);
    Task ExcluirModuloAsync(int moduloId, int usuarioId, CancellationToken ct = default);
    Task ReordenarModulosAsync(int trilhaId, IReadOnlyList<int> moduloIds, int usuarioId, CancellationToken ct = default);

    Task<ResultadoPaginado<AulaDto>> ListarAulasAsync(int moduloId, FiltroConteudoDto filtro, bool somenteAprovado, CancellationToken ct = default);
    Task<AulaDto> ObterAulaAsync(int aulaId, bool somenteAprovado, CancellationToken ct = default);
    Task<AulaDto> CriarAulaAsync(int moduloId, AulaRequestDto dto, int usuarioId, CancellationToken ct = default);
    Task<AulaDto> AtualizarAulaAsync(int aulaId, AulaRequestDto dto, int usuarioId, CancellationToken ct = default);
    Task ExcluirAulaAsync(int aulaId, int usuarioId, CancellationToken ct = default);
    Task ReordenarAulasAsync(int moduloId, IReadOnlyList<int> aulaIds, int usuarioId, CancellationToken ct = default);
}
