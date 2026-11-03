using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Services
{
    public class AuthApiService : IAuthApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public string? CurrentToken { get; set; }

        public AuthApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<AuthResponseDto?> LoginAsync(string email, string senha)
        {
            var client = _httpClientFactory.CreateClient("DucksApi");
            var request = new LoginRequestDto { Email = email, Senha = senha };

            var response = await client.PostAsJsonAsync("api/auth/login", request);
            if (!response.IsSuccessStatusCode)
            {
                return new AuthResponseDto { Mensagem = "Credenciais inválidas ou acesso não autorizado." };
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (result != null && !string.IsNullOrEmpty(result.Token))
            {
                CurrentToken = result.Token;
            }

            return result;
        }
    }
}