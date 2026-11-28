namespace Ducks.Desktop.Core.Models
{
    /// <summary>Resposta de GET api/admin/metrics/summary.</summary>
    public class DashboardMetricsDto
    {
        public int TotalUsuarios { get; set; }
        public int TotalUsuariosAtivos { get; set; }
        public int TotalTrilhas { get; set; }
        public int TotalAulasCadastradas { get; set; }
        public long TotalXpDistribuido { get; set; }
        public int ItensPendentesModeracao { get; set; }

        public double PercentualAtivos => TotalUsuarios <= 0 ? 0 : TotalUsuariosAtivos * 100.0 / TotalUsuarios;
    }
}
