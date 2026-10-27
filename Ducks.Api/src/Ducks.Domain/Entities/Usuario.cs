using System;

namespace Ducks.Domain.Entities;

public class Usuario
{
    public int UsuarioID { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public bool EstaAtivo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
}