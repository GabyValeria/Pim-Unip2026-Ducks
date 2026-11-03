using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Services
{
    public class MetricsApiService : IMetricsApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthApiService _authService;

        public MetricsApiService(IHttpClientFactory httpClientFactory, IAuthApiService authService)
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

        public async Task<DashboardMetricsDto?> GetDashboardMetricsAsync()
        {
            var client = CreateClientWithAuth();
            var response = await client.GetFromJsonAsync<DashboardMetricsDto>("api/admin/metrics/summary");
            return response ?? new DashboardMetricsDto();
        }
    }
}