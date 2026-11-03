using System.Collections.Generic;
using System.Threading.Tasks;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.Core.Models;
using Moq;
using Xunit;

namespace Ducks.Desktop.Tests.ViewModels
{
    public class ModerationViewModelTests
    {
        [Fact]
        public async Task CarregarFilaCommand_PopulatesFilaModeracaoCollection()
        {
            // Arrange
            var apiServiceMock = new Mock<IModerationApiService>();
            apiServiceMock.Setup(s => s.GetPendingQueueAsync())
                .ReturnsAsync(new List<ModerationItemDto>
                {
                    new ModerationItemDto { ItemID = 10, TipoConteudo = "Denuncia", TituloOuDescricao = "Conteúdo impróprio", Autor = "UserX" }
                });

            var viewModel = new ModerationViewModel(apiServiceMock.Object);

            // Act
            await viewModel.CarregarFilaCommand.ExecuteAsync(null);

            // Assert
            Assert.Single(viewModel.FilaModeracao);
            Assert.Equal("Denuncia", viewModel.FilaModeracao[0].TipoConteudo);
            Assert.False(viewModel.IsLoading);
        }
    }
}