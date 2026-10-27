using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Admin.Desktop.Models;
using Ducks.Admin.Desktop.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Ducks.Admin.Desktop.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ISignalRService _signalRService;
        private readonly IApiClient _apiClient;

        [ObservableProperty]
        private DashboardMetricsDto _metrics = new();

        [ObservableProperty]
        private ObservableCollection<AuditLogDto> _auditLogs = new();

        [ObservableProperty]
        private string _realTimeStatus = "Desconectado do Hub SignalR.";

        [ObservableProperty]
        private bool _isRealTimeActive;

        public DashboardViewModel(ISignalRService signalRService, IApiClient apiClient)
        {
            _signalRService = signalRService;
            _apiClient = apiClient;

            // Subscreve aos eventos do SignalR
            _signalRService.OnAdminNotificationReceived += (titulo, mensagem) =>
            {
                AuditLogs.Insert(0, new AuditLogDto
                {
                    AdministradorEmail = titulo,
                    AcaoRealizada = mensagem,
                    DataHora = System.DateTime.Now
                });
            };

            _signalRService.OnModerationAlertReceived += (total) =>
            {
                Metrics.ItensPendentesModeracao = total;
                OnPropertyChanged(nameof(Metrics));
            };
        }

        [RelayCommand]
        private async Task ConnectRealTimeAsync()
        {
            RealTimeStatus = "Conectando ao SignalR...";
            await _signalRService.ConnectAsync("https://ducks-api-sistema-hfbndmc0cudwgcg6.brazilsouth-01.azurewebsites.net/hubs/admin");
            IsRealTimeActive = _signalRService.IsConnected;
            RealTimeStatus = IsRealTimeActive ? "Conectado em Tempo Real (SignalR Ativo)" : "Falha na conexão em tempo real.";
        }

        [RelayCommand]
        private async Task LoadMetricsAsync()
        {
            // Simulação de carregamento inicial de métricas de painel
            Metrics = new DashboardMetricsDto
            {
                TotalUsuariosAtivos = 1250,
                TotalCursosPublicados = 24,
                ItensPendentesModeracao = 3,
                AcessosHoje = 412
            };
            await Task.CompletedTask;
        }
    }
}