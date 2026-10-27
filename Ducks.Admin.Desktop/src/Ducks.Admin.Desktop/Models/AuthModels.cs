namespace Ducks.Admin.Desktop.Models
{
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public bool Sucesso { get; set; }
    }
}