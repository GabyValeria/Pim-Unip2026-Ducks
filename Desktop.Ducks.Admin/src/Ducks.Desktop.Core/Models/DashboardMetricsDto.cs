namespace Ducks.Desktop.Core.Models
{
    public class DashboardMetricsDto
    {
        public int TotalUsuariosAtivos { get; set; }
        public int TotalTrilhas { get; set; }
        public int TotalAulasConcluidas { get; set; }
        public int TotalXpDistribuido { get; set; }
        public double TaxaRetencaoMedia { get; set; } // Ex: 85.4%
        public int ItensPendentesModercao { get; set; }
    }
}