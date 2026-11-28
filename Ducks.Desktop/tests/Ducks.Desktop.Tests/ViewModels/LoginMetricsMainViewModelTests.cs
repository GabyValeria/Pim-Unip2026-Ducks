using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.Core.Models;
using Moq;

namespace Ducks.Desktop.Tests
{
    public class LoginViewModelTests
    {
        private readonly Mock<IAuthApiService> _auth = new();

        [Theory]
        [InlineData("", "senha")]
        [InlineData("ana@ducks.com", "")]
        [InlineData("   ", "   ")]
        public async Task Login_WithMissingFields_ShowsErrorWithoutCallingApi(string email, string senha)
        {
            var vm = new LoginViewModel(_auth.Object) { Email = email, Senha = senha };

            await vm.LoginCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Contains("preencha", vm.MensagemErro);
            _auth.Verify(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("sem-arroba")]
        [InlineData("a@")]
        [InlineData("ana @ducks.com")]
        public async Task Login_WithInvalidEmail_ShowsErrorWithoutCallingApi(string email)
        {
            var vm = new LoginViewModel(_auth.Object) { Email = email, Senha = "x" };

            await vm.LoginCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Contains("e-mail válido", vm.MensagemErro);
            _auth.Verify(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Login_Success_InvokesCallbackAndClearsError()
        {
            _auth.Setup(a => a.LoginAsync("ana@ducks.com", "Senha123!")).ReturnsAsync(new AuthResponseDto { Token = "jwt" });
            var chamado = 0;
            var vm = new LoginViewModel(_auth.Object, () => chamado++) { Email = "ana@ducks.com", Senha = "Senha123!" };

            await vm.LoginCommand.ExecuteAsync(null);

            Assert.Equal(1, chamado);
            Assert.False(vm.HasError);
            Assert.False(vm.IsLoading);
        }

        [Fact]
        public async Task Login_WithApiFailure_ShowsApiMessageAndDoesNotNavigate()
        {
            _auth.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AuthResponseDto { Mensagem = "Conta bloqueada." });
            var chamado = 0;
            var vm = new LoginViewModel(_auth.Object, () => chamado++) { Email = "ana@ducks.com", Senha = "x" };

            await vm.LoginCommand.ExecuteAsync(null);

            Assert.Equal(0, chamado);
            Assert.True(vm.HasError);
            Assert.Equal("Conta bloqueada.", vm.MensagemErro);
        }

        [Fact]
        public async Task Login_WithNullResponse_ShowsDefaultMessage()
        {
            _auth.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((AuthResponseDto?)null);
            var vm = new LoginViewModel(_auth.Object) { Email = "ana@ducks.com", Senha = "x" };

            await vm.LoginCommand.ExecuteAsync(null);

            Assert.Equal("E-mail ou senha inválidos.", vm.MensagemErro);
        }

        [Fact]
        public async Task Login_WhenServiceThrows_ShowsErrorAndStopsLoading()
        {
            _auth.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("falha"));
            var vm = new LoginViewModel(_auth.Object) { Email = "ana@ducks.com", Senha = "x" };

            await vm.LoginCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Contains("falha", vm.MensagemErro);
            Assert.False(vm.IsLoading);
        }
    }

    public class MetricsDashboardViewModelTests
    {
        private readonly Mock<IMetricsApiService> _metrics = new();
        private readonly Mock<IRoleApiService> _roles = new();

