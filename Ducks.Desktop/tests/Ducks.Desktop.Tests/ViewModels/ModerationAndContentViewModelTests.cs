using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.Core.Models;
using Moq;

namespace Ducks.Desktop.Tests
{
    public class ModerationViewModelTests
    {
        private readonly Mock<IModerationApiService> _service = new();
        private readonly Mock<IDialogService> _dialog = new();

        private static ModerationItemDto Item(string tipo, int id, string titulo) =>
            new() { Tipo = tipo, ConteudoID = id, Titulo = titulo, Autor = "Maria", Status = "PENDENTE" };

        private ModerationViewModel Create(params ModerationItemDto[] fila)
        {
            _service.Setup(s => s.GetPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Lists.Of(fila));
            _service.Setup(s => s.TryStartRealtimeAsync(It.IsAny<Action<ModerationItemDto>>())).ReturnsAsync(true);
            return new ModerationViewModel(_service.Object, _dialog.Object);
        }

        [Fact]
        public async Task InitializeAsync_LoadsQueueAndStartsRealtime()
        {
            var vm = Create(Item("TRILHA", 1, "Álgebra"), Item("AULA", 2, "Frações"));

            await vm.InitializeAsync();

            Assert.Equal(2, vm.FilaModeracao.Count);
            Assert.True(vm.TempoRealAtivo);
            Assert.Contains("2 item", vm.StatusMessage);
        }

        [Fact]
        public async Task InitializeAsync_WithEmptyQueue_ShowsFriendlyMessage()
        {
            var vm = Create();

            await vm.InitializeAsync();

            Assert.Empty(vm.FilaModeracao);
            Assert.Equal("Nenhum item pendente de moderação.", vm.StatusMessage);
        }

        [Fact]
        public async Task InitializeAsync_WhenRealtimeUnavailable_StillWorks()
        {
            var vm = Create(Item("TRILHA", 1, "Álgebra"));
            _service.Setup(s => s.TryStartRealtimeAsync(It.IsAny<Action<ModerationItemDto>>())).ReturnsAsync(false);

            await vm.InitializeAsync();

            Assert.False(vm.TempoRealAtivo);
            Assert.Single(vm.FilaModeracao);
        }

        [Fact]
        public async Task LoadQueue_WhenApiFails_ShowsError()
        {
            var vm = Create();
            _service.Setup(s => s.GetPendingAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Forbidden, "Você não tem permissão para realizar esta ação."));

            await vm.CarregarFilaAsync();

            Assert.True(vm.HasError);
            Assert.Contains("permissão", vm.StatusMessage);
        }

        [Fact]
        public async Task Approve_CallsApiAndRemovesItemFromQueue()
        {
            var item = Item("TRILHA", 3, "Geometria");
            var vm = Create(item);
            await vm.InitializeAsync();

            await vm.AprovarCommand.ExecuteAsync(item);

            _service.Verify(s => s.ApproveAsync("TRILHA", 3, null, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Empty(vm.FilaModeracao);
            Assert.Contains("aprovado", vm.StatusMessage);
        }

        [Fact]
        public async Task Approve_WhenApiFails_KeepsItemInQueue()
        {
            var item = Item("TRILHA", 3, "Geometria");
            var vm = Create(item);
            await vm.InitializeAsync();
            _service.Setup(s => s.ApproveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Validation, "Item já foi decidido."));

            await vm.AprovarCommand.ExecuteAsync(item);

            Assert.Single(vm.FilaModeracao);
            Assert.True(vm.HasError);
            Assert.Equal("Item já foi decidido.", vm.StatusMessage);
        }

        [Fact]
        public async Task Reject_WhenPromptCancelled_DoesNotCallApi()
        {
            var item = Item("AULA", 5, "Aula 5");
            var vm = Create(item);
            await vm.InitializeAsync();
            string vazio = string.Empty;
            _dialog.Setup(d => d.ShowPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), out vazio)).Returns(false);

            await vm.RejeitarCommand.ExecuteAsync(item);

            _service.Verify(s => s.RejectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.Single(vm.FilaModeracao);
        }

