using System.Net;
using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;
using Moq;

namespace Ducks.Desktop.Tests
{
    public class ApiClientTests
    {
        private static (ApiClient Client, Mock<IAuthApiService> Auth, StubHttpHandler Handler) Create(StubHttpHandler handler)
        {
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(ApiClient.ClientName)).Returns(handler.CreateClient());

            var auth = new Mock<IAuthApiService>();
            auth.SetupProperty(a => a.CurrentToken, "token-1");

            return (new ApiClient(factory.Object, auth.Object), auth, handler);
        }

        [Fact]
        public async Task GetAsync_Success_DeserializesBodyAndSendsBearerToken()
        {
            var handler = new StubHttpHandler().Enqueue(HttpStatusCode.OK, "{\"chave\":\"site.nome\",\"valor\":\"Ducks\"}");
            var (client, _, _) = Create(handler);

            var result = await client.GetAsync<SettingDto>("api/admin/settings");

            Assert.NotNull(result);
            Assert.Equal("site.nome", result!.Chave);
            Assert.Equal("Ducks", result.Valor);
            var call = Assert.Single(handler.Calls);
            Assert.Equal(HttpMethod.Get, call.Method);
            Assert.Equal("Bearer token-1", call.Authorization);
            Assert.EndsWith("api/admin/settings", call.Uri);
        }

        [Fact]
        public async Task GetAsync_WithoutToken_DoesNotSendAuthorizationHeader()
        {
            var handler = new StubHttpHandler().Enqueue(HttpStatusCode.OK, "{}");
            var (client, auth, _) = Create(handler);
            auth.Object.CurrentToken = null;

            await client.GetAsync<SettingDto>("api/health");

            Assert.Null(Assert.Single(handler.Calls).Authorization);
        }

        [Fact]
        public async Task PostAsync_NoContent_ReturnsDefault()
        {
            var handler = new StubHttpHandler().Enqueue(HttpStatusCode.NoContent);
            var (client, _, _) = Create(handler);

            var result = await client.PostAsync<SettingDto>("api/x", new { A = 1 });

            Assert.Null(result);
        }

        [Fact]
        public async Task PutAsync_SerializesBodyAsCamelCaseJson()
        {
            var handler = new StubHttpHandler().Enqueue(HttpStatusCode.NoContent);
            var (client, _, _) = Create(handler);

            await client.PutAsync("api/admin/settings", new UpdateSettingRequestDto { Chave = "x", Valor = "y" });

            var call = Assert.Single(handler.Calls);
            Assert.Equal(HttpMethod.Put, call.Method);
            Assert.Contains("\"chave\":\"x\"", call.Body);
            Assert.Contains("\"valor\":\"y\"", call.Body);
        }

        [Fact]
        public async Task DeleteAsync_SendsDeleteRequest()
        {
            var handler = new StubHttpHandler().Enqueue(HttpStatusCode.NoContent);
            var (client, _, _) = Create(handler);

            await client.DeleteAsync("api/admin/gamification/badges/3");

            var call = Assert.Single(handler.Calls);
            Assert.Equal(HttpMethod.Delete, call.Method);
        }

        [Fact]
        public async Task Unauthorized_WhenRefreshSucceeds_RetriesOnceWithNewToken()
        {
            var handler = new StubHttpHandler()
                .Enqueue(HttpStatusCode.Unauthorized)
                .Enqueue(HttpStatusCode.OK, "{\"chave\":\"a\",\"valor\":\"b\"}");
            var (client, auth, _) = Create(handler);
            auth.Setup(a => a.TryRefreshAsync(It.IsAny<CancellationToken>()))
                .Callback(() => auth.Object.CurrentToken = "token-2")
                .ReturnsAsync(true);

            var result = await client.GetAsync<SettingDto>("api/admin/settings");

            Assert.Equal("a", result!.Chave);
            Assert.Equal(2, handler.Calls.Count);
            Assert.Equal("Bearer token-1", handler.Calls[0].Authorization);
            Assert.Equal("Bearer token-2", handler.Calls[1].Authorization);
            auth.Verify(a => a.NotifySessionExpired(), Times.Never);
        }

