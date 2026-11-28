using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.Core.Models;
using Moq;

namespace Ducks.Desktop.Tests
{
    public class UserManagementViewModelTests
    {
        private readonly Mock<IUserAdminApiService> _users = new();
        private readonly Mock<IDialogService> _dialog = new();
        private readonly Mock<IAuthApiService> _auth = new();

        private UserManagementViewModel Create() => new(_users.Object, _dialog.Object, _auth.Object);

        private static UserDto U(int id, string nome, bool bloqueado = false, params string[] roles) => new()
        {
            UsuarioID = id,
            NomeUsuario = nome,
            Email = $"{nome.ToLowerInvariant()}@ducks.com",
            IsBlocked = bloqueado,
            Roles = roles
        };

        private void SetupPage(IEnumerable<UserDto> items, int total)
        {
            var lista = items.ToList();
            _users.Setup(s => s.GetUsersAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((string? q, int p, int ps, CancellationToken ct) =>
                    Task.FromResult(new PagedResult<UserDto> { Page = p, PageSize = ps, Total = total, Items = lista }));
        }

        [Fact]
        public async Task InitializeAsync_LoadsFirstPage()
        {
            SetupPage(new[] { U(1, "Ana", false, "Admin"), U(2, "Beto", false, "Aluno") }, 2);
            var vm = Create();

            await vm.InitializeAsync();

            Assert.Equal(2, vm.Usuarios.Count);
            Assert.Equal(2, vm.TotalRegistros);
            Assert.Equal(1, vm.TotalPaginas);
            Assert.Equal("Página 1 de 1 · 2 usuário(s)", vm.TextoPaginacao);
            Assert.False(vm.IsLoading);
            Assert.False(vm.HasError);
            _users.Verify(s => s.GetUsersAsync(It.IsAny<string?>(), 1, UserManagementViewModel.TamanhoPagina, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WithNoUsers_ShowsEmptyMessage()
        {
            SetupPage(Array.Empty<UserDto>(), 0);
            var vm = Create();

            await vm.InitializeAsync();

            Assert.Empty(vm.Usuarios);
            Assert.Equal("Nenhum usuário encontrado.", vm.StatusMessage);
        }

        [Fact]
        public async Task RoleFilter_ShowsOnlyUsersWithSelectedRole()
        {
            SetupPage(new[] { U(1, "Ana", false, "Admin"), U(2, "Beto", false, "Aluno") }, 2);
            var vm = Create();
            await vm.InitializeAsync();

            Assert.Contains("Admin", vm.OpcoesFiltroPapeis);
            Assert.Contains("Aluno", vm.OpcoesFiltroPapeis);

            vm.FiltroPapelSelecionado = "Admin";

            var unico = Assert.Single(vm.Usuarios);
            Assert.Equal("Ana", unico.NomeUsuario);
        }

        [Fact]
        public async Task Pagination_NavigatesBetweenPages()
        {
            SetupPage(new[] { U(1, "Ana") }, 60);
            var vm = Create();
            await vm.InitializeAsync();

            Assert.Equal(3, vm.TotalPaginas);
            Assert.False(vm.PaginaAnteriorCommand.CanExecute(null));
            Assert.True(vm.ProximaPaginaCommand.CanExecute(null));

            await vm.ProximaPaginaCommand.ExecuteAsync(null);

            Assert.Equal(2, vm.PaginaAtual);
            Assert.True(vm.PaginaAnteriorCommand.CanExecute(null));
            _users.Verify(s => s.GetUsersAsync(It.IsAny<string?>(), 2, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Search_RestartsFromFirstPageWithTerm()
        {
            SetupPage(new[] { U(1, "Ana") }, 1);
            var vm = Create();
            vm.FiltroTermo = "ana";

            await vm.PesquisarCommand.ExecuteAsync(null);

            _users.Verify(s => s.GetUsersAsync("ana", 1, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ApiFailure_ShowsFriendlyErrorAndStopsLoading()
        {
            _users.Setup(s => s.GetUsersAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Network, "Sem conexão com a API."));
            var vm = Create();

            await vm.InitializeAsync();

            Assert.True(vm.HasError);
            Assert.Equal("Sem conexão com a API.", vm.StatusMessage);
            Assert.False(vm.IsLoading);
        }

        [Fact]
        public async Task UnexpectedFailure_DoesNotCrashAndReportsError()
        {
            _users.Setup(s => s.GetUsersAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));
            var vm = Create();

            await vm.InitializeAsync();

            Assert.True(vm.HasError);
            Assert.Contains("boom", vm.StatusMessage);
        }

        [Fact]
        public async Task ToggleBlock_OwnAccount_IsRefusedWithoutCallingApi()
        {
            _auth.SetupGet(a => a.CurrentUserId).Returns(1);
            var vm = Create();

            await vm.AlternarBloqueioCommand.ExecuteAsync(U(1, "Ana"));

            Assert.True(vm.HasError);
            _dialog.Verify(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _users.Verify(s => s.SetBlockedAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ToggleBlock_WhenUserCancelsConfirmation_DoesNothing()
        {
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            var vm = Create();

            await vm.AlternarBloqueioCommand.ExecuteAsync(U(2, "Beto"));

            _users.Verify(s => s.SetBlockedAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ToggleBlock_Confirmed_BlocksAndReloadsList()
        {
            SetupPage(new[] { U(2, "Beto") }, 1);
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            var vm = Create();

            await vm.AlternarBloqueioCommand.ExecuteAsync(U(2, "Beto"));

            _users.Verify(s => s.SetBlockedAsync(2, true, It.IsAny<CancellationToken>()), Times.Once);
            _users.Verify(s => s.GetUsersAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.False(vm.HasError);
            Assert.Contains("bloqueado", vm.StatusMessage);
        }

        [Fact]
        public async Task ToggleBlock_OnBlockedUser_Unblocks()
        {
            SetupPage(new[] { U(2, "Beto") }, 1);
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            var vm = Create();

            await vm.AlternarBloqueioCommand.ExecuteAsync(U(2, "Beto", bloqueado: true));

            _users.Verify(s => s.SetBlockedAsync(2, false, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains("desbloqueado", vm.StatusMessage);
        }

        [Fact]
        public async Task ToggleBlock_WhenApiFails_ShowsErrorAndDoesNotReload()
        {
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _users.Setup(s => s.SetBlockedAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Forbidden, "Sem permissão."));
            var vm = Create();

            await vm.AlternarBloqueioCommand.ExecuteAsync(U(2, "Beto"));

            Assert.True(vm.HasError);
            Assert.Equal("Sem permissão.", vm.StatusMessage);
            _users.Verify(s => s.GetUsersAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    public class RoleManagementViewModelTests
    {
        private readonly Mock<IUserAdminApiService> _users = new();
        private readonly Mock<IRoleApiService> _roles = new();
        private readonly Mock<IDialogService> _dialog = new();
        private readonly Mock<IAuthApiService> _auth = new();

        private RoleManagementViewModel Create() => new(_users.Object, _roles.Object, _dialog.Object, _auth.Object);

        private void SetupData()
        {
            _roles.Setup(r => r.GetRolesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lists.Of(new RoleDto { PapelID = 1, NomePapel = "Admin" }, new RoleDto { PapelID = 2, NomePapel = "Curador" }));
            _users.Setup(u => u.GetUsersAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<UserDto>
                {
                    Total = 2,
                    Items = new List<UserDto>
                    {
                        new() { UsuarioID = 1, NomeUsuario = "Ana", Roles = new[] { "Admin" } },
                        new() { UsuarioID = 9, NomeUsuario = "Beto", Roles = new[] { "Aluno" } }
                    }
                });
        }

        [Fact]
        public async Task InitializeAsync_LoadsUsersAndRoles()
        {
            SetupData();
            var vm = Create();

            await vm.InitializeAsync();

            Assert.Equal(2, vm.Usuarios.Count);
            Assert.Equal(2, vm.Papeis.Count);
            Assert.False(vm.HasError);
        }

        [Fact]
        public async Task AssignRole_WithoutSelection_ShowsError()
        {
            var vm = Create();

            await vm.AtribuirPapelCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _roles.Verify(r => r.AssignRoleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AssignRole_ToOwnAccount_IsRefused()
        {
            SetupData();
            _auth.SetupGet(a => a.CurrentUserId).Returns(1);
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedUsuario = vm.Usuarios.First(u => u.UsuarioID == 1);
            vm.SelectedPapel = vm.Papeis.First(p => p.PapelID == 2);

            await vm.AtribuirPapelCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _roles.Verify(r => r.AssignRoleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AssignRole_WhenCancelled_DoesNotCallApi()
        {
            SetupData();
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedUsuario = vm.Usuarios.First(u => u.UsuarioID == 9);
            vm.SelectedPapel = vm.Papeis.First(p => p.PapelID == 2);

            await vm.AtribuirPapelCommand.ExecuteAsync(null);

            _roles.Verify(r => r.AssignRoleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AssignRole_Confirmed_CallsApiAndReloads()
        {
            SetupData();
            _auth.SetupGet(a => a.CurrentUserId).Returns(1);
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedUsuario = vm.Usuarios.First(u => u.UsuarioID == 9);
            vm.SelectedPapel = vm.Papeis.First(p => p.PapelID == 2);

            await vm.AtribuirPapelCommand.ExecuteAsync(null);

            _roles.Verify(r => r.AssignRoleAsync(9, 2, It.IsAny<CancellationToken>()), Times.Once);
            _users.Verify(u => u.GetUsersAsync(It.IsAny<string?>(), 1, 100, It.IsAny<CancellationToken>()), Times.Exactly(2));
            Assert.False(vm.HasError);
            Assert.Contains("Curador", vm.StatusMessage);
        }

        [Fact]
        public async Task AssignRole_WhenApiFails_ShowsError()
        {
            SetupData();
            _auth.SetupGet(a => a.CurrentUserId).Returns(1);
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _roles.Setup(r => r.AssignRoleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.NotFound, "Usuário ou papel não encontrado."));
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedUsuario = vm.Usuarios.First(u => u.UsuarioID == 9);
            vm.SelectedPapel = vm.Papeis.First(p => p.PapelID == 2);

            await vm.AtribuirPapelCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Equal("Usuário ou papel não encontrado.", vm.StatusMessage);
        }
    }
}
