using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Services
{
    public class AiAutomationApiService : IAiAutomationApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthApiService _authService;

        public AiAutomationApiService(IHttpClientFactory httpClientFactory, IAuthApiService authService)
        {
            _httpClientFactory = httpClientFactory;
            _authService = authService;
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

        public async Task<AiResponseDto> ProcessarTarefaAdminAsync(AiPromptRequestDto request)
        {
            var client = CreateClientWithAuth();
            var response = await client.PostAsJsonAsync("api/admin/ai/automate", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AiResponseDto>();
                return result ?? new AiResponseDto { Sucesso = false, MensagemErro = "Resposta vazia da API de IA." };
            }

            return new AiResponseDto { Sucesso = false, MensagemErro = $"Erro HTTP: {response.StatusCode}" };
        }
    }
}