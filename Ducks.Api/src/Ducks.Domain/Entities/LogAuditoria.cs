namespace Ducks.Domain.Entities;

public class LogAuditoria
{
    public int LogAuditoriaID { get; set; }
    public int UsuarioID { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Detalhes { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}