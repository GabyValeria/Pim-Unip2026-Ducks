using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.Core.Models;
using Xunit;

namespace Ducks.Desktop.Tests
{
    public class UserManagementViewModelTests
    {
        [Fact]
        public async Task Deve_Carregar_Usuarios_Com_Sucesso()
        {
            // Arrange
            var apiServiceMock = new Mock<IUserAdminApiService>();
            apiServiceMock.Setup(s => s.GetUsersAsync())
                .ReturnsAsync(new List<UserDto>
                {
                    new UserDto { UsuarioID = 1, NomeUsuario = "Teste Aluno", Email = "aluno@ducks.com", Role = "Aluno", IsBlocked = false }
                });

            var dialogServiceMock = new Mock<IDialogService>();

            // Act
            var viewModel = new UserManagementViewModel(apiServiceMock.Object, dialogServiceMock.Object);
            // Aguarda o carregamento assíncrono do construtor
            await Task.Delay(100);

            // Assert
            Assert.Single(viewModel.Usuarios);
            Assert.Equal("Teste Aluno", viewModel.Usuarios[0].NomeUsuario);
            Assert.Equal("Usuários carregados com sucesso.", viewModel.MensagemStatus);
        }
    }
}