        private MetricsDashboardViewModel Create(DashboardMetricsDto metricas)
        {
            _metrics.Setup(m => m.GetDashboardMetricsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(metricas);
            _roles.Setup(r => r.GetRolesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lists.Of(new RoleDto { PapelID = 1, NomePapel = "Admin", Descricao = "Acesso completo" }));
            return new MetricsDashboardViewModel(_metrics.Object, _roles.Object);
        }

        [Theory]
        [InlineData(0L, "0")]
        [InlineData(999L, "999")]
        [InlineData(1000L, "1k")]
        [InlineData(12400L, "12.4k")]
        [InlineData(1500000L, "1.5M")]
        public void FormatCompact_AbbreviatesLargeNumbers(long valor, string esperado)
        {
            Assert.Equal(esperado, MetricsDashboardViewModel.FormatarCompacto(valor));
        }

        [Fact]
        public async Task Initialize_PopulatesCardsAndBars()
        {
            var vm = Create(new DashboardMetricsDto
            {
                TotalUsuarios = 100,
                TotalUsuariosAtivos = 80,
                TotalTrilhas = 5,
                TotalAulasCadastradas = 40,
                TotalXpDistribuido = 12000,
                ItensPendentesModeracao = 3
            });

            await vm.InitializeAsync();

            Assert.Equal("100", vm.TotalUsuariosTexto);
            Assert.Equal("80% ativos", vm.UsuariosAtivosPill);
            Assert.Equal("40", vm.TotalAulasTexto);
            Assert.Equal("5 trilha(s)", vm.TrilhasPill);
            Assert.Equal("3", vm.PendenciasTexto);
            Assert.Equal("Aguardando revisão", vm.PendenciasPill);
            Assert.Equal("12k XP distribuídos", vm.XpDistribuidoTexto);
            Assert.Equal(5, vm.Barras.Count);
            Assert.Single(vm.Papeis);
            Assert.False(vm.HasError);
        }

        [Fact]
        public async Task Bars_AreScaledToTheLargestValueWithMinimumHeight()
        {
            var vm = Create(new DashboardMetricsDto { TotalUsuarios = 100, TotalUsuariosAtivos = 50, ItensPendentesModeracao = 1 });

            await vm.InitializeAsync();

            var usuarios = vm.Barras.First(b => b.Rotulo == "Usuários");
            var ativos = vm.Barras.First(b => b.Rotulo == "Ativos");
            var pendentes = vm.Barras.First(b => b.Rotulo == "Pendentes");
            Assert.Equal(MetricsDashboardViewModel.AlturaMaximaBarra, usuarios.Altura);
            Assert.Equal(MetricsDashboardViewModel.AlturaMaximaBarra / 2, ativos.Altura, 5);
            Assert.True(pendentes.Altura >= 8); // altura mínima para a barra continuar visível
        }

        [Fact]
        public async Task Initialize_WithEmptyPlatform_DoesNotDivideByZero()
        {
            var vm = Create(new DashboardMetricsDto());

            await vm.InitializeAsync();

            Assert.Equal("0", vm.TotalUsuariosTexto);
            Assert.Equal("0% ativos", vm.UsuariosAtivosPill);
            Assert.Equal("Fila em dia", vm.PendenciasPill);
            Assert.All(vm.Barras, b => Assert.True(b.Altura > 0));
        }

        [Fact]
        public async Task Initialize_WhenRolesFail_StillShowsMetrics()
        {
            var vm = Create(new DashboardMetricsDto { TotalUsuarios = 10 });
            _roles.Setup(r => r.GetRolesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Server, "erro"));

            await vm.InitializeAsync();

            Assert.Equal("10", vm.TotalUsuariosTexto);
            Assert.Empty(vm.Papeis);
            Assert.False(vm.HasError);
        }

        [Fact]
        public async Task Initialize_WhenMetricsFail_ShowsError()
        {
            var vm = Create(new DashboardMetricsDto());
            _metrics.Setup(m => m.GetDashboardMetricsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Network, "Sem conexão."));

            await vm.InitializeAsync();

            Assert.True(vm.HasError);
            Assert.Equal("Sem conexão.", vm.StatusMessage);
            Assert.Equal("—", vm.TotalUsuariosTexto);
        }
    }

    public class MainViewModelTests
    {
        private readonly Mock<IServiceProvider> _services = new();
        private readonly Mock<IAuthApiService> _auth = new();
        private readonly Mock<IDialogService> _dialog = new();
        private readonly Mock<IAuditApiService> _audit = new();

        private MainViewModel Create() => new(_services.Object, _auth.Object, _dialog.Object);

        private AuditLogsViewModel RegisterAuditViewModel()
        {
            _audit.Setup(a => a.GetLogsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Lists.Of<AuditLogDto>());
            var vm = new AuditLogsViewModel(_audit.Object);
            _services.Setup(s => s.GetService(typeof(AuditLogsViewModel))).Returns(vm);
            return vm;
        }

