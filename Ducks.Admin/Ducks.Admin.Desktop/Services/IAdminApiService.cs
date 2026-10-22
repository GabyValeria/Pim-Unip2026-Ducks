using Refit;
using Ducks.Admin.Desktop.Models;

namespace Ducks.Admin.Desktop.Services;

public interface IAdminApiService
{
    // Usuários (Passo 3)
    [Get("/api/admin/usuarios")]
    Task<PagedResultDto<UsuarioAdminDto>> ObterUsuariosAsync([Query] string? termo, [Query] int pagina = 1, [Query] int tamanhoPagina = 10);

    [Put("/api/admin/usuarios/{id}/status")]
    Task AlterarStatusUsuarioAsync(int id, [Body] AlterarStatusRequestDto request);

    // Conteúdo Educacional (Passo 4)
    [Get("/api/admin/trilhas")]
    Task<List<TrilhaAdminDto>> ObterTrilhasAsync();

    [Post("/api/admin/trilhas")]
    Task CriarTrilhaAsync([Body] CriarTrilhaRequestDto request);

    [Put("/api/admin/trilhas/{id}/aprovar")]
    Task AprovarTrilhaAsync(int id);

    // Gamificação e Conquistas (Passo 4)
    [Get("/api/admin/conquistas")]
    Task<List<ConquistaAdminDto>> ObterConquistasAsync();

    [Post("/api/admin/conquistas")]
    Task CriarConquistaAsync([Body] CriarConquistaRequestDto request);
}