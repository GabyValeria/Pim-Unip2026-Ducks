using System;
using System.Text.Json.Serialization;

namespace Ducks.Admin.Desktop.Models
{
    public class LoginRequestDto
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("senha")]
        public string Senha { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        [JsonPropertyName("usuarioID")]
        public int UsuarioID { get; set; }

        [JsonPropertyName("nomeUsuario")]
        public string NomeUsuario { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("expiracao")]
        public DateTime Expiracao { get; set; }

        [JsonPropertyName("sucesso")]
        public bool Sucesso { get; set; }

        [JsonPropertyName("mensagem")]
        public string Mensagem { get; set; } = string.Empty;
    }
}