        [Fact]
        public async Task Navigate_SetsCurrentModuleTitlesAndInitializesIt()
        {
            var audit = RegisterAuditViewModel();
            var main = Create();

            await main.NavegarAuditoriaLogsCommand.ExecuteAsync(null);

            Assert.Same(audit, main.ViewModelAtual);
            Assert.Equal("Relatórios e Auditoria", main.TituloPagina);
            Assert.Equal(nameof(AuditLogsViewModel), main.ModuloAtivo);
            _audit.Verify(a => a.GetLogsAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Navigate_WhenModuleCannotBeResolved_ShowsDialogAndKeepsAppAlive()
        {
            var main = Create(); // nenhum ViewModel registrado => GetRequiredService lança

            await main.NavegarAuditoriaLogsCommand.ExecuteAsync(null);

            _dialog.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), true), Times.Once);
            Assert.Null(main.ViewModelAtual);
        }

        [Fact]
        public async Task Navigate_DisposesPreviousDisposableModule()
        {
            // Segunda navegação deve descartar o módulo anterior (ex.: encerra o SignalR da moderação).
            var moderation = new Mock<IModerationApiService>();
            moderation.Setup(m => m.GetPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Lists.Of<ModerationItemDto>());
            moderation.Setup(m => m.TryStartRealtimeAsync(It.IsAny<Action<ModerationItemDto>>())).ReturnsAsync(false);
            var moderationVm = new ModerationViewModel(moderation.Object, _dialog.Object);
            _services.Setup(s => s.GetService(typeof(ModerationViewModel))).Returns(moderationVm);
            RegisterAuditViewModel();
            var main = Create();

            await main.NavegarModeracaoCommand.ExecuteAsync(null);
            await main.NavegarAuditoriaLogsCommand.ExecuteAsync(null);

            moderation.Verify(m => m.StopRealtimeAsync(), Times.Once);
        }

        [Fact]
        public async Task GlobalSearch_OpensUsersFilteredByTerm()
        {
            var users = new Mock<IUserAdminApiService>();
            users.Setup(u => u.GetUsersAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<UserDto>());
            var usersVm = new UserManagementViewModel(users.Object, _dialog.Object, _auth.Object);
            _services.Setup(s => s.GetService(typeof(UserManagementViewModel))).Returns(usersVm);
            var main = Create();
            main.TermoBusca = "  maria ";

            await main.PesquisarUsuarioCommand.ExecuteAsync(null);

            Assert.Equal("maria", usersVm.FiltroTermo);
            users.Verify(u => u.GetUsersAsync("maria", 1, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Logout_WhenNotConfirmed_KeepsSession()
        {
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            var main = Create();
            var raised = 0;
            main.LogoutRequested += () => raised++;

            main.SairCommand.Execute(null);

            _auth.Verify(a => a.Logout(), Times.Never);
            Assert.Equal(0, raised);
        }

        [Fact]
        public void Logout_WhenConfirmed_ClearsSessionAndRaisesEvent()
        {
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            var main = Create();
            var raised = 0;
            main.LogoutRequested += () => raised++;

            main.SairCommand.Execute(null);

            _auth.Verify(a => a.Logout(), Times.Once);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void SessionExpired_IsForwardedToTheShell()
        {
            var main = Create();
            var raised = 0;
            main.SessionExpiredRequested += () => raised++;

            _auth.Raise(a => a.SessionExpired += null);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Dispose_StopsListeningToSessionExpiration()
        {
            var main = Create();
            var raised = 0;
            main.SessionExpiredRequested += () => raised++;

            main.Dispose();
            _auth.Raise(a => a.SessionExpired += null);

            Assert.Equal(0, raised);
        }

        [Fact]
        public void UserIdentity_UsesLoggedUserNameOrFallback()
        {
            Assert.Equal("Administrador", Create().NomeUsuario);
            Assert.Equal("A", Create().InicialUsuario);

            _auth.SetupGet(a => a.CurrentUserName).Returns("maria");
            var main = Create();

            Assert.Equal("maria", main.NomeUsuario);
            Assert.Equal("M", main.InicialUsuario);
        }
    }
}
