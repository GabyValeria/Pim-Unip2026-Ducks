using System.ComponentModel.DataAnnotations;

namespace Ducks.Application.DTOs;

public sealed class RefreshTokenDto
{
    [StringLength(200, MinimumLength = 40)]
    public string? RefreshToken { get; set; }
}
