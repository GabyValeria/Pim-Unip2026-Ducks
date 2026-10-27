using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ducks.Admin.Desktop.Models;

namespace Ducks.Admin.Desktop.Services
{
    public interface IApiClient
    {
        string? Token { get; set; }
        Task<AuthResponseDto?> LoginAsync(string email, string senha);
        Task<List<UserAdminDto>> GetUsersAsync();
        Task<bool> ToggleUserStatusAsync(int usuarioId);
        Task<List<ModerationItemDto>> GetPendingModerationAsync();
        Task<bool> ReviewModerationAsync(int itemId, bool aprovado);
    }

    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        public string? Token { get; set; }

        public ApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            // Configuração da URL base da API Ducks no Azure caso não venha injetada
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://ducks-api-sistema-hfbndmc0cudwgcg6.brazilsouth-01.azurewebsites.net/");
            }
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

        public async Task<List<UserAdminDto>> GetUsersAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<List<UserAdminDto>>("api/admin/usuarios");
            return response ?? new List<UserAdminDto>();
        }

        public async Task<bool> ToggleUserStatusAsync(int usuarioId)
        {
            var response = await _httpClient.PostAsync($"api/admin/usuarios/{usuarioId}/toggle-status", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<ModerationItemDto>> GetPendingModerationAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<List<ModerationItemDto>>("api/admin/moderacao/pendentes");
            return response ?? new List<ModerationItemDto>();
        }

        public async Task<bool> ReviewModerationAsync(int itemId, bool aprovado)
        {
            var payload = new { Aprovado = aprovado };
            var response = await _httpClient.PostAsJsonAsync($"api/admin/moderacao/{itemId}/revisar", payload);
            return response.IsSuccessStatusCode;
        }
    }
}