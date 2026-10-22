namespace Ducks.Admin.Desktop.Models;

public class TrilhaAdminDto
{
    public int TrilhaId { get; set; }
    public string NomeTrilha { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // PENDENTE, APROVADO, REJEITADO
    public int MateriaId { get; set; }
}

public class CriarTrilhaRequestDto
{
    public string NomeTrilha { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int MateriaId { get; set; }
}