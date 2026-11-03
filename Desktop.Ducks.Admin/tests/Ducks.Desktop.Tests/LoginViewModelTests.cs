using System.Threading.Tasks;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.Core.Models;
using Moq;
using Xunit;

namespace Ducks.Desktop.Tests.ViewModels
{
    public class LoginViewModelTests
    {
        [Fact]
        public async Task LoginCommand_WithValidCredentials_InvokesSuccessCallback()
        {
            // Arrange
            var authServiceMock = new Mock<IAuthApiService>();
            authServiceMock.Setup(s => s.LoginAsync("admin@ducks.com", "Admin123!"))
                .ReturnsAsync(new AuthResponseDto { Sucesso = true, Token = "token-abc" });

            bool successCallbackInvoked = false;
            var viewModel = new LoginViewModel(authServiceMock.Object, () => successCallbackInvoked = true)
            {
                Email = "admin@ducks.com",
                Senha = "Admin123!"
            };

            // Act
            await viewModel.LoginCommand.ExecuteAsync(null);

            // Assert
            Assert.True(successCallbackInvoked);
            Assert.False(viewModel.HasError);
            Assert.Equal(string.Empty, viewModel.MensagemErro);
        }

        [Fact]
        public async Task LoginCommand_WithInvalidCredentials_ShowsErrorMessage()
        {
            // Arrange
            var authServiceMock = new Mock<IAuthApiService>();
            authServiceMock.Setup(s => s.LoginAsync("wrong@ducks.com", "senhaerrada"))
                .ReturnsAsync(new AuthResponseDto { Sucesso = false, Mensagem = "Credenciais inválidas." });

            var viewModel = new LoginViewModel(authServiceMock.Object, () => { })
            {
                Email = "wrong@ducks.com",
                Senha = "senhaerrada"
            };

            // Act
            await viewModel.LoginCommand.ExecuteAsync(null);

            // Assert
            Assert.True(viewModel.HasError);
            Assert.Equal("Credenciais inválidas.", viewModel.MensagemErro);
        }
    }
}