using System.Net.Http;
using Moq;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels;
using Xunit;

namespace Ducks.Desktop.Tests
{
    public class MetricsDashboardViewModelTests
    {
        [Fact]
        public void Deve_Inicializar_Com_Valores_Padrao()
        {
            // Arrange
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var authServiceMock = new Mock<IAuthApiService>();

            // Act
            var viewModel = new MetricsDashboardViewModel(httpClientFactoryMock.Object, authServiceMock.Object);

            // Assert
            Assert.Equal(6, viewModel.TotalUsuariosAtivos);
            Assert.Equal("88.5%", viewModel.TaxaRetencao);
            Assert.Equal(0, viewModel.ModercaoPendente);
            Assert.Equal("Todos", viewModel.FiltroSelecionado);
            Assert.NotNull(viewModel.OpcoesFiltroPapeis);
            Assert.Contains("Alunos", viewModel.OpcoesFiltroPapeis);
        }

        [Fact]
        public void Deve_Atualizar_Total_Usuarios_Quando_Filtro_Mudar()
        {
            // Arrange
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var authServiceMock = new Mock<IAuthApiService>();
            var viewModel = new MetricsDashboardViewModel(httpClientFactoryMock.Object, authServiceMock.Object);

            // Act
            viewModel.FiltroSelecionado = "Alunos";

            // Assert
            Assert.Equal(4, viewModel.TotalUsuariosAtivos);
            
            // Act
            viewModel.FiltroSelecionado = "Moderadores";

            // Assert
            Assert.Equal(1, viewModel.TotalUsuariosAtivos);
        }
    }
}