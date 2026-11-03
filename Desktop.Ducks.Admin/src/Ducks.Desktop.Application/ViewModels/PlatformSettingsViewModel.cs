using System;
using System.Collections.Generic;
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
    public class ParametroGlobalDto
    {
        public string Chave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }

    public partial class PlatformSettingsViewModel : ObservableObject
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthApiService _authService;

        [ObservableProperty]
        private ObservableCollection<ParametroGlobalDto> _parametros = new();

        [ObservableProperty]
        private ParametroGlobalDto? _selectedParametro;

        [ObservableProperty]
        private string _novoValor = string.Empty;

        [ObservableProperty]
        private string _mensagemFeedback = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public IAsyncRelayCommand CarregarParametrosCommand { get; }
        public IAsyncRelayCommand SalvarParametroCommand { get; }

        public PlatformSettingsViewModel(IHttpClientFactory httpClientFactory, IAuthApiService authService)
        {
            _httpClientFactory = httpClientFactory;
            _authService = authService;

            CarregarParametrosCommand = new AsyncRelayCommand(CarregarParametrosAsync);
            SalvarParametroCommand = new AsyncRelayCommand(SalvarParametroAsync);
            
            _ = CarregarParametrosAsync();
        }

        private HttpClient CreateClientWithAuth()
        {
            var client = _httpClientFactory.CreateClient("DucksApi");
            if (!string.IsNullOrEmpty(_authService.CurrentToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authService.CurrentToken);
            }
            return client;
        }

        partial void OnSelectedParametroChanged(ParametroGlobalDto? value)
        {
            if (value != null)
            {
                NovoValor = value.Valor;
            }
        }

        public async Task CarregarParametrosAsync()
        {
            IsLoading = true;
            MensagemFeedback = "Carregando parâmetros globais...";
            try
            {
                var client = CreateClientWithAuth();
                var settings = await client.GetFromJsonAsync<Dictionary<string, string>>("api/admin/settings");
                if (settings != null)
                {
                    Parametros.Clear();
                    foreach (var item in settings)
                    {
                        Parametros.Add(new ParametroGlobalDto { Chave = item.Key, Valor = item.Value });
                    }
                    MensagemFeedback = "Parâmetros carregados com sucesso.";
                }
            }
            catch (Exception ex)
            {
                MensagemFeedback = $"Erro ao carregar parâmetros: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SalvarParametroAsync()
        {
            if (SelectedParametro == null || string.IsNullOrWhiteSpace(NovoValor))
            {
                MensagemFeedback = "Selecione um parâmetro e informe o novo valor.";
                return;
            }

            try
            {
                var client = CreateClientWithAuth();
                var payload = new { Chave = SelectedParametro.Chave, Valor = NovoValor };
                var response = await client.PutAsJsonAsync("api/admin/settings", payload);

                if (response.IsSuccessStatusCode)
                {
                    MensagemFeedback = $"Parâmetro '{SelectedParametro.Chave}' atualizado com sucesso para '{NovoValor}'!";
                    await CarregarParametrosAsync();
                }
                else
                {
                    MensagemFeedback = "Falha ao comunicar com a API para salvar o parâmetro.";
                }
            }
            catch (Exception ex)
            {
                MensagemFeedback = $"Erro ao salvar: {ex.Message}";
            }
        }
    }
}