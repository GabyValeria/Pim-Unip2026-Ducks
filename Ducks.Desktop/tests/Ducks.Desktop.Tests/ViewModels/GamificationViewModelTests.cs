using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.Core.Models;
using Moq;

namespace Ducks.Desktop.Tests
{
    public class GamificationViewModelTests
    {
        private readonly Mock<IGamificationApiService> _service = new();
        private readonly Mock<IDialogService> _dialog = new();

        private GamificationManagementViewModel Create()
        {
            _service.Setup(s => s.GetBadgesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lists.Of(new BadgeDto { ConquistaID = 3, Nome = "Pato Mestre", XPNecessario = 100, Ativa = true }));
            _service.Setup(s => s.GetChallengesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lists.Of(new ChallengeDto { DesafioID = 4, Nome = "Maratona", RecompensaXP = 50, Ativo = true }));
            return new GamificationManagementViewModel(_service.Object, _dialog.Object);
        }

        [Fact]
        public async Task InitializeAsync_LoadsBadgesAndChallenges()
        {
            var vm = Create();

            await vm.InitializeAsync();

            Assert.Single(vm.Badges);
            Assert.Single(vm.Desafios);
            Assert.False(vm.HasError);
        }

        [Fact]
        public async Task InitializeAsync_WhenApiFails_ShowsError()
        {
            _service.Setup(s => s.GetBadgesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Server, "Servidor indisponível."));
            var vm = new GamificationManagementViewModel(_service.Object, _dialog.Object);

            await vm.InitializeAsync();

            Assert.True(vm.HasError);
            Assert.Equal("Servidor indisponível.", vm.StatusMessage);
        }

        // ---------------- Medalhas ----------------

