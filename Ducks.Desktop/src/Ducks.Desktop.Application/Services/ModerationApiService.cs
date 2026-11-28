using System.Net.Http.Json;
using System.Text.Json;
using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ducks.Desktop.Application.Services
{
    public class ModerationApiService : IModerationApiService
    {
        private readonly IApiClient _api;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthApiService _authService;
        private HubConnection? _hubConnection;

        public ModerationApiService(IApiClient api, IHttpClientFactory httpClientFactory, IAuthApiService authService)
        {
            _api = api;
            _httpClientFactory = httpClientFactory;
            _authService = authService;
        }

        public async Task<IReadOnlyList<ModerationItemDto>> GetPendingAsync(CancellationToken cancellationToken = default) =>
            ModerationItemParser.ParseList(await _api.GetAsync<JsonElement>(ApiRoutes.ModeracaoPendentes, cancellationToken));

        public async Task<IReadOnlyList<ModerationItemDto>> GetHistoryAsync(CancellationToken cancellationToken = default) =>
            ModerationItemParser.ParseList(await _api.GetAsync<JsonElement>(ApiRoutes.ModeracaoHistorico, cancellationToken));

        public Task ApproveAsync(string tipo, int id, string? comentario = null, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ModeracaoAprovar(tipo, id), new { Comentario = comentario }, cancellationToken);

        public Task RejectAsync(string tipo, int id, string justificativa, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ModeracaoRejeitar(tipo, id), new { Justificativa = justificativa }, cancellationToken);

        public Task RemoveAsync(string tipo, int id, string justificativa, CancellationToken cancellationToken = default) =>
            _api.PostAsync(ApiRoutes.ModeracaoRemover(tipo, id), new { Justificativa = justificativa }, cancellationToken);

        public async Task<bool> TryStartRealtimeAsync(Action<ModerationItemDto> onNewItem)
        {
            try
            {
                await StopRealtimeAsync();

                var baseAddress = _httpClientFactory.CreateClient(ApiClient.ClientName).BaseAddress?.ToString().TrimEnd('/');
                if (string.IsNullOrEmpty(baseAddress)) return false;

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{baseAddress}/{ApiRoutes.ModerationHub}", options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(_authService.CurrentToken);
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<JsonElement>("ReceiveModerationAlert", payload =>
                {
                    onNewItem(ModerationItemParser.Parse(payload));
                });

                await _hubConnection.StartAsync();
                return true;
            }
            catch
            {
                // O hub é opcional: a fila continua funcional via botão "Atualizar".
                await StopRealtimeAsync();
                return false;
            }
        }

        public async Task StopRealtimeAsync()
        {
            var hub = _hubConnection;
            _hubConnection = null;
            if (hub == null) return;

            try { await hub.StopAsync(); } catch { /* ignorado */ }
            await hub.DisposeAsync();
        }
    }
}
