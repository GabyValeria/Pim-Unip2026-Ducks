using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ducks.Api.Models;

[Table("Materias")]
public class Materia
{
    [Key]
    [Column("MateriaID")]
    public int MateriaId { get; set; }

    [Column("NomeMateria")]
    public string NomeMateria { get; set; } = string.Empty;

    [Column("Descricao")]
    public string? Descricao { get; set; }
}