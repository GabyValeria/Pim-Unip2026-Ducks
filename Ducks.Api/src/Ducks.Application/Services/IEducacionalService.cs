using Ducks.Domain.Entities;

namespace Ducks.Application.Services;

public interface IEducacionalService
{
    Task<IEnumerable<TrilhaAprendizagem>> ListarTrilhasAsync(CancellationToken cancellationToken = default);
    Task<XPUsuario?> ObterXPUsuarioAsync(int usuarioID, CancellationToken cancellationToken = default);
    Task AdicionarXPAsync(int usuarioID, int quantidadeXP, string motivo, CancellationToken cancellationToken = default);
}
