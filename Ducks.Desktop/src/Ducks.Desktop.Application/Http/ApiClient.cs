using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ducks.Desktop.Application.Interfaces;

namespace Ducks.Desktop.Application.Http
{
    public sealed class ApiClient : IApiClient
    {
        public const string ClientName = "DucksApi";

        public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthApiService _authService;

        public ApiClient(IHttpClientFactory httpClientFactory, IAuthApiService authService)
        {
            _httpClientFactory = httpClientFactory;
            _authService = authService;
        }

        public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Get, url, null, cancellationToken);
            return await ReadAsync<T>(response, cancellationToken);
        }

        public async Task<T?> PostAsync<T>(string url, object? body, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Post, url, body, cancellationToken);
            return await ReadAsync<T>(response, cancellationToken);
        }

        public async Task PostAsync(string url, object? body, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Post, url, body, cancellationToken);
        }

        public async Task<T?> PutAsync<T>(string url, object? body, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Put, url, body, cancellationToken);
            return await ReadAsync<T>(response, cancellationToken);
        }

        public async Task PutAsync(string url, object? body, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Put, url, body, cancellationToken);
        }

        public async Task DeleteAsync(string url, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Delete, url, null, cancellationToken);
        }

        private static async Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
        {
            if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
                return default;

            try
            {
                return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            }
            catch (JsonException ex)
            {
                throw new ApiException(ApiErrorKind.Unknown,
                    "A resposta da API está em um formato inesperado. Verifique se o Desktop e a API estão na mesma versão.",
                    response.StatusCode, ex);
            }
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, CancellationToken ct)
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var client = _httpClientFactory.CreateClient(ClientName);
                using var request = new HttpRequestMessage(method, url);

                if (body != null)
                    request.Content = JsonContent.Create(body, body.GetType(), null, JsonOptions);

                if (!string.IsNullOrEmpty(_authService.CurrentToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.CurrentToken);

                HttpResponseMessage response;
                try
                {
                    response = await client.SendAsync(request, ct);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    throw new ApiException(ApiErrorKind.Timeout,
                        "A API demorou demais para responder. Tente novamente em instantes.");
                }
                catch (HttpRequestException ex)
                {
                    throw new ApiException(ApiErrorKind.Network,
                        "Não foi possível conectar à API da Ducks. Verifique sua internet ou o status do servidor.",
                        null, ex);
                }

                if (response.IsSuccessStatusCode)
                    return response;

                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0
                    && await _authService.TryRefreshAsync(ct))
                {
                    response.Dispose();
                    continue; // repete a requisição com o novo token
                }

                var errorBody = await SafeReadBodyAsync(response, ct);
                var status = response.StatusCode;
                response.Dispose();

                if (status == HttpStatusCode.Unauthorized)
                    _authService.NotifySessionExpired();

                throw ApiErrorParser.FromResponse(status, errorBody);
            }

            // Inalcançável: o laço sempre retorna ou lança.
            throw new ApiException(ApiErrorKind.Unknown, "Falha inesperada ao comunicar com a API.");
        }

        private static async Task<string?> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try { return await response.Content.ReadAsStringAsync(ct); }
            catch { return null; }
        }
    }
}
