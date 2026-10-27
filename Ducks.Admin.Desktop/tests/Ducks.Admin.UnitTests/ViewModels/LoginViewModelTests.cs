using Ducks.Admin.Desktop.Models;
using Ducks.Admin.Desktop.Services;
using Ducks.Admin.Desktop.ViewModels;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Ducks.Admin.UnitTests.ViewModels
{
    public class LoginViewModelTests
    {
        [Fact]
        public async Task LoginCommand_WithEmptyFields_ShouldShowError()
        {
            // Arrange
            var mockClient = new Mock<IApiClient>();
            var vm = new LoginViewModel(mockClient.Object)
            {
                Email = string.Empty,
                Senha = string.Empty
            };

            // Act
            await vm.LoginCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal("Preencha o e-mail e a senha.", vm.ErrorMessage);
        }

        [Fact]
        public async Task LoginCommand_WithValidCredentials_ShouldSucceedAndStoreToken()
        {
            // Arrange
            var mockClient = new Mock<IApiClient>();
            mockClient.Setup(c => c.LoginAsync("admintest@ducks.com", "SenhaForte123!"))
                      .ReturnsAsync(new AuthResponseDto { Token = "mock-jwt-token-xyz", Sucesso = true, Mensagem = "Ok" });

            var vm = new LoginViewModel(mockClient.Object)
            {
                Email = "admintest@ducks.com",
                Senha = "SenhaForte123!"
            };

            // Act
            await vm.LoginCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal("Login realizado com sucesso!", vm.ErrorMessage);
            mockClient.Verify(c => c.LoginAsync("admintest@ducks.com", "SenhaForte123!"), Times.Once);
        }
    }
}