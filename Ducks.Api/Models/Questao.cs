using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ducks.Api.Models;

[Table("Questoes")]
public class Questao
{
    [Key]
    [Column("QuestaoID")]
    public int QuestaoId { get; set; }

    [Column("MateriaID")]
    public int MateriaId { get; set; }

    [Column("TextoQuestao")]
    public string TextoQuestao { get; set; } = string.Empty;

    [Column("TipoQuestao")]
    public string TipoQuestao { get; set; } = string.Empty;

    [Column("Peso")]
    public int Peso { get; set; }

    [Column("NivelDificuldade")]
    public string NivelDificuldade { get; set; } = "FACIL"; // FACIL, MEDIO, DIFICIL

    [Column("Status")]
    public string Status { get; set; } = string.Empty;

    [Column("ModuloID")]
    public int ModuloId { get; set; }

    [Column("CriadoPorUsuarioID")]
    public int CriadoPorUsuarioId { get; set; }

    [Column("AprovadoPorUsuarioID")]
    public int? AprovadoPorUsuarioId { get; set; }
}