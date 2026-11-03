using System;

namespace Ducks.Desktop.Core.Models
{
    public class AuthResponseDto
    {
        public int UsuarioID { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime Expiracao { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        
        private bool _sucesso;
        public bool Sucesso 
        { 
            get => _sucesso || !string.IsNullOrEmpty(Token); 
            set => _sucesso = value; 
        }
    }

    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }
}