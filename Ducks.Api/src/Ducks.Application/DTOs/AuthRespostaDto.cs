namespace Ducks.Application.DTOs;

public class AuthRespostaDto
{
    public int UsuarioID { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime Expiracao { get; set; }
}