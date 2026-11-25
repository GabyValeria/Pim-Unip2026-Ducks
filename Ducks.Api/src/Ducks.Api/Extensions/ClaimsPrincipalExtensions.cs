using System.Security.Claims;

namespace Ducks.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>ID do usuário autenticado (claim NameIdentifier). Sem ele a requisição recebe 401.</summary>
    public static int ObterUsuarioId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new UnauthorizedAccessException();
}
