using Ducks.Admin.Desktop.ViewModels;
using Xunit;

namespace Ducks.Admin.UnitTests.ViewModels
{
    public class MainViewModelTests
    {
        [Fact]
        public void InitialState_ShouldHaveCorrectTitle()
        {
            // Arrange & Act
            var viewModel = new MainViewModel();

            // Assert
            Assert.Equal("Ducks Admin Desktop - Painel Administrativo", viewModel.ApplicationTitle);
        }

        [Fact]
        public void RefreshConnectionCommand_ShouldUpdateStatusMessage()
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act
            viewModel.RefreshConnectionCommand.Execute(null);

            // Assert
            Assert.Equal("Conexão verificada com a API backend da plataforma Ducks.", viewModel.StatusMessage);
        }
    }
}