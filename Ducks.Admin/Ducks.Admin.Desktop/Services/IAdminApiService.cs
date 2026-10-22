using Refit;
using Ducks.Admin.Desktop.Models;

namespace Ducks.Admin.Desktop.Services;

public interface IAdminApiService
{
    [Get("/api/admin/usuarios")]
    Task<PagedResultDto<UsuarioAdminDto>> ObterUsuariosAsync(
        [Query] string? termo, 
        [Query] int pagina = 1, 
        [Query] int tamanhoPagina = 10);

    [Put("/api/admin/usuarios/{id}/status")]
    Task AlterarStatusUsuarioAsync(int id, [Body] AlterarStatusRequestDto request);
}