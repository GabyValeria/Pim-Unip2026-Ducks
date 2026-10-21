using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ducks.Api.Models;

[Table("Usuarios")]
public class Usuario
{
    [Key]
    [Column("UsuarioID")]
    public int UsuarioId { get; set; }

    [Column("NomeUsuario")]
    public string NomeUsuario { get; set; } = string.Empty;

    [Column("Email")]
    public string Email { get; set; } = string.Empty;

    [Column("HashSenha")]
    public string HashSenha { get; set; } = string.Empty;

    [Column("SaltSenha")]
    public string SaltSenha { get; set; } = string.Empty;

    [Column("EstaAtivo")]
    public bool EstaAtivo { get; set; }

    [Column("CriadoEm")]
    public DateTime CriadoEm { get; set; }

    [Column("AtualizadoEm")]
    public DateTime? AtualizadoEm { get; set; }
}