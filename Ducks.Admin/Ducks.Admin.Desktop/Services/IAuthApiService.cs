using Refit;

namespace Ducks.Admin.Desktop.Services;

public record LoginRequestDto(string Email, string Senha);
public record LoginResponseDto(string Token, string Mensagem);

public interface IAuthApiService
{
    [Post("/api/auth/login")]
    Task<LoginResponseDto> LoginAsync([Body] LoginRequestDto request);
}