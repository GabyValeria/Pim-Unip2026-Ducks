using System.Threading.Tasks;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.Core.Models;
using Moq;
using Xunit;

namespace Ducks.Desktop.Tests.ViewModels
{
    public class AiAutomationViewModelTests
    {
        [Fact]
        public async Task ExecutarAssistenteCommand_WithValidContext_ReturnsAiResult()
        {
            // Arrange
            var apiServiceMock = new Mock<IAiAutomationApiService>();
            apiServiceMock.Setup(s => s.ProcessarTarefaAdminAsync(It.IsAny<AiPromptRequestDto>()))
                .ReturnsAsync(new AiResponseDto
                {
                    Sucesso = true,
                    ResultadoGerado = "Relatório sintético gerado com sucesso pela IA."
                });

            var dialogServiceMock = new Mock<IDialogService>();

            var viewModel = new AiAutomationViewModel(apiServiceMock.Object, dialogServiceMock.Object)
            {
                ContextoEntrada = "Dados de teste para moderação pendente."
            };

            // Act
            await viewModel.ExecutarAssistenteCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal("Relatório sintético gerado com sucesso pela IA.", viewModel.ResultadoIa);
            Assert.False(viewModel.IsLoading);
            apiServiceMock.Verify(s => s.ProcessarTarefaAdminAsync(It.Is<AiPromptRequestDto>(req => req.Contexto == "Dados de teste para moderação pendente.")), Times.Once);
        }
    }
}