using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ducks.Api.Models;

[Table("XPUsuario")]
public class XPUsuario
{
    [Key]
    [Column("UsuarioID")]
    public int UsuarioId { get; set; }

    [Column("TotalXP")]
    public int TotalXP { get; set; }

    [Column("NivelAtualID")]
    public int NivelAtualId { get; set; }

    [Column("AtualizadoEm")]
    public DateTime AtualizadoEm { get; set; }
}