using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ducks.Api.Models;

[Table("TrilhasAprendizagem")]
public class TrilhaAprendizagem
{
    [Key]
    [Column("TrilhaID")]
    public int TrilhaId { get; set; }

    [Column("NomeTrilha")]
    public string NomeTrilha { get; set; } = string.Empty;

    [Column("Descricao")]
    public string? Descricao { get; set; }

    [Column("MateriaID")]
    public int MateriaId { get; set; }

    [Column("Status")]
    public string Status { get; set; } = "PENDENTE"; // PENDENTE, APROVADO, REJEITADO

    [Column("CriadoPorUsuarioID")]
    public int CriadoPorUsuarioId { get; set; }

    [Column("AprovadoPorUsuarioID")]
    public int? AprovadoPorUsuarioId { get; set; }

    [Column("AtualizadoEm")]
    public DateTime? AtualizadoEm { get; set; }
}