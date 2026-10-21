using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ducks.Api.Models;

[Table("Aulas")]
public class Aula
{
    [Key]
    [Column("AulaID")]
    public int AulaId { get; set; }

    [Column("ModuloID")]
    public int ModuloId { get; set; }

    [Column("Titulo")]
    public string Titulo { get; set; } = string.Empty;

    [Column("UrlConteudo")]
    public string? UrlConteudo { get; set; }

    [Column("IndiceOrdem")]
    public int IndiceOrdem { get; set; }

    [Column("Status")]
    public string Status { get; set; } = "PENDENTE";
}