        [Fact]
        public async Task Unauthorized_WhenRefreshFails_NotifiesSessionExpiredAndThrows()
        {
            var handler = new StubHttpHandler().Enqueue(HttpStatusCode.Unauthorized);
            var (client, auth, _) = Create(handler);
            auth.Setup(a => a.TryRefreshAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<SettingDto>("api/admin/settings"));

            Assert.Equal(ApiErrorKind.Unauthorized, ex.Kind);
            auth.Verify(a => a.NotifySessionExpired(), Times.Once);
            Assert.Single(handler.Calls);
        }

        [Fact]
        public async Task Unauthorized_AfterRetry_DoesNotLoopForever()
        {
            var handler = new StubHttpHandler()
                .Enqueue(HttpStatusCode.Unauthorized)
                .Enqueue(HttpStatusCode.Unauthorized);
            var (client, auth, _) = Create(handler);
            auth.Setup(a => a.TryRefreshAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<SettingDto>("api/x"));

            Assert.Equal(ApiErrorKind.Unauthorized, ex.Kind);
            Assert.Equal(2, handler.Calls.Count);
            auth.Verify(a => a.TryRefreshAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BadRequest_ThrowsValidationExceptionWithApiMessage()
        {
            var handler = new StubHttpHandler().Enqueue(HttpStatusCode.BadRequest, "\"TipoRequisito inválido.\"");
            var (client, _, _) = Create(handler);

            var ex = await Assert.ThrowsAsync<ApiException>(() => client.PostAsync("api/x", new { A = 1 }));

            Assert.Equal(ApiErrorKind.Validation, ex.Kind);
            Assert.Equal("TipoRequisito inválido.", ex.Message);
        }

        [Theory]
        [InlineData(HttpStatusCode.Forbidden, ApiErrorKind.Forbidden)]
        [InlineData(HttpStatusCode.NotFound, ApiErrorKind.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError, ApiErrorKind.Server)]
        public async Task ErrorStatusCodes_AreTranslatedToApiException(HttpStatusCode status, ApiErrorKind expected)
        {
            var handler = new StubHttpHandler().Enqueue(status);
            var (client, _, _) = Create(handler);

            var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<SettingDto>("api/x"));

            Assert.Equal(expected, ex.Kind);
        }

        [Fact]
        public async Task ConnectionFailure_ThrowsNetworkApiException()
        {
            var handler = new StubHttpHandler().EnqueueException(new HttpRequestException("sem rede"));
            var (client, _, _) = Create(handler);

            var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<SettingDto>("api/x"));

            Assert.Equal(ApiErrorKind.Network, ex.Kind);
            Assert.Contains("conectar", ex.Message);
        }

        [Fact]
        public async Task Timeout_ThrowsTimeoutApiException()
        {
            var handler = new StubHttpHandler().EnqueueException(new TaskCanceledException("timeout"));
            var (client, _, _) = Create(handler);

            var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<SettingDto>("api/x"));

            Assert.Equal(ApiErrorKind.Timeout, ex.Kind);
        }

        [Fact]
        public async Task UserCancellation_PropagatesOperationCanceledException()
        {
            var handler = new StubHttpHandler().EnqueueException(new TaskCanceledException("cancelado"));
            var (client, _, _) = Create(handler);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync<SettingDto>("api/x", cts.Token));
        }

        [Fact]
        public async Task MalformedJson_ThrowsApiExceptionInsteadOfJsonException()
        {
            var handler = new StubHttpHandler().Enqueue(HttpStatusCode.OK, "isto não é json");
            var (client, _, _) = Create(handler);

            var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<SettingDto>("api/x"));

            Assert.Equal(ApiErrorKind.Unknown, ex.Kind);
        }
    }
}
