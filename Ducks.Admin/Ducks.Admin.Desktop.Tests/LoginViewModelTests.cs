using Xunit;
using Moq;
using System.Net.Http;
using Ducks.Admin.Desktop.ViewModels;
using Ducks.Admin.Desktop.Services;
using Ducks.Admin.Desktop.Stores;

namespace Ducks.Admin.Desktop.Tests;

public class LoginViewModelTests
{
    private readonly Mock<IAuthApiService> _authApiMock;
    private readonly UserSessionStore _userSessionStore;
    private readonly NavigationStore _navigationStore;
    private readonly Mock<IAdminApiService> _adminApiMock;

    public LoginViewModelTests()
    {
        _authApiMock = new Mock<IAuthApiService>();
        _adminApiMock = new Mock<IAdminApiService>();
        _userSessionStore = new UserSessionStore();
        _navigationStore = new NavigationStore();
    }

    [Fact]
    public async Task LoginAsync_CredenciaisValidas_DeveDefinirSessaoENavegar()
    {
        // Arrange
        var requestEsperado = new LoginRequestDto("admin@ducks.com", "123456");
        var responseFake = new LoginResponseDto("token-jwt-fake-12345", "Sucesso");

        _authApiMock
            .Setup(a => a.LoginAsync(It.Is<LoginRequestDto>(r => r.Email == requestEsperado.Email)))
            .ReturnsAsync(responseFake);

        var dashboardViewModel = new DashboardViewModel();
        var viewModel = new LoginViewModel(_authApiMock.Object, _userSessionStore, _navigationStore, dashboardViewModel)
        {
            Email = "admin@ducks.com",
            Senha = "123456"
        };

        // Act
        await viewModel.LoginCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_userSessionStore.IsLoggedIn);
        Assert.Equal("token-jwt-fake-12345", _userSessionStore.JwtToken);
        Assert.Equal(dashboardViewModel, _navigationStore.CurrentViewModel);
        Assert.Null(viewModel.MensagemErro);
    }

    [Fact]
    public async Task LoginAsync_FalhaNaApi_DeveExibirMensagemDeErro()
    {
        // Arrange
        _authApiMock
            .Setup(a => a.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ThrowsAsync(new HttpRequestException("Não autorizado"));

        var dashboardViewModel = new DashboardViewModel();
        var viewModel = new LoginViewModel(_authApiMock.Object, _userSessionStore, _navigationStore, dashboardViewModel)
        {
            Email = "invalido@ducks.com",
            Senha = "errada"
        };

        // Act
        await viewModel.LoginCommand.ExecuteAsync(null);

        // Assert
        Assert.False(_userSessionStore.IsLoggedIn);
        Assert.NotNull(viewModel.MensagemErro);
        Assert.Contains("Falha ao autenticar", viewModel.MensagemErro);
    }
}