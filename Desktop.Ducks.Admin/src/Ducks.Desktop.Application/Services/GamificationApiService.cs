using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Services
{
    public class GamificationApiService : IGamificationApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthApiService _authService;

        public GamificationApiService(IHttpClientFactory httpClientFactory, IAuthApiService authService)
        {
            _httpClientFactory = httpClientFactory;
            _authService = authService;
        }

        public async Task<IEnumerable<BadgeDto>> GetBadgesAsync()
        {
            var client = CreateClientWithAuth();
            var response = await client.GetFromJsonAsync<IEnumerable<BadgeDto>>("api/admin/gamification/badges");
            return response ?? new List<BadgeDto>();
        }

        public async Task<bool> CreateBadgeAsync(BadgeDto badge)
        {
            var client = CreateClientWithAuth();
            var response = await client.PostAsJsonAsync("api/admin/gamification/badges", badge);
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<ChallengeDto>> GetChallengesAsync()
        {
            var client = CreateClientWithAuth();
            var response = await client.GetFromJsonAsync<IEnumerable<ChallengeDto>>("api/admin/gamification/challenges");
            return response ?? new List<ChallengeDto>();
        }

        public async Task<bool> SaveChallengeAsync(ChallengeDto challenge)
        {
            var client = CreateClientWithAuth();
            var response = await client.PostAsJsonAsync("api/admin/gamification/challenges", challenge);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AdjustUserXpAsync(UserXpAdjustmentDto adjustment)
        {
            var client = CreateClientWithAuth();
            var response = await client.PostAsJsonAsync("api/admin/gamification/xp/adjust", adjustment);
            return response.IsSuccessStatusCode;
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
    }
}