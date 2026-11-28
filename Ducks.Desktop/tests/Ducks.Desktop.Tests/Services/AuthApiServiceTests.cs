using System.Net;
using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Services;
using Moq;

namespace Ducks.Desktop.Tests
{
    public class AuthApiServiceTests
    {
        private const string LoginOk =
            "{\"usuarioID\":5,\"nomeUsuario\":\"Ana\",\"email\":\"ana@ducks.com\",\"token\":\"jwt-1\",\"refreshToken\":\"refresh-1\"}";

        private static AuthApiService Create(StubHttpHandler handler)
        {
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(ApiClient.ClientName)).Returns(handler.CreateClient());
            return new AuthApiService(factory.Object);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_StoresSessionData()
        {
            var handler = new StubHttpHandler().Enqueue(HttpStatusCode.OK, LoginOk);
            var service = Create(handler);

            var result = await service.LoginAsync("  ana@ducks.com ", "Senha123!");

            Assert.NotNull(result);
            Assert.True(result!.Sucesso);
            Assert.True(service.IsAuthenticated);
            Assert.Equal("jwt-1", service.CurrentToken);
            Assert.Equal("Ana", service.CurrentUserName);
            Assert.Equal(5, service.CurrentUserId);

            var call = Assert.Single(handler.Calls);
            Assert.EndsWith("api/auth/login", call.Uri);
            Assert.Contains("\"email\":\"ana@ducks.com\"", call.Body); // e-mail enviado sem espaços
        }

        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_ReturnsFriendlyMessageAndStaysLoggedOut()
        {
            var service = Create(new StubHttpHandler().Enqueue(HttpStatusCode.Unauthorized));

            var result = await service.LoginAsync("ana@ducks.com", "errada");

            Assert.False(result!.Sucesso);
            Assert.Equal("E-mail ou senha inválidos.", result.Mensagem);
            Assert.False(service.IsAuthenticated);
        }

        [Fact]
        public async Task LoginAsync_WithApiMessage_UsesApiMessage()
        {
            var service = Create(new StubHttpHandler().Enqueue(HttpStatusCode.BadRequest, "{\"message\":\"Conta bloqueada.\"}"));

            var result = await service.LoginAsync("ana@ducks.com", "x");

            Assert.Equal("Conta bloqueada.", result!.Mensagem);
        }

        [Fact]
        public async Task LoginAsync_TooManyRequests_ExplainsRateLimit()
        {
            var service = Create(new StubHttpHandler().Enqueue(HttpStatusCode.TooManyRequests));

            var result = await service.LoginAsync("ana@ducks.com", "x");

            Assert.Contains("Muitas tentativas", result!.Mensagem);
        }

        [Fact]
        public async Task LoginAsync_ServerError_ReportsUnavailableApi()
        {
            var service = Create(new StubHttpHandler().Enqueue(HttpStatusCode.InternalServerError));

            var result = await service.LoginAsync("ana@ducks.com", "x");

            Assert.Contains("indisponível", result!.Mensagem);
        }

        [Fact]
        public async Task LoginAsync_WhenApiIsUnreachable_ReturnsConnectionMessage()
        {
            var service = Create(new StubHttpHandler().EnqueueException(new HttpRequestException("offline")));

            var result = await service.LoginAsync("ana@ducks.com", "x");

            Assert.Contains("Não foi possível conectar", result!.Mensagem);
            Assert.False(service.IsAuthenticated);
        }

        [Fact]
        public async Task LoginAsync_WithMalformedResponse_ReturnsFormatMessage()
        {
            var service = Create(new StubHttpHandler().Enqueue(HttpStatusCode.OK, "não é json"));

            var result = await service.LoginAsync("ana@ducks.com", "x");

            Assert.Contains("formato inesperado", result!.Mensagem);
        }

        [Fact]
        public async Task Logout_ClearsSession()
        {
            var service = Create(new StubHttpHandler().Enqueue(HttpStatusCode.OK, LoginOk));
            await service.LoginAsync("ana@ducks.com", "x");

            service.Logout();

            Assert.False(service.IsAuthenticated);
            Assert.Null(service.CurrentToken);
            Assert.Null(service.CurrentUserName);
            Assert.Equal(0, service.CurrentUserId);
        }

        [Fact]
        public void NotifySessionExpired_WhenNotAuthenticated_DoesNotRaiseEvent()
        {
            var service = Create(new StubHttpHandler());
            var raised = 0;
            service.SessionExpired += () => raised++;

            service.NotifySessionExpired();

            Assert.Equal(0, raised);
        }

        [Fact]
        public async Task NotifySessionExpired_WhenAuthenticated_LogsOutAndRaisesEventOnce()
        {
            var service = Create(new StubHttpHandler().Enqueue(HttpStatusCode.OK, LoginOk));
            await service.LoginAsync("ana@ducks.com", "x");
            var raised = 0;
            service.SessionExpired += () => raised++;

            service.NotifySessionExpired();
            service.NotifySessionExpired(); // segunda chamada já não está autenticado

            Assert.Equal(1, raised);
            Assert.False(service.IsAuthenticated);
        }

        [Fact]
        public async Task TryRefreshAsync_WithoutRefreshToken_ReturnsFalse()
        {
            var service = Create(new StubHttpHandler().Enqueue(HttpStatusCode.OK, "{\"usuarioID\":1,\"token\":\"jwt\"}"));
            await service.LoginAsync("ana@ducks.com", "x");

            Assert.False(await service.TryRefreshAsync());
        }

        [Fact]
        public async Task TryRefreshAsync_WithValidRefreshToken_UpdatesAccessToken()
        {
            var handler = new StubHttpHandler()
                .Enqueue(HttpStatusCode.OK, LoginOk)
                .Enqueue(HttpStatusCode.OK, "{\"token\":\"jwt-2\",\"refreshToken\":\"refresh-2\"}");
            var service = Create(handler);
            await service.LoginAsync("ana@ducks.com", "x");

            var refreshed = await service.TryRefreshAsync();

            Assert.True(refreshed);
            Assert.Equal("jwt-2", service.CurrentToken);
            Assert.EndsWith("api/auth/refresh", handler.Calls[1].Uri);
            Assert.Contains("refresh-1", handler.Calls[1].Body);
        }

        [Fact]
        public async Task TryRefreshAsync_WhenApiRejects_ReturnsFalseAndKeepsToken()
        {
            var handler = new StubHttpHandler()
                .Enqueue(HttpStatusCode.OK, LoginOk)
                .Enqueue(HttpStatusCode.Unauthorized);
            var service = Create(handler);
            await service.LoginAsync("ana@ducks.com", "x");

            Assert.False(await service.TryRefreshAsync());
            Assert.Equal("jwt-1", service.CurrentToken);
        }

        [Fact]
        public async Task TryRefreshAsync_WhenApiIsUnreachable_ReturnsFalse()
        {
            var handler = new StubHttpHandler()
                .Enqueue(HttpStatusCode.OK, LoginOk)
                .EnqueueException(new HttpRequestException("offline"));
            var service = Create(handler);
            await service.LoginAsync("ana@ducks.com", "x");

            Assert.False(await service.TryRefreshAsync());
        }
    }
}