        [Fact]
        public async Task Reject_SendsJustificationAndRemovesItem()
        {
            var item = Item("AULA", 5, "Aula 5");
            var vm = Create(item);
            await vm.InitializeAsync();
            string justificativa = "Erro de português";
            _dialog.Setup(d => d.ShowPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), out justificativa)).Returns(true);

            await vm.RejeitarCommand.ExecuteAsync(item);

            _service.Verify(s => s.RejectAsync("AULA", 5, "Erro de português", It.IsAny<CancellationToken>()), Times.Once);
            Assert.Empty(vm.FilaModeracao);
        }

        [Fact]
        public async Task Remove_SendsJustificationAndRemovesItem()
        {
            var item = Item("QUESTAO", 8, "Questão 8");
            var vm = Create(item);
            await vm.InitializeAsync();
            string justificativa = "Conteúdo inadequado";
            _dialog.Setup(d => d.ShowPrompt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), out justificativa)).Returns(true);

            await vm.RemoverCommand.ExecuteAsync(item);

            _service.Verify(s => s.RemoveAsync("QUESTAO", 8, "Conteúdo inadequado", It.IsAny<CancellationToken>()), Times.Once);
            Assert.Empty(vm.FilaModeracao);
        }

        [Fact]
        public async Task Actions_OnItemWithoutTypeOrId_ShowErrorAndSkipApi()
        {
            var vm = Create();
            var invalido = new ModerationItemDto { Titulo = "Sem identificação" };

            await vm.AprovarCommand.ExecuteAsync(invalido);

            Assert.True(vm.HasError);
            _service.Verify(s => s.ApproveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task LoadHistory_PopulatesHistoryList()
        {
            var vm = Create();
            _service.Setup(s => s.GetHistoryAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lists.Of(new ModerationItemDto { Tipo = "AULA", ConteudoID = 1, Acao = "APROVAR" }));

            await vm.CarregarHistoricoAsync();

            Assert.Single(vm.Historico);
        }

        [Fact]
        public async Task LoadHistory_WhenEmpty_ExplainsEmptyState()
        {
            var vm = Create();
            _service.Setup(s => s.GetHistoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Lists.Of<ModerationItemDto>());

            await vm.CarregarHistoricoAsync();

            Assert.Equal("Nenhuma decisão registrada ainda.", vm.StatusMessage);
        }

        [Fact]
        public void Dispose_StopsRealtimeConnection()
        {
            var vm = Create();

            vm.Dispose();

            _service.Verify(s => s.StopRealtimeAsync(), Times.Once);
        }
    }

    public class CoursesAndTrailsViewModelTests
    {
        private readonly Mock<IContentApiService> _service = new();
        private readonly Mock<IDialogService> _dialog = new();

        private static TrilhaDto T(int id, string nome, string status) =>
            new() { TrilhaID = id, NomeTrilha = nome, Descricao = $"Trilha de {nome}", Status = status, MateriaID = 1 };

        private CoursesAndTrailsViewModel Create(params TrilhaDto[] trilhas)
        {
            _service.Setup(s => s.GetTrilhasAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Lists.Of(trilhas));
            return new CoursesAndTrailsViewModel(_service.Object, _dialog.Object);
        }

        [Fact]
        public async Task InitializeAsync_LoadsTracksAndCountsByStatus()
        {
            var vm = Create(T(1, "Matemática", "APROVADO"), T(2, "Física", "PENDENTE"), T(3, "Química", "PENDENTE"));

            await vm.InitializeAsync();

            Assert.Equal(3, vm.TotalTrilhas);
            Assert.Equal(2, vm.TotalPendentes);
            Assert.Equal(1, vm.TotalAprovadas);
            Assert.Equal(3, vm.Itens.Count);
            Assert.Contains("PENDENTE", vm.OpcoesStatus);
        }

        [Fact]
        public async Task TextFilter_NarrowsList()
        {
            var vm = Create(T(1, "Matemática", "APROVADO"), T(2, "Física", "PENDENTE"));
            await vm.InitializeAsync();

            vm.FiltroTermo = "fís";

            var item = Assert.Single(vm.Itens);
            Assert.Equal("Física", item.NomeTrilha);
        }

        [Fact]
        public async Task StatusFilter_NarrowsList()
        {
            var vm = Create(T(1, "Matemática", "APROVADO"), T(2, "Física", "PENDENTE"));
            await vm.InitializeAsync();

            vm.FiltroStatusSelecionado = "PENDENTE";

            Assert.Single(vm.Itens);
        }

        [Theory]
        [InlineData("", "desc", 1)]
        [InlineData("ab", "desc", 1)]
        [InlineData("Matemática", "desc", 0)]
        public void ValidateNewTrack_RequiresNameAndSubject(string nome, string descricao, int materiaId)
        {
            var vm = Create();
            vm.NovaTrilhaNome = nome;
            vm.NovaTrilhaDescricao = descricao;
            vm.NovaTrilhaMateriaId = materiaId;

            Assert.NotNull(vm.ValidarNovaTrilha());
        }

        [Fact]
        public void ValidateNewTrack_AcceptsValidData()
        {
            var vm = Create();
            vm.NovaTrilhaNome = "Matemática Básica";
            vm.NovaTrilhaMateriaId = 2;

            Assert.Null(vm.ValidarNovaTrilha());
        }

        [Fact]
        public async Task CreateTrack_WithInvalidData_SkipsApi()
        {
            var vm = Create();

            await vm.CriarTrilhaCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.CreateTrilhaAsync(It.IsAny<CreateTrilhaRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateTrack_SendsTrimmedDataClearsFormAndReloads()
        {
            var vm = Create();
            vm.NovaTrilhaNome = "  Matemática Básica ";
            vm.NovaTrilhaDescricao = " Fundamentos ";
            vm.NovaTrilhaMateriaId = 2;

            await vm.CriarTrilhaCommand.ExecuteAsync(null);

            _service.Verify(s => s.CreateTrilhaAsync(
                It.Is<CreateTrilhaRequestDto>(d => d.NomeTrilha == "Matemática Básica" && d.Descricao == "Fundamentos" && d.MateriaID == 2),
                It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(string.Empty, vm.NovaTrilhaNome);
            Assert.Equal(0, vm.NovaTrilhaMateriaId);
            Assert.Contains("PENDENTE", vm.StatusMessage);
            _service.Verify(s => s.GetTrilhasAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateTrack_WhenApiRejects_KeepsFormData()
        {
            var vm = Create();
            _service.Setup(s => s.CreateTrilhaAsync(It.IsAny<CreateTrilhaRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Validation, "Matéria inexistente."));
            vm.NovaTrilhaNome = "Matemática Básica";
            vm.NovaTrilhaMateriaId = 99;

            await vm.CriarTrilhaCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Equal("Matéria inexistente.", vm.StatusMessage);
            Assert.Equal("Matemática Básica", vm.NovaTrilhaNome);
        }

        [Fact]
        public async Task DeleteTrack_WithoutSelection_ShowsError()
        {
            var vm = Create();

            await vm.ExcluirTrilhaCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.DeleteTrilhaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteTrack_WhenNotConfirmed_DoesNothing()
        {
            var vm = Create(T(1, "Matemática", "APROVADO"));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            await vm.ExcluirTrilhaCommand.ExecuteAsync(null);

            _service.Verify(s => s.DeleteTrilhaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteTrack_Confirmed_DeletesAndReloads()
        {
            var vm = Create(T(1, "Matemática", "APROVADO"));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            await vm.ExcluirTrilhaCommand.ExecuteAsync(null);

            _service.Verify(s => s.DeleteTrilhaAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains("excluída", vm.StatusMessage);
        }

        [Fact]
        public async Task SendToReview_WithoutSelection_ShowsError()
        {
            var vm = Create();

            await vm.EnviarRevisaoCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.SendTrilhaToReviewAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SendToReview_CallsApiForSelectedTrack()
        {
            var vm = Create(T(7, "Matemática", "RASCUNHO"));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];

            await vm.EnviarRevisaoCommand.ExecuteAsync(null);

            _service.Verify(s => s.SendTrilhaToReviewAsync(7, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains("enviada para revisão", vm.StatusMessage);
        }
    }
}
