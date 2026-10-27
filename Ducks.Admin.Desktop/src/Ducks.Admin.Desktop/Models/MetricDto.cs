namespace Ducks.Admin.Desktop.Models
{
    public class DashboardMetricsDto
    {
        public int TotalUsuariosAtivos { get; set; }
        public int TotalCursosPublicados { get; set; }
        public int ItensPendentesModeracao { get; set; }
        public int AcessosHoje { get; set; }
    }
}