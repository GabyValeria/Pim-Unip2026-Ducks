using System.Net;
using System.Net.Http.Json;
using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.Services
{
    public class AuthApiService : IAuthApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private string? _refreshToken;

        public AuthApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public string? CurrentToken { get; set; }
        public string? CurrentUserName { get; private set; }
        public int CurrentUserId { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken);

        public event Action? SessionExpired;

        public async Task<AuthResponseDto?> LoginAsync(string email, string senha)
        {
            var client = _httpClientFactory.CreateClient(ApiClient.ClientName);
            var request = new LoginRequestDto { Email = email.Trim(), Senha = senha };

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsJsonAsync(ApiRoutes.Login, request, ApiClient.JsonOptions);
            }
            catch (OperationCanceledException)
            {
                return new AuthResponseDto { Mensagem = "A API demorou demais para responder. Tente novamente." };
            }
            catch (HttpRequestException)
            {
                return new AuthResponseDto { Mensagem = "Não foi possível conectar à API da Ducks. Verifique sua internet ou o status do servidor." };
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var apiMessage = ApiErrorParser.ExtractMessage(body);
                    var mensagem = response.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest =>
                            apiMessage ?? "E-mail ou senha inválidos.",
                        HttpStatusCode.Forbidden => "Acesso não autorizado para esta conta.",
                        HttpStatusCode.TooManyRequests => "Muitas tentativas de login. Aguarde um instante e tente novamente.",
                        _ => (int)response.StatusCode >= 500
                            ? "A API está indisponível no momento. Tente novamente em instantes."
                            : apiMessage ?? "Não foi possível autenticar."
                    };
                    return new AuthResponseDto { Mensagem = mensagem };
                }

                AuthResponseDto? result;
                try
                {
                    result = await response.Content.ReadFromJsonAsync<AuthResponseDto>(ApiClient.JsonOptions);
                }
                catch (System.Text.Json.JsonException)
                {
                    return new AuthResponseDto { Mensagem = "Resposta de login em formato inesperado." };
                }

                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    CurrentToken = result.Token;
                    _refreshToken = string.IsNullOrEmpty(result.RefreshToken) ? null : result.RefreshToken;
                    CurrentUserName = result.NomeUsuario;
                    CurrentUserId = result.UsuarioID;
                }

                return result;
            }
        }

        public async Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_refreshToken)) return false;

            var tokenAntes = CurrentToken;
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                // Outra requisição já renovou enquanto aguardávamos o lock.
                if (!string.Equals(tokenAntes, CurrentToken, StringComparison.Ordinal) && !string.IsNullOrEmpty(CurrentToken))
                    return true;

                var client = _httpClientFactory.CreateClient(ApiClient.ClientName);
                var payload = new RefreshTokenRequestDto { RefreshToken = _refreshToken! };

                using var response = await client.PostAsJsonAsync(ApiRoutes.Refresh, payload, ApiClient.JsonOptions, cancellationToken);
                if (!response.IsSuccessStatusCode) return false;

                var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>(ApiClient.JsonOptions, cancellationToken);
                if (result == null || string.IsNullOrEmpty(result.Token)) return false;

                CurrentToken = result.Token;
                if (!string.IsNullOrEmpty(result.RefreshToken)) _refreshToken = result.RefreshToken;
                return true;
            }
            catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException
                                       || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public void NotifySessionExpired()
        {
            if (!IsAuthenticated) return; // evita notificações repetidas / durante o login
            Logout();
            SessionExpired?.Invoke();
        }

        public void Logout()
        {
            CurrentToken = null;
            CurrentUserName = null;
            CurrentUserId = 0;
            _refreshToken = null;
        }
    }
}
