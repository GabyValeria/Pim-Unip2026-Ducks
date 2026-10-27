using Ducks.Admin.Desktop.Models;
using Ducks.Admin.Desktop.Services;
using Ducks.Admin.Desktop.ViewModels;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Ducks.Admin.UnitTests.ViewModels
{
    public class UsersViewModelTests
    {
        [Fact]
        public async Task LoadUsersCommand_ShouldPopulateUsuariosCollection()
        {
            // Arrange
            var mockClient = new Mock<IApiClient>();
            var sampleList = new List<UserAdminDto>
            {
                new UserAdminDto { UsuarioID = 1, NomeUsuario = "Aluno Teste", Email = "aluno@ducks.com", EstaAtivo = true }
            };
            mockClient.Setup(c => c.GetUsersAsync()).ReturnsAsync(sampleList);

            var vm = new UsersViewModel(mockClient.Object);

            // Act
            await vm.LoadUsersCommand.ExecuteAsync(null);

            // Assert
            Assert.Single(vm.Usuarios);
            Assert.Equal("Aluno Teste", vm.Usuarios[0].NomeUsuario);
            Assert.Equal("1 usuários carregados.", vm.StatusMessage);
        }
    }
}