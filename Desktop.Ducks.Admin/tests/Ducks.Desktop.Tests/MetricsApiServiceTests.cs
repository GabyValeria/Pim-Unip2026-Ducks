using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Ducks.Desktop.Application.Services;
using Ducks.Desktop.Core.Models;
using Moq;
using Moq.Protected;
using Xunit;

namespace Ducks.Desktop.Tests.Services
{
    public class MetricsApiServiceTests
    {
        [Fact]
        public async Task GetDashboardMetricsAsync_ReturnsValidMetricsDto()
        {
            // Arrange
            var expectedMetrics = new DashboardMetricsDto
            {
                TotalUsuariosAtivos = 500,
                TaxaRetencaoMedia = 91.2,
                TotalXpDistribuido = 12500
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(expectedMetrics)
                });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new System.Uri("https://localhost:5058/")
            };

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(f => f.CreateClient("DucksApi")).Returns(httpClient);

            var authServiceMock = new Mock<Ducks.Desktop.Application.Interfaces.IAuthApiService>();
            authServiceMock.Setup(a => a.CurrentToken).Returns("fake-jwt-token");

            var metricsService = new MetricsApiService(httpClientFactoryMock.Object, authServiceMock.Object);

            // Act
            var result = await metricsService.GetDashboardMetricsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(500, result.TotalUsuariosAtivos);
            Assert.Equal(91.2, result.TaxaRetencaoMedia);
        }
    }
}