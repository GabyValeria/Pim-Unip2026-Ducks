using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ducks.Desktop.Application.Services
{
    public class ModerationApiService : IModerationApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthApiService _authService;
        private HubConnection? _hubConnection;

        public ModerationApiService(IHttpClientFactory httpClientFactory, IAuthApiService authService)
        {
            _httpClientFactory = httpClientFactory;
            _authService = authService;
        }

        public async Task<IEnumerable<ModerationItemDto>> GetPendingQueueAsync()
        {
            var client = CreateClientWithAuth();
            var response = await client.GetFromJsonAsync<IEnumerable<ModerationItemDto>>("api/admin/moderation/pending");
            return response ?? new List<ModerationItemDto>();
        }

        public async Task<bool> ApproveItemAsync(int itemId)
        {
            var client = CreateClientWithAuth();
            var response = await client.PostAsync($"api/admin/moderation/{itemId}/approve", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RejectItemAsync(int itemId)
        {
            var client = CreateClientWithAuth();
            var response = await client.PostAsync($"api/admin/moderation/{itemId}/reject", null);
            return response.IsSuccessStatusCode;
        }

        public async Task InitializeSignalRAsync(Action<ModerationItemDto> onNewItemReceived)
        {
            var client = _httpClientFactory.CreateClient("DucksApi");
            var baseUrl = client.BaseAddress?.ToString().TrimEnd('/') ?? "https://ducks-api-sistema-hfbndmc0cudwgcg6.brazilsouth-01.azurewebsites.net";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/hubs/moderation", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_authService.CurrentToken);
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<ModerationItemDto>("ReceiveModerationAlert", (item) =>
            {
                onNewItemReceived?.Invoke(item);
            });

            await _hubConnection.StartAsync();
        }

        public async Task StopSignalRAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
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