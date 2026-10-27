using System;

namespace Ducks.Admin.Desktop.Models
{
    public class UserAdminDto
    {
        public int UsuarioID { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EstaAtivo { get; set; } = true;
        public DateTime CriadoEm { get; set; }
    }
}