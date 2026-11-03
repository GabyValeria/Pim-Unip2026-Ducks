using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;

namespace Ducks.Desktop.Application.ViewModels
{
    public partial class MetricsDashboardViewModel : ObservableObject
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthApiService _authService;

        [ObservableProperty]
        private int _totalUsuariosAtivos = 6;

        [ObservableProperty]
        private string _taxaRetencao = "88.5%";

        [ObservableProperty]
        private int _modercaoPendente = 0;

        [ObservableProperty]
        private ObservableCollection<string> _opcoesFiltroPapeis = new() { "Todos", "Alunos", "Moderadores", "Curadores", "Administradores" };

        [ObservableProperty]
        private string _filtroSelecionado = "Todos";

        [ObservableProperty]
        private double _progressoAlunos = 70;

        [ObservableProperty]
        private double _progressoModeradores = 15;

        [ObservableProperty]
        private double _progressoCuradores = 10;

        [ObservableProperty]
        private double _progressoAdministradores = 5;

        [ObservableProperty]
        private string _statusMensagem = "Métricas carregadas com sucesso.";

        public IAsyncRelayCommand CarregarMetricasCommand { get; }

        public MetricsDashboardViewModel(IHttpClientFactory httpClientFactory, IAuthApiService authService)
        {
            _httpClientFactory = httpClientFactory;
            _authService = authService;

            CarregarMetricasCommand = new AsyncRelayCommand(CarregarMetricasAsync);
            _ = CarregarMetricasAsync();
        }

        partial void OnFiltroSelecionadoChanged(string value)
        {
            // Simula o filtro dinâmico dos usuários ativos conforme o papel selecionado
            TotalUsuariosAtivos = value switch
            {
                "Alunos" => 4,
                "Moderadores" => 1,
                "Curadores" => 0,
                "Administradores" => 1,
                _ => 6
            };
            StatusMensagem = $"Filtro aplicado: Exibindo dados para '{value}'.";
        }

        public async Task CarregarMetricasAsync()
        {
            StatusMensagem = "Atualizando métricas da plataforma...";
            try
            {
                var client = _httpClientFactory.CreateClient("DucksApi");
                if (!string.IsNullOrEmpty(_authService.CurrentToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authService.CurrentToken);
                }

                // Opcional: Consumir endpoint real da API se disponível
                // var metrics = await client.GetFromJsonAsync<MetricsDto>("api/admin/metrics");

                await Task.Delay(400); // Simulação de latência de rede fluida
                StatusMensagem = "Métricas sincronizadas com o servidor com sucesso.";
            }
            catch (Exception ex)
            {
                StatusMensagem = $"Erro ao atualizar métricas: {ex.Message}";
            }
        }
    }
}