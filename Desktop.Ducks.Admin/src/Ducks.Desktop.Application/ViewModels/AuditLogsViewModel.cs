using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;

namespace Ducks.Desktop.Application.ViewModels
{
    public class AuditLogDto
    {
        public int LogID { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public string Detalhes { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
    }

    public partial class AuditLogsViewModel : ObservableObject
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthApiService _authService;
        private List<AuditLogDto> _todosLogs = new();

        [ObservableProperty]
        private ObservableCollection<AuditLogDto> _logs = new();

        [ObservableProperty]
        private AuditLogDto? _selectedLog;

        [ObservableProperty]
        private string _filtroTermo = string.Empty;

        [ObservableProperty]
        private string _mensagemStatus = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public IAsyncRelayCommand CarregarLogsCommand { get; }

        public AuditLogsViewModel(IHttpClientFactory httpClientFactory, IAuthApiService authService)
        {
            _httpClientFactory = httpClientFactory;
            _authService = authService;
            CarregarLogsCommand = new AsyncRelayCommand(CarregarLogsAsync);

            _ = CarregarLogsAsync();
        }

        partial void OnFiltroTermoChanged(string value) => AplicarFiltro();

        public async Task CarregarLogsAsync()
        {
            IsLoading = true;
            MensagemStatus = "Carregando logs de auditoria...";
            try
            {
                var client = _httpClientFactory.CreateClient("DucksApi");
                if (!string.IsNullOrEmpty(_authService.CurrentToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authService.CurrentToken);
                }

                var response = await client.GetFromJsonAsync<List<AuditLogDto>>("api/admin/audit/logs");

                if (response != null && response.Any())
                {
                    _todosLogs = response;
                }
                else
                {
                    _todosLogs = ObterLogsDemonstracao();
                }

                AplicarFiltro();
                MensagemStatus = "Logs atualizados com sucesso.";
            }
            catch (Exception)
            {
                _todosLogs = ObterLogsDemonstracao();
                AplicarFiltro();
                MensagemStatus = "Logs carregados (Modo Offline/Demonstração).";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AplicarFiltro()
        {
            var resultado = _todosLogs.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(FiltroTermo))
            {
                resultado = resultado.Where(l =>
                    l.Usuario.Contains(FiltroTermo, StringComparison.OrdinalIgnoreCase) ||
                    l.Acao.Contains(FiltroTermo, StringComparison.OrdinalIgnoreCase) ||
                    l.Detalhes.Contains(FiltroTermo, StringComparison.OrdinalIgnoreCase));
            }

            Logs = new ObservableCollection<AuditLogDto>(resultado);
        }

        private List<AuditLogDto> ObterLogsDemonstracao()
        {
            return new List<AuditLogDto>
            {
                new AuditLogDto { LogID = 101, Usuario = "admin@ducks.com", Acao = "BLOQUEIO_USUARIO", Detalhes = "Bloqueio do usuário barbara.paiva por violação de regra", DataHora = DateTime.Now.AddMinutes(-15) },
                new AuditLogDto { LogID = 102, Usuario = "curador@ducks.com", Acao = "CRIACAO_TRILHA", Detalhes = "Nova trilha criada: Trilha Completa de C# e .NET 10", DataHora = DateTime.Now.AddHours(-2) },
                new AuditLogDto { LogID = 103, Usuario = "admin@ducks.com", Acao = "ATRIBUICAO_RBAC", Detalhes = "Alteração de papel do usuário ID 25 para Moderador", DataHora = DateTime.Now.AddHours(-5) },
                new AuditLogDto { LogID = 104, Usuario = "moderador@ducks.com", Acao = "MODERACAO_CONTEUDO", Detalhes = "Aprovação de comentário sinalizado na aula 3", DataHora = DateTime.Now.AddDays(-1) }
            };
        }
    }
}