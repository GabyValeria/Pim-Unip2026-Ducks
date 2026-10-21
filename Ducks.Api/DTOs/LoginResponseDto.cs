namespace Ducks.Api.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string TipoToken { get; set; } = "Bearer";
    public int ExpiraEmMinutos { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public List<string> Papeis { get; set; } = new();
}