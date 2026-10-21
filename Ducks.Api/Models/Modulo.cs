using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ducks.Api.Models;

[Table("Modulos")]
public class Modulo
{
    [Key]
    [Column("ModuloID")]
    public int ModuloId { get; set; }

    [Column("TrilhaID")]
    public int TrilhaId { get; set; }

    [Column("Titulo")]
    public string Titulo { get; set; } = string.Empty;

    [Column("IndiceOrdem")]
    public int IndiceOrdem { get; set; }
}