using Ducks.Application.DTOs;

namespace Ducks.Application.Services;

public interface IAuthService
{
    Task<AuthRespostaDto> RegistrarAsync(RegistrarUsuarioDto dto, string? device, CancellationToken cancellationToken = default);
    Task<AuthRespostaDto> LoginAsync(LoginDto dto, string? device, CancellationToken cancellationToken = default);
    Task<AuthRespostaDto> RenovarAsync(string refreshToken, string? device, CancellationToken cancellationToken = default);
    Task RevogarAsync(string refreshToken, CancellationToken cancellationToken = default);
}