        [Fact]
        public async Task SaveBadge_WithInvalidName_ShowsValidationAndSkipsApi()
        {
            var vm = Create();
            vm.BadgeForm.Nome = "A";

            await vm.SalvarMedalhaCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.CreateBadgeAsync(It.IsAny<BadgeRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SaveBadge_New_CreatesReloadsAndResetsForm()
        {
            var vm = Create();
            vm.BadgeForm.Nome = "  Super Pato ";
            vm.BadgeForm.XpNecessario = 200;

            await vm.SalvarMedalhaCommand.ExecuteAsync(null);

            _service.Verify(s => s.CreateBadgeAsync(
                It.Is<BadgeRequestDto>(b => b.Nome == "Super Pato" && b.XPNecessario == 200),
                It.IsAny<CancellationToken>()), Times.Once);
            _service.Verify(s => s.GetBadgesAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(0, vm.BadgeForm.Id);
            Assert.Equal(string.Empty, vm.BadgeForm.Nome);
            Assert.Equal("Medalha criada com sucesso.", vm.StatusMessage);
            Assert.False(vm.HasError);
        }

        [Fact]
        public async Task SaveBadge_AfterSelectingExisting_UpdatesInsteadOfCreating()
        {
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedBadge = vm.Badges[0];
            vm.BadgeForm.Nome = "Pato Lendário";

            await vm.SalvarMedalhaCommand.ExecuteAsync(null);

            _service.Verify(s => s.UpdateBadgeAsync(3, It.Is<BadgeRequestDto>(b => b.Nome == "Pato Lendário"), It.IsAny<CancellationToken>()), Times.Once);
            _service.Verify(s => s.CreateBadgeAsync(It.IsAny<BadgeRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.Equal("Medalha atualizada com sucesso.", vm.StatusMessage);
        }

        [Fact]
        public async Task SaveBadge_WhenApiRejects_KeepsFormAndShowsApiMessage()
        {
            var vm = Create();
            _service.Setup(s => s.CreateBadgeAsync(It.IsAny<BadgeRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Validation, "Nome já existe."));
            vm.BadgeForm.Nome = "Pato";

            await vm.SalvarMedalhaCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Equal("Nome já existe.", vm.StatusMessage);
            Assert.Equal("Pato", vm.BadgeForm.Nome);
        }

        [Fact]
        public async Task DeleteBadge_WithoutSelection_ShowsError()
        {
            var vm = Create();

            await vm.ExcluirMedalhaCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.DeleteBadgeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteBadge_WhenNotConfirmed_DoesNothing()
        {
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedBadge = vm.Badges[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            await vm.ExcluirMedalhaCommand.ExecuteAsync(null);

            _service.Verify(s => s.DeleteBadgeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteBadge_Confirmed_DeletesAndReloads()
        {
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedBadge = vm.Badges[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            await vm.ExcluirMedalhaCommand.ExecuteAsync(null);

            _service.Verify(s => s.DeleteBadgeAsync(3, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains("excluída", vm.StatusMessage);
        }

        [Fact]
        public async Task DeleteBadge_AlreadyAwarded_WarnsInConfirmation()
        {
            var vm = Create();
            _service.Setup(s => s.GetBadgesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lists.Of(new BadgeDto { ConquistaID = 3, Nome = "Pato Mestre", TotalConcedidas = 12 }));
            await vm.InitializeAsync();
            vm.SelectedBadge = vm.Badges[0];
            string? mensagem = null;
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((_, m) => mensagem = m)
                .Returns(false);

            await vm.ExcluirMedalhaCommand.ExecuteAsync(null);

            Assert.Contains("12", mensagem);
        }

        // ---------------- Desafios ----------------

        [Fact]
        public async Task SaveChallenge_WithEndBeforeStart_ShowsValidationAndSkipsApi()
        {
            var vm = Create();
            vm.DesafioForm.Nome = "Maratona";
            vm.DesafioForm.InicioEm = new DateTime(2026, 12, 10);
            vm.DesafioForm.FimEm = new DateTime(2026, 12, 1);

            await vm.SalvarDesafioCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.CreateChallengeAsync(It.IsAny<ChallengeRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SaveChallenge_New_CreatesWithRequirementData()
        {
            var vm = Create();
            vm.DesafioForm.Nome = "Estudioso";
            vm.DesafioForm.RecompensaXp = 80;
            vm.DesafioForm.TipoRequisito = "AULAS_CONCLUIDAS";
            vm.DesafioForm.MetaRequisito = 10;

            await vm.SalvarDesafioCommand.ExecuteAsync(null);

            _service.Verify(s => s.CreateChallengeAsync(
                It.Is<ChallengeRequestDto>(c => c.Nome == "Estudioso" && c.RecompensaXP == 80 && c.TipoRequisito == "AULAS_CONCLUIDAS" && c.MetaRequisito == 10),
                It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal("Desafio criado com sucesso.", vm.StatusMessage);
        }

        [Fact]
        public async Task SaveChallenge_AfterSelectingExisting_Updates()
        {
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedDesafio = vm.Desafios[0];

            await vm.SalvarDesafioCommand.ExecuteAsync(null);

            _service.Verify(s => s.UpdateChallengeAsync(4, It.IsAny<ChallengeRequestDto>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteChallenge_Confirmed_Deletes()
        {
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedDesafio = vm.Desafios[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            await vm.ExcluirDesafioCommand.ExecuteAsync(null);

            _service.Verify(s => s.DeleteChallengeAsync(4, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------------- Medalhas por usuário ----------------

        [Fact]
        public async Task ConsultUserBadges_WithInvalidId_ShowsError()
        {
            var vm = Create();
            vm.UsuarioConsultaId = 0;

            await vm.ConsultarMedalhasUsuarioCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.GetUserBadgesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ConsultUserBadges_ListsBadgesOfUser()
        {
            var vm = Create();
            _service.Setup(s => s.GetUserBadgesAsync(9, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lists.Of(new UserBadgeDto { ConquistaID = 1, Nome = "Primeira aula" }));
            vm.UsuarioConsultaId = 9;

            await vm.ConsultarMedalhasUsuarioCommand.ExecuteAsync(null);

            Assert.Single(vm.MedalhasDoUsuario);
            Assert.Contains("1 medalha", vm.StatusMessage);
        }

        [Fact]
        public async Task ConsultUserBadges_WithNoBadges_ExplainsEmptyResult()
        {
            var vm = Create();
            _service.Setup(s => s.GetUserBadgesAsync(9, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lists.Of<UserBadgeDto>());
            vm.UsuarioConsultaId = 9;

            await vm.ConsultarMedalhasUsuarioCommand.ExecuteAsync(null);

            Assert.Empty(vm.MedalhasDoUsuario);
            Assert.Contains("ainda não possui", vm.StatusMessage);
        }

        [Fact]
        public void NewBadge_ResetsSelectionAndForm()
        {
            var vm = Create();
            vm.BadgeForm.Nome = "Sujo";

            vm.NovaMedalhaCommand.Execute(null);

            Assert.Null(vm.SelectedBadge);
            Assert.Equal(string.Empty, vm.BadgeForm.Nome);
        }
    }
}
