using Ducks.Application.DTOs;

namespace Ducks.Application.Services;

public interface IAuthService
{
    Task<AuthRespostaDto> RegistrarAsync(RegistrarUsuarioDto dto);
    Task<AuthRespostaDto> LoginAsync(LoginDto dto);
}