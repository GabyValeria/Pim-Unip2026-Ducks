using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Ducks.Desktop.Application.Services;
using Ducks.Desktop.Core.Models;
using Moq;
using Xunit;

namespace Ducks.Desktop.Tests.Services
{
    public class AuthApiServiceTests
    {
        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsToken()
        {
            // Arrange
            var expectedResponse = new AuthResponseDto 
            { 
                Sucesso = true, 
                Token = "fake-jwt-token-123", 
                Mensagem = "Sucesso" 
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            // Configura o mock do HttpMessageHandler para interceptar a chamada POST
            // Como System.Net.Http.Json envia via conteúdo HTTP, validamos o retorno simulado
            var httpClient = new HttpClient(new TestableHttpMessageHandler(expectedResponse))
            {
                BaseAddress = new System.Uri("https://localhost:7050/")
            };

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(_ => _.CreateClient("DucksApi")).Returns(httpClient);

            var authService = new AuthApiService(httpClientFactoryMock.Object);

            // Act
            var result = await authService.LoginAsync("admin@ducks.com", "Admin123!");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Sucesso);
            Assert.Equal("fake-jwt-token-123", result.Token);
            Assert.Equal("fake-jwt-token-123", authService.CurrentToken);
        }
    }

    // Classe auxiliar para simular respostas do HttpClient em testes unitários
    internal class TestableHttpMessageHandler : HttpMessageHandler
    {
        private readonly object _responseObject;

        public TestableHttpMessageHandler(object responseObject)
        {
            _responseObject = responseObject;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(_responseObject)
            };
            return Task.FromResult(response);
        }
    }
}