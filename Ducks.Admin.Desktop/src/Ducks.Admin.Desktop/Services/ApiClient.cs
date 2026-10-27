using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ducks.Admin.Desktop.Models;

namespace Ducks.Admin.Desktop.Services
{
    public interface IApiClient
    {
        Task<AuthResponseDto?> LoginAsync(string email, string senha);
        string? Token { get; set; }
    }

    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        public string? Token { get; set; }

        public ApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Configuração da URL base da API Ducks no Azure
            _httpClient.BaseAddress = new Uri("https://ducks-api-sistema-hfbndmc0cudwgcg6.brazilsouth-01.azurewebsites.net/");
        }

        public async Task<AuthResponseDto?> LoginAsync(string email, string senha)
        {
            var payload = new LoginRequestDto { Email = email, Senha = senha };
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", payload);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            }
            
            return new AuthResponseDto 
            { 
                Sucesso = false, 
                Mensagem = "Falha na autenticação com a API. Verifique suas credenciais." 
            };
        }
    }
}