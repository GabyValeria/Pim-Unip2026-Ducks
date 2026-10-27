using System;

namespace Ducks.Admin.Desktop.Models
{
    public class AuditLogDto
    {
        public int AuditID { get; set; }
        public string AdministradorEmail { get; set; } = string.Empty;
        public string AcaoRealizada { get; set; } = string.Empty;
        public DateTime DataHora { get; set; } = DateTime.UtcNow;
    }
}