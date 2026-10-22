using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Admin.Desktop.Models;
using Ducks.Admin.Desktop.Services;
using Ducks.Admin.Desktop.Stores;
using System.Collections.ObjectModel;
using System.Windows;

namespace Ducks.Admin.Desktop.ViewModels;

public partial class AiAssistantViewModel : ObservableObject
{
    private readonly IAdminApiService _adminApiService;
    private readonly AdminRealtimeService _realtimeService;
    private readonly UserSessionStore _sessionStore;

    [ObservableProperty] private string _promptInput = string.Empty;
    [ObservableProperty] private string _respostaIa = "Olá Administrador! Como posso ajudar na moderação ou automação hoje?";
    [ObservableProperty] private bool _isWorking;
    
    [ObservableProperty] private int _usuariosAtivosOnline = 0;

    public ObservableCollection<AuditLogDto> LogsAuditoria { get; } = new();

    public AiAssistantViewModel(
        IAdminApiService adminApiService, 
        AdminRealtimeService realtimeService,
        UserSessionStore sessionStore)
    {
        _adminApiService = adminApiService;
        _realtimeService = realtimeService;
        _sessionStore = sessionStore;

        _realtimeService.MetricUpdated += OnMetricUpdated;
        _realtimeService.AuditLogReceived += OnAuditLogReceived;

        _ = InicializarTempoRealAsync();
        _ = CarregarLogsAsync();
    }

    private async Task InicializarTempoRealAsync()
    {
        if (!string.IsNullOrEmpty(_sessionStore.JwtToken))
        {
            await _realtimeService.ConnectAsync("https://localhost:5001", _sessionStore.JwtToken);
        }
    }

    private void OnMetricUpdated(string metric, int value)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (metric == "UsuariosAtivos") UsuariosAtivosOnline = value;
        });
    }

    private void OnAuditLogReceived(string user, string action)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogsAuditoria.Insert(0, new AuditLogDto 
            { 
                Usuario = user, 
                Acao = action, 
                DataHora = DateTime.Now 
            });
        });
    }

    [RelayCommand]
    private async Task EnviarPromptAsync()
    {
        if (string.IsNullOrWhiteSpace(PromptInput)) return;

        try
        {
            IsWorking = true;
            var promptAtual = PromptInput;
            PromptInput = string.Empty;
            RespostaIa = "Processando com IA do ecossistema Ducks...";
            
            var response = await _adminApiService.ConsultarAssistenteIaAsync(new AiRequestDto(promptAtual, "Administração Geral"));
            RespostaIa = response.Resposta;
        }
        catch (Exception)
        {
            RespostaIa = "Erro ao comunicar com o Assistente de IA via API.";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task CarregarLogsAsync()
    {
        try
        {
            var logs = await _adminApiService.ObterLogsAuditoriaAsync();
            LogsAuditoria.Clear();
            foreach (var log in logs) LogsAuditoria.Add(log);
        }
        catch (Exception) { }
    }
}