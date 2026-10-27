using Ducks.Domain.Entities;

namespace Ducks.Application.Services;

public interface IEducacionalService
{
    Task<IEnumerable<TrilhaAprendizagem>> ListarTrilhasAsync();
    Task<XPUsuario?> ObterXPUsuarioAsync(int usuarioID);
    Task AdicionarXPAsync(int usuarioID, int quantidadeXP, string motivo);
}