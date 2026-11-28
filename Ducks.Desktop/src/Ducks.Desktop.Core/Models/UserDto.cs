using System;
using System.Linq;

namespace Ducks.Desktop.Core.Models
{
    /// <summary>Usuário retornado por GET api/admin/users (campo Roles é uma lista).</summary>
    public class UserDto
    {
        public int UsuarioID { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string[] Roles { get; set; } = Array.Empty<string>();
        public bool IsBlocked { get; set; }
        public DateTime CriadoEm { get; set; }

        /// <summary>Papéis formatados para exibição em tabelas.</summary>
        public string RolesDisplay => Roles is { Length: > 0 } ? string.Join(", ", Roles) : "—";

        public string StatusDisplay => IsBlocked ? "Bloqueado" : "Ativo";

        public bool HasRole(string role) =>
            Roles != null && Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    }
}
