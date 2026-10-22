namespace Ducks.Admin.Desktop.Models;

public class AuditLogDto
{
    public int Id { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public DateTime DataHora { get; set; }
}