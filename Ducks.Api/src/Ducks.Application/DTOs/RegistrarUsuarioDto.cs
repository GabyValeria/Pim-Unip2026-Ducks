using System.ComponentModel.DataAnnotations;

namespace Ducks.Application.DTOs;

public class RegistrarUsuarioDto
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string NomeUsuario { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Senha { get; set; } = string.Empty;
}
