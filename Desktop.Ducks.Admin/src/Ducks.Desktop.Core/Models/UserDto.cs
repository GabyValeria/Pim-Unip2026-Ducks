using System;

namespace Ducks.Desktop.Core.Models
{
    public class UserDto
    {
        public int UsuarioID { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}