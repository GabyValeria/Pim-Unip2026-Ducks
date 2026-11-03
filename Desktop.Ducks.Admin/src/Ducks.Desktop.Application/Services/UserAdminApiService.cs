using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Services
{
    public class UserAdminApiService : IUserAdminApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthApiService _authService;

        public UserAdminApiService(IHttpClientFactory httpClientFactory, IAuthApiService authService)
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

        public async Task<IEnumerable<UserDto>> GetUsersAsync(string? search = null)
        {
            var client = CreateClientWithAuth();
            var url = string.IsNullOrEmpty(search) ? "api/admin/users" : $"api/admin/users?search={search}";
            var response = await client.GetFromJsonAsync<IEnumerable<UserDto>>(url);
            return response ?? new List<UserDto>();
        }

        public async Task<bool> ToggleBlockUserAsync(int userId, bool block)
        {
            var client = CreateClientWithAuth();
            var response = await client.PutAsync($"api/admin/users/{userId}/block?block={block}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<UserDto>> GetAuthorsAsync()
        {
            var client = CreateClientWithAuth();
            var response = await client.GetFromJsonAsync<IEnumerable<UserDto>>("api/admin/authors");
            return response ?? new List<UserDto>();
        }

        public async Task<bool> ApproveAuthorAsync(int userId)
        {
            var client = CreateClientWithAuth();
            var response = await client.PostAsync($"api/admin/authors/{userId}/approve", null);
            return response.IsSuccessStatusCode;
        }
    }
}