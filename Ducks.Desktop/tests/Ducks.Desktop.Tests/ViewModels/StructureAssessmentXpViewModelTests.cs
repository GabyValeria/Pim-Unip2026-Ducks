using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.Core.Models;
using Moq;

namespace Ducks.Desktop.Tests
{
    public class ContentStructureViewModelTests
    {
        private readonly Mock<IContentApiService> _trilhas = new();
        private readonly Mock<IContentManagementApiService> _service = new();
        private readonly Mock<IDialogService> _dialog = new();

        private static ModuloDto M(int id, int ordem, string titulo = "Módulo") =>
            new() { ModuloID = id, TrilhaID = 1, Titulo = $"{titulo} {id}", IndiceOrdem = ordem, Status = "RASCUNHO" };

        private static AulaDto A(int id, int ordem) =>
            new() { AulaID = id, ModuloID = 10, Titulo = $"Aula {id}", UrlConteudo = "https://x", IndiceOrdem = ordem, Status = "RASCUNHO" };

        private ContentStructureViewModel Create(IReadOnlyList<ModuloDto>? modulos = null, IReadOnlyList<AulaDto>? aulas = null)
        {
            _trilhas.Setup(t => t.GetTrilhasAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lists.Of(new TrilhaDto { TrilhaID = 1, NomeTrilha = "Matemática" }));
            _service.Setup(s => s.GetModulosAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(modulos ?? Lists.Of<ModuloDto>());
            _service.Setup(s => s.GetAulasAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(aulas ?? Lists.Of<AulaDto>());
            return new ContentStructureViewModel(_trilhas.Object, _service.Object, _dialog.Object);
        }

        private static async Task<ContentStructureViewModel> WithTrilhaAsync(ContentStructureViewModel vm)
        {
            await vm.InitializeAsync();
            vm.SelectedTrilha = vm.Trilhas[0];
            await vm.CarregarModulosAsync();
            return vm;
        }

        [Fact]
        public async Task Initialize_LoadsTrilhas()
        {
            var vm = Create();

            await vm.InitializeAsync();

            Assert.Single(vm.Trilhas);
            Assert.False(vm.HasError);
        }

        [Fact]
        public async Task Initialize_WhenApiFails_ShowsFriendlyError()
        {
            var vm = Create();
            _trilhas.Setup(t => t.GetTrilhasAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Network, "Sem conexão com a API."));

            await vm.InitializeAsync();

            Assert.True(vm.HasError);
            Assert.Equal("Sem conexão com a API.", vm.StatusMessage);
        }

        [Fact]
        public async Task LoadModulos_OrdersByIndiceOrdem()
        {
            var vm = await WithTrilhaAsync(Create(Lists.Of(M(2, 2), M(1, 1), M(3, 3))));

            Assert.Equal(new[] { 1, 2, 3 }, vm.Modulos.Select(m => m.ModuloID));
        }

        [Fact]
        public async Task SalvarModulo_WithoutTrilha_ShowsError()
        {
            var vm = Create();
            vm.ModuloTitulo = "Frações";

            await vm.SalvarModuloCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.CreateModuloAsync(It.IsAny<int>(), It.IsAny<ModuloRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ab")]
        public async Task SalvarModulo_WithInvalidTitle_SkipsApi(string titulo)
        {
            var vm = await WithTrilhaAsync(Create());
            vm.ModuloTitulo = titulo;

            await vm.SalvarModuloCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.CreateModuloAsync(It.IsAny<int>(), It.IsAny<ModuloRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SalvarModulo_CreatesWithTrimmedData()
        {
            var vm = await WithTrilhaAsync(Create());
            vm.ModuloTitulo = "  Frações ";
            vm.ModuloDescricao = "  ";

            await vm.SalvarModuloCommand.ExecuteAsync(null);

            _service.Verify(s => s.CreateModuloAsync(1,
                It.Is<ModuloRequestDto>(d => d.Titulo == "Frações" && d.Descricao == null), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(string.Empty, vm.ModuloTitulo);
            Assert.False(vm.HasError);
        }

        [Fact]
        public async Task EditarModulo_LoadsFormAndSaveSendsUpdate()
        {
            var vm = await WithTrilhaAsync(Create(Lists.Of(M(5, 1))));
            vm.SelectedModulo = vm.Modulos[0];

            vm.EditarModuloCommand.Execute(null);
            Assert.True(vm.EditandoModulo);
            Assert.Equal("Módulo 5", vm.ModuloTitulo);

            vm.ModuloTitulo = "Módulo renomeado";
            await vm.SalvarModuloCommand.ExecuteAsync(null);

            _service.Verify(s => s.UpdateModuloAsync(5, It.Is<ModuloRequestDto>(d => d.Titulo == "Módulo renomeado"), It.IsAny<CancellationToken>()), Times.Once);
            _service.Verify(s => s.CreateModuloAsync(It.IsAny<int>(), It.IsAny<ModuloRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExcluirModulo_WhenNotConfirmed_DoesNothing()
        {
            var vm = await WithTrilhaAsync(Create(Lists.Of(M(5, 1))));
            vm.SelectedModulo = vm.Modulos[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            await vm.ExcluirModuloCommand.ExecuteAsync(null);

            _service.Verify(s => s.DeleteModuloAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExcluirModulo_WhenConfirmed_DeletesAndReloads()
        {
            var vm = await WithTrilhaAsync(Create(Lists.Of(M(5, 1))));
            vm.SelectedModulo = vm.Modulos[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            await vm.ExcluirModuloCommand.ExecuteAsync(null);

            _service.Verify(s => s.DeleteModuloAsync(5, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains("excluído", vm.StatusMessage);
        }

        [Fact]
        public async Task ExcluirModulo_WithoutSelection_ShowsError()
        {
            var vm = await WithTrilhaAsync(Create());

            await vm.ExcluirModuloCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
        }

        [Fact]
        public async Task DescerModulo_SendsNewOrderToApi()
        {
            var vm = await WithTrilhaAsync(Create(Lists.Of(M(1, 1), M(2, 2), M(3, 3))));
            vm.SelectedModulo = vm.Modulos[0];

            await vm.DescerModuloCommand.ExecuteAsync(null);

            _service.Verify(s => s.ReorderModulosAsync(1,
                It.Is<IReadOnlyList<int>>(ids => ids.SequenceEqual(new[] { 2, 1, 3 })), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SubirModulo_WhenAlreadyFirst_DoesNotCallApi()
        {
            var vm = await WithTrilhaAsync(Create(Lists.Of(M(1, 1), M(2, 2))));
            vm.SelectedModulo = vm.Modulos[0];

            await vm.SubirModuloCommand.ExecuteAsync(null);

            _service.Verify(s => s.ReorderModulosAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0, -1, null)]
        [InlineData(2, 1, null)]
        [InlineData(1, -1, new[] { 20, 10, 30 })]
        [InlineData(1, 1, new[] { 10, 30, 20 })]
        public void ReordenarIds_MovesOnlyWithinBounds(int indice, int delta, int[]? esperado)
        {
            var ids = new List<int> { 10, 20, 30 };

            var resultado = ContentStructureViewModel.ReordenarIds(ids, ids[indice], delta);

            if (esperado == null) Assert.Null(resultado);
            else Assert.Equal(esperado, resultado);
        }

        [Fact]
        public async Task SalvarAula_WithoutModulo_ShowsError()
        {
            var vm = await WithTrilhaAsync(Create());
            vm.AulaTitulo = "Introdução";

            await vm.SalvarAulaCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.CreateAulaAsync(It.IsAny<int>(), It.IsAny<AulaRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SalvarAula_CreatesInSelectedModulo()
        {
            var vm = await WithTrilhaAsync(Create(Lists.Of(M(10, 1))));
            vm.SelectedModulo = vm.Modulos[0];
            await vm.CarregarAulasAsync();
            vm.AulaTitulo = " Introdução ";
            vm.AulaUrl = " https://exemplo.com/v1 ";
            vm.AulaConteudo = "Texto da aula";

            await vm.SalvarAulaCommand.ExecuteAsync(null);

            _service.Verify(s => s.CreateAulaAsync(10,
                It.Is<AulaRequestDto>(d => d.Titulo == "Introdução" && d.UrlConteudo == "https://exemplo.com/v1" && d.Conteudo == "Texto da aula"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void ValidarAula_RejectsTooLongUrl()
        {
            var vm = Create();
            vm.AulaTitulo = "Introdução";
            vm.AulaUrl = new string('a', 501);

            Assert.NotNull(vm.ValidarAula());
        }

        [Fact]
        public async Task SalvarAula_WhenApiRejects_KeepsFormAndShowsApiMessage()
        {
            var vm = await WithTrilhaAsync(Create(Lists.Of(M(10, 1))));
            vm.SelectedModulo = vm.Modulos[0];
            _service.Setup(s => s.CreateAulaAsync(It.IsAny<int>(), It.IsAny<AulaRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Validation, "Título já utilizado neste módulo."));
            vm.AulaTitulo = "Introdução";

            await vm.SalvarAulaCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Equal("Título já utilizado neste módulo.", vm.StatusMessage);
            Assert.Equal("Introdução", vm.AulaTitulo);
        }
    }

    public class QuestionBankViewModelTests
    {
        private readonly Mock<IAssessmentApiService> _service = new();
        private readonly Mock<IDialogService> _dialog = new();

        private QuestionBankViewModel Create(params QuestaoDto[] questoes)
        {
            _service.Setup(s => s.GetQuestoesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<QuestaoDto> { Page = 1, PageSize = 25, Total = questoes.Length, Items = questoes.ToList() });
            return new QuestionBankViewModel(_service.Object, _dialog.Object);
        }

        private static QuestaoDto Q(int id) => new()
        {
            QuestaoID = id, Enunciado = "Quanto é 2+2?", Tipo = "MULTIPLA_ESCOLHA", Dificuldade = "FACIL", Peso = 2,
            Tags = new List<string> { "soma", "basico" }, TrilhaID = 1, Status = "RASCUNHO",
            Alternativas = new List<AlternativaDto>
            {
                new() { AlternativaID = 11, Texto = "3" },
                new() { AlternativaID = 12, Texto = "4", Correta = true, Explicacao = "2+2=4" }
            }
        };

        private static void Preencher(QuestionBankViewModel vm)
        {
            vm.Enunciado = "Quanto é 2+2?";
            vm.Alternativas[0].Texto = "3";
            vm.Alternativas[1].Texto = "4";
            vm.Alternativas[1].Correta = true;
        }

        [Fact]
        public async Task Initialize_LoadsFirstPageWithoutFilters()
        {
            var vm = Create(Q(1), Q(2));

            await vm.InitializeAsync();

            Assert.Equal(2, vm.Itens.Count);
            Assert.Equal(2, vm.TotalItens);
            _service.Verify(s => s.GetQuestoesAsync(null, null, null, 1, QuestionBankViewModel.TamanhoPagina, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Initialize_WhenEmpty_ShowsFriendlyMessage()
        {
            var vm = Create();

            await vm.InitializeAsync();

            Assert.Contains("Nenhuma questão", vm.StatusMessage);
        }

        [Fact]
        public async Task Search_SendsFiltersAndResetsToFirstPage()
        {
            var vm = Create(Q(1));
            vm.Pagina = 3;
            vm.FiltroBusca = "  soma ";
            vm.FiltroStatus = "APROVADO";
            vm.FiltroDificuldade = "DIFICIL";

            await vm.BuscarCommand.ExecuteAsync(null);

            _service.Verify(s => s.GetQuestoesAsync("soma", "APROVADO", "DIFICIL", 1, QuestionBankViewModel.TamanhoPagina, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NextPage_StopsAtLastPage()
        {
            var vm = Create(Q(1));
            await vm.InitializeAsync();

            await vm.PaginaSeguinteCommand.ExecuteAsync(null);

            Assert.Equal(1, vm.Pagina);
            _service.Verify(s => s.GetQuestoesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), 2, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void NewForm_StartsWithTwoEmptyAlternatives()
        {
            var vm = Create();

            Assert.Equal(2, vm.Alternativas.Count);
            Assert.False(vm.Editando);
        }

        [Fact]
        public void Validar_AcceptsCompleteQuestion()
        {
            var vm = Create();
            Preencher(vm);

            Assert.Null(vm.Validar(out var dto));
            Assert.NotNull(dto);
            Assert.Equal(2, dto!.Alternativas.Count);
        }

        [Fact]
        public void Validar_RejectsShortStatement()
        {
            var vm = Create();
            Preencher(vm);
            vm.Enunciado = "abc";

            Assert.NotNull(vm.Validar(out _));
        }

        [Fact]
        public void Validar_RequiresAtLeastOneCorrectAlternative()
        {
            var vm = Create();
            Preencher(vm);
            vm.Alternativas[1].Correta = false;

            Assert.Contains("correta", vm.Validar(out _));
        }

        [Fact]
        public void Validar_RejectsBlankAlternativeText()
        {
            var vm = Create();
            Preencher(vm);
            vm.Alternativas[0].Texto = "  ";

            Assert.NotNull(vm.Validar(out _));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("101")]
        [InlineData("x")]
        public void Validar_RejectsInvalidWeight(string peso)
        {
            var vm = Create();
            Preencher(vm);
            vm.PesoTexto = peso;

            Assert.NotNull(vm.Validar(out _));
        }

        [Fact]
        public void Validar_TrueFalseRequiresExactlyTwoAlternatives()
        {
            var vm = Create();
            Preencher(vm);
            vm.AdicionarAlternativaCommand.Execute(null);
            vm.Alternativas[2].Texto = "Talvez";
            vm.Tipo = "VERDADEIRO_FALSO";

            Assert.Contains("exatamente 2", vm.Validar(out _));
        }

        [Fact]
        public void Validar_RejectsMoreThanTenTags()
        {
            var vm = Create();
            Preencher(vm);
            vm.TagsTexto = string.Join(",", Enumerable.Range(1, 11).Select(i => $"t{i}"));

            Assert.Contains("10 tags", vm.Validar(out _));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("abc")]
        public void Validar_RejectsInvalidOptionalIds(string valor)
        {
            var vm = Create();
            Preencher(vm);
            vm.TrilhaIdTexto = valor;

            Assert.NotNull(vm.Validar(out _));
        }

        [Fact]
        public void AddAlternative_IsLimitedToSix()
        {
            var vm = Create();
            for (var i = 0; i < 10; i++) vm.AdicionarAlternativaCommand.Execute(null);

            Assert.Equal(QuestionBankViewModel.MaxAlternativas, vm.Alternativas.Count);
            Assert.True(vm.HasError);
        }

        [Fact]
        public void RemoveAlternative_KeepsAtLeastTwo()
        {
            var vm = Create();

            vm.RemoverAlternativaCommand.Execute(vm.Alternativas[0]);

            Assert.Equal(2, vm.Alternativas.Count);
            Assert.True(vm.HasError);
        }

        [Fact]
        public async Task Salvar_WithInvalidData_SkipsApi()
        {
            var vm = Create();

            await vm.SalvarCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.CreateQuestaoAsync(It.IsAny<QuestaoRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Salvar_CreatesQuestionParsingTagsAndResetsForm()
        {
            var vm = Create();
            Preencher(vm);
            vm.TagsTexto = " soma , Soma, basico ";
            vm.Assunto = "  Aritmética ";
            vm.TrilhaIdTexto = "4";

            await vm.SalvarCommand.ExecuteAsync(null);

            _service.Verify(s => s.CreateQuestaoAsync(It.Is<QuestaoRequestDto>(d =>
                d.Enunciado == "Quanto é 2+2?" && d.Assunto == "Aritmética" && d.TrilhaID == 4 && d.ModuloID == null &&
                d.Tags.SequenceEqual(new[] { "soma", "basico" }) && d.Alternativas.Count == 2 && d.Alternativas[1].Correta),
                It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(string.Empty, vm.Enunciado);
            Assert.Contains("RASCUNHO", vm.StatusMessage);
        }

        [Fact]
        public async Task Salvar_WhenApiRejects_KeepsFormData()
        {
            var vm = Create();
            Preencher(vm);
            _service.Setup(s => s.CreateQuestaoAsync(It.IsAny<QuestaoRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Validation, "Trilha inexistente."));

            await vm.SalvarCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Equal("Trilha inexistente.", vm.StatusMessage);
            Assert.Equal("Quanto é 2+2?", vm.Enunciado);
        }

        [Fact]
        public async Task Editar_LoadsQuestionAndSaveKeepsAlternativeIds()
        {
            var vm = Create(Q(7));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];

            vm.EditarQuestaoCommand.Execute(null);
            Assert.True(vm.Editando);
            Assert.Equal("soma, basico", vm.TagsTexto);
            Assert.Equal("2", vm.PesoTexto);

            await vm.SalvarCommand.ExecuteAsync(null);

            _service.Verify(s => s.UpdateQuestaoAsync(7, It.Is<QuestaoRequestDto>(d =>
                d.Peso == 2 && d.Alternativas[0].AlternativaID == 11 && d.Alternativas[1].AlternativaID == 12 && d.Alternativas[1].Explicacao == "2+2=4"),
                It.IsAny<CancellationToken>()), Times.Once);
            _service.Verify(s => s.CreateQuestaoAsync(It.IsAny<QuestaoRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.False(vm.Editando);
        }

        [Fact]
        public async Task Excluir_WhenNotConfirmed_DoesNothing()
        {
            var vm = Create(Q(7));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            await vm.ExcluirCommand.ExecuteAsync(null);

            _service.Verify(s => s.DeleteQuestaoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Excluir_WhenConfirmed_DeletesAndReloads()
        {
            var vm = Create(Q(7));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            await vm.ExcluirCommand.ExecuteAsync(null);

            _service.Verify(s => s.DeleteQuestaoAsync(7, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains("excluída", vm.StatusMessage);
        }

        [Fact]
        public async Task EnviarRevisao_WithoutSelection_ShowsError()
        {
            var vm = Create();

            await vm.EnviarRevisaoCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.SendQuestaoToReviewAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task EnviarRevisao_SendsSelectedQuestion()
        {
            var vm = Create(Q(7));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];

            await vm.EnviarRevisaoCommand.ExecuteAsync(null);

            _service.Verify(s => s.SendQuestaoToReviewAsync(7, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class SimuladosViewModelTests
    {
        private readonly Mock<IAssessmentApiService> _service = new();
        private readonly Mock<IDialogService> _dialog = new();

        private static SimuladoDto S(int id, params int[] questoes) => new()
        {
            ProvaID = id, Titulo = $"Simulado {id}", NotaMinimaAprovacao = 70, RecompensaXP = 80, NumeroQuestoes = 5, Status = "RASCUNHO",
            TempoLimiteMinutos = 45,
            Questoes = questoes.Select((q, i) => new QuestaoDoSimuladoDto { QuestaoID = q, Ordem = i + 1, Enunciado = $"Q{q}" }).ToList()
        };

        private SimuladosViewModel Create(params SimuladoDto[] simulados)
        {
            _service.Setup(s => s.GetSimuladosAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<SimuladoDto> { Page = 1, PageSize = 25, Total = simulados.Length, Items = simulados.ToList() });
            return new SimuladosViewModel(_service.Object, _dialog.Object);
        }

        [Fact]
        public async Task Initialize_LoadsSimulados()
        {
            var vm = Create(S(1), S(2));

            await vm.InitializeAsync();

            Assert.Equal(2, vm.Itens.Count);
            _service.Verify(s => s.GetSimuladosAsync(null, null, 1, SimuladosViewModel.TamanhoPagina, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SelectingSimulado_ShowsItsQuestionsInOrder()
        {
            var vm = Create(S(1, 30, 10, 20));
            await vm.InitializeAsync();

            vm.SelectedItem = vm.Itens[0];

            Assert.Equal(3, vm.Questoes.Count);
            Assert.Equal("30, 10, 20", vm.QuestaoIdsTexto);
        }

        [Theory]
        [InlineData("", "60", "50", "10")]
        [InlineData("ab", "60", "50", "10")]
        [InlineData("Simulado", "101", "50", "10")]
        [InlineData("Simulado", "60", "10001", "10")]
        [InlineData("Simulado", "60", "50", "0")]
        [InlineData("Simulado", "60", "50", "201")]
        [InlineData("Simulado", "x", "50", "10")]
        public void Validar_RejectsOutOfRangeValues(string titulo, string nota, string xp, string numero)
        {
            var vm = Create();
            vm.Titulo = titulo;
            vm.NotaMinimaTexto = nota;
            vm.RecompensaXpTexto = xp;
            vm.NumeroQuestoesTexto = numero;

            Assert.NotNull(vm.Validar(out var dto));
            Assert.Null(dto);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("601")]
        public void Validar_RejectsInvalidTimeLimit(string tempo)
        {
            var vm = Create();
            vm.Titulo = "Simulado";
            vm.TempoLimiteTexto = tempo;

            Assert.NotNull(vm.Validar(out _));
        }

        [Fact]
        public void Validar_BlankOptionalFieldsBecomeNull()
        {
            var vm = Create();
            vm.Titulo = "  Simulado geral ";

            Assert.Null(vm.Validar(out var dto));
            Assert.Equal("Simulado geral", dto!.Titulo);
            Assert.Null(dto.TempoLimiteMinutos);
            Assert.Null(dto.MaxTentativas);
            Assert.Null(dto.TrilhaID);
            Assert.Equal(60, dto.NotaMinimaAprovacao);
            Assert.Equal(50, dto.RecompensaXP);
            Assert.Equal(10, dto.NumeroQuestoes);
        }

        [Fact]
        public async Task Salvar_CreatesSimuladoAndResetsForm()
        {
            var vm = Create();
            vm.Titulo = "Simulado geral";
            vm.TempoLimiteTexto = "45";

            await vm.SalvarCommand.ExecuteAsync(null);

            _service.Verify(s => s.CreateSimuladoAsync(It.Is<SimuladoRequestDto>(d => d.Titulo == "Simulado geral" && d.TempoLimiteMinutos == 45), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(string.Empty, vm.Titulo);
            Assert.Contains("RASCUNHO", vm.StatusMessage);
        }

        [Fact]
        public async Task Salvar_WithInvalidData_SkipsApi()
        {
            var vm = Create();

            await vm.SalvarCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.CreateSimuladoAsync(It.IsAny<SimuladoRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Editar_LoadsFormAndSaveSendsUpdate()
        {
            var vm = Create(S(3));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];

            vm.EditarSimuladoCommand.Execute(null);
            Assert.True(vm.Editando);
            Assert.Equal("45", vm.TempoLimiteTexto);
            Assert.Equal("70", vm.NotaMinimaTexto);

            await vm.SalvarCommand.ExecuteAsync(null);

            _service.Verify(s => s.UpdateSimuladoAsync(3, It.Is<SimuladoRequestDto>(d => d.NotaMinimaAprovacao == 70 && d.RecompensaXP == 80), It.IsAny<CancellationToken>()), Times.Once);
            _service.Verify(s => s.CreateSimuladoAsync(It.IsAny<SimuladoRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Excluir_WhenConfirmed_Deletes()
        {
            var vm = Create(S(3));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            await vm.ExcluirCommand.ExecuteAsync(null);

            _service.Verify(s => s.DeleteSimuladoAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Excluir_WhenNotConfirmed_DoesNothing()
        {
            var vm = Create(S(3));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            await vm.ExcluirCommand.ExecuteAsync(null);

            _service.Verify(s => s.DeleteSimuladoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task EnviarRevisao_SendsSelectedSimulado()
        {
            var vm = Create(S(3));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];

            await vm.EnviarRevisaoCommand.ExecuteAsync(null);

            _service.Verify(s => s.SendSimuladoToReviewAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("1, 2, 3", new[] { 1, 2, 3 })]
        [InlineData("3;1 2", new[] { 3, 1, 2 })]
        [InlineData("5, 5, 6", new[] { 5, 6 })]
        [InlineData("", new int[0])]
        public void ParseIds_AcceptsSeparatorsAndRemovesDuplicates(string texto, int[] esperado)
        {
            Assert.Equal(esperado, SimuladosViewModel.ParseIds(texto));
        }

        [Theory]
        [InlineData("1, abc")]
        [InlineData("0")]
        [InlineData("-3")]
        public void ParseIds_RejectsInvalidValues(string texto)
        {
            Assert.Null(SimuladosViewModel.ParseIds(texto));
        }

        [Fact]
        public async Task DefinirQuestoes_WithoutSelection_ShowsError()
        {
            var vm = Create();
            vm.QuestaoIdsTexto = "1,2";

            await vm.DefinirQuestoesCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.SetQuestoesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DefinirQuestoes_WithEmptyOrInvalidIds_SkipsApi()
        {
            var vm = Create(S(3));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];

            vm.QuestaoIdsTexto = "";
            await vm.DefinirQuestoesCommand.ExecuteAsync(null);
            vm.QuestaoIdsTexto = "1, x";
            await vm.DefinirQuestoesCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.SetQuestoesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DefinirQuestoes_SendsOrderedIdsAndReloads()
        {
            var vm = Create(S(3));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            vm.QuestaoIdsTexto = "8, 4, 9";

            await vm.DefinirQuestoesCommand.ExecuteAsync(null);

            _service.Verify(s => s.SetQuestoesAsync(3, It.Is<IReadOnlyList<int>>(ids => ids.SequenceEqual(new[] { 8, 4, 9 })), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains("3 questão", vm.StatusMessage);
        }

        [Fact]
        public async Task DefinirQuestoes_WhenApiRejects_ShowsApiMessage()
        {
            var vm = Create(S(3));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            vm.QuestaoIdsTexto = "8";
            _service.Setup(s => s.SetQuestoesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Validation, "A questão 8 não está aprovada."));

            await vm.DefinirQuestoesCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Equal("A questão 8 não está aprovada.", vm.StatusMessage);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("201")]
        [InlineData("x")]
        public void ValidarSorteio_RejectsInvalidQuantity(string quantidade)
        {
            var vm = Create();
            vm.SorteioQuantidadeTexto = quantidade;

            Assert.NotNull(vm.ValidarSorteio(out _));
        }

        [Fact]
        public void ValidarSorteio_MapsFilters()
        {
            var vm = Create();
            vm.SorteioQuantidadeTexto = "5";
            vm.SorteioDificuldade = "MEDIO";
            vm.SorteioTag = " soma ";
            vm.SorteioSubstituir = false;

            Assert.Null(vm.ValidarSorteio(out var dto));
            Assert.Equal(5, dto!.Quantidade);
            Assert.Equal("MEDIO", dto.Dificuldade);
            Assert.Equal("soma", dto.Tag);
            Assert.Null(dto.Assunto);
            Assert.False(dto.SubstituirAtuais);
        }

        [Fact]
        public void ValidarSorteio_TodosMeansNoDifficultyFilter()
        {
            var vm = Create();

            Assert.Null(vm.ValidarSorteio(out var dto));
            Assert.Null(dto!.Dificuldade);
            Assert.Null(dto.Quantidade);
            Assert.True(dto.SubstituirAtuais);
        }

        [Fact]
        public async Task Sortear_WithoutSelection_ShowsError()
        {
            var vm = Create();

            await vm.SortearCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.SortearQuestoesAsync(It.IsAny<int>(), It.IsAny<SortearQuestoesRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Sortear_WhenReplacingExistingQuestionsAndNotConfirmed_DoesNothing()
        {
            var vm = Create(S(3, 1, 2));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            await vm.SortearCommand.ExecuteAsync(null);

            _service.Verify(s => s.SortearQuestoesAsync(It.IsAny<int>(), It.IsAny<SortearQuestoesRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Sortear_WithoutExistingQuestions_RunsWithoutConfirmation()
        {
            var vm = Create(S(3));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            vm.SorteioQuantidadeTexto = "4";

            await vm.SortearCommand.ExecuteAsync(null);

            _service.Verify(s => s.SortearQuestoesAsync(3, It.Is<SortearQuestoesRequestDto>(d => d.Quantidade == 4 && d.SubstituirAtuais), It.IsAny<CancellationToken>()), Times.Once);
            _dialog.Verify(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Sortear_WhenConfirmed_CompletesWithSuccessMessage()
        {
            var vm = Create(S(3, 1));
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _service.Setup(s => s.SortearQuestoesAsync(3, It.IsAny<SortearQuestoesRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(S(3, 7, 8));

            await vm.SortearCommand.ExecuteAsync(null);

            Assert.False(vm.HasError);
            Assert.Contains("Sorteio concluído", vm.StatusMessage);
        }
    }

    public class XpAdjustmentViewModelTests
    {
        private readonly Mock<IXpApiService> _service = new();
        private readonly Mock<IDialogService> _dialog = new();

        private XpAdjustmentViewModel Create(string usuario = "5", string quantidade = "100", string motivo = "Bônus de participação")
        {
            return new XpAdjustmentViewModel(_service.Object, _dialog.Object)
            {
                UsuarioIdTexto = usuario,
                QuantidadeTexto = quantidade,
                Motivo = motivo
            };
        }

        [Theory]
        [InlineData("", "100", "Motivo válido")]
        [InlineData("0", "100", "Motivo válido")]
        [InlineData("abc", "100", "Motivo válido")]
        [InlineData("5", "", "Motivo válido")]
        [InlineData("5", "0", "Motivo válido")]
        [InlineData("5", "10001", "Motivo válido")]
        [InlineData("5", "-10001", "Motivo válido")]
        [InlineData("5", "100", "")]
        [InlineData("5", "100", "ab")]
        public void Validar_RejectsInvalidInput(string usuario, string quantidade, string motivo)
        {
            var vm = Create(usuario, quantidade, motivo);

            Assert.NotNull(vm.Validar(out var dto));
            Assert.Null(dto);
        }

        [Theory]
        [InlineData("100", 100)]
        [InlineData("-50", -50)]
        [InlineData("10000", 10000)]
        [InlineData("-10000", -10000)]
        public void Validar_AcceptsBoundaryValues(string quantidade, int esperado)
        {
            var vm = Create(quantidade: quantidade);

            Assert.Null(vm.Validar(out var dto));
            Assert.Equal(esperado, dto!.QuantidadeXP);
        }

        [Fact]
        public void Validar_RejectsTooLongReason()
        {
            var vm = Create(motivo: new string('a', 251));

            Assert.NotNull(vm.Validar(out _));
        }

        [Fact]
        public async Task Aplicar_WithInvalidData_SkipsDialogAndApi()
        {
            var vm = Create(usuario: "");

            await vm.AplicarCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _dialog.Verify(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _service.Verify(s => s.AdjustXpAsync(It.IsAny<AdicionarXpRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Aplicar_WhenNotConfirmed_DoesNotCallApi()
        {
            var vm = Create();
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            await vm.AplicarCommand.ExecuteAsync(null);

            _service.Verify(s => s.AdjustXpAsync(It.IsAny<AdicionarXpRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.Equal("100", vm.QuantidadeTexto);
        }

        [Fact]
        public async Task Aplicar_WhenConfirmed_SendsTrimmedDataAndClearsAmountAndReason()
        {
            var vm = Create(usuario: " 5 ", quantidade: " -30 ", motivo: "  Correção de pontuação ");
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            await vm.AplicarCommand.ExecuteAsync(null);

            _service.Verify(s => s.AdjustXpAsync(It.Is<AdicionarXpRequestDto>(d =>
                d.UsuarioID == 5 && d.QuantidadeXP == -30 && d.Motivo == "Correção de pontuação"), It.IsAny<CancellationToken>()), Times.Once);
            Assert.False(vm.HasError);
            Assert.Equal(string.Empty, vm.QuantidadeTexto);
            Assert.Equal(string.Empty, vm.Motivo);
            Assert.Contains("#5", vm.StatusMessage);
        }

        [Fact]
        public async Task Aplicar_WhenApiRejects_KeepsFormAndShowsApiMessage()
        {
            var vm = Create();
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _service.Setup(s => s.AdjustXpAsync(It.IsAny<AdicionarXpRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.NotFound, "Usuário não encontrado."));

            await vm.AplicarCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Equal("Usuário não encontrado.", vm.StatusMessage);
            Assert.Equal("100", vm.QuantidadeTexto);
        }
    }

    public class CoursesAndTrailsEditTests
    {
        private readonly Mock<IContentApiService> _content = new();
        private readonly Mock<IContentManagementApiService> _management = new();
        private readonly Mock<IDialogService> _dialog = new();

        private CoursesAndTrailsViewModel Create(bool withManagement = true)
        {
            _content.Setup(c => c.GetTrilhasAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lists.Of(new TrilhaDto { TrilhaID = 7, NomeTrilha = "Matemática", Descricao = "Fundamentos", MateriaID = 2, Status = "APROVADO" }));
            return new CoursesAndTrailsViewModel(_content.Object, _dialog.Object, withManagement ? _management.Object : null);
        }

        [Fact]
        public void EditarTrilha_WithoutSelection_ShowsError()
        {
            var vm = Create();

            vm.EditarTrilhaCommand.Execute(null);

            Assert.True(vm.HasError);
            Assert.False(vm.EditandoTrilha);
        }

        [Fact]
        public async Task EditarTrilha_LoadsSelectedTrilhaIntoForm()
        {
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];

            vm.EditarTrilhaCommand.Execute(null);

            Assert.True(vm.EditandoTrilha);
            Assert.Equal("Matemática", vm.NovaTrilhaNome);
            Assert.Equal(2, vm.NovaTrilhaMateriaId);
            Assert.Equal("Salvar alterações", vm.TextoBotaoSalvar);
            Assert.Contains("#7", vm.TituloFormulario);
        }

        [Fact]
        public async Task SaveWhileEditing_SendsUpdateInsteadOfCreate()
        {
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            vm.EditarTrilhaCommand.Execute(null);
            vm.NovaTrilhaNome = "Matemática Avançada";

            await vm.CriarTrilhaCommand.ExecuteAsync(null);

            _management.Verify(m => m.UpdateTrilhaAsync(7, It.Is<CreateTrilhaRequestDto>(d => d.NomeTrilha == "Matemática Avançada" && d.MateriaID == 2), It.IsAny<CancellationToken>()), Times.Once);
            _content.Verify(c => c.CreateTrilhaAsync(It.IsAny<CreateTrilhaRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.False(vm.EditandoTrilha);
            Assert.Equal(string.Empty, vm.NovaTrilhaNome);
            Assert.Contains("atualizada", vm.StatusMessage);
        }

        [Fact]
        public async Task SaveWhileEditing_WhenApiRejects_KeepsEditingState()
        {
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            vm.EditarTrilhaCommand.Execute(null);
            _management.Setup(m => m.UpdateTrilhaAsync(It.IsAny<int>(), It.IsAny<CreateTrilhaRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Validation, "Matéria inexistente."));

            await vm.CriarTrilhaCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.True(vm.EditandoTrilha);
            Assert.Equal("Matemática", vm.NovaTrilhaNome);
        }

        [Fact]
        public async Task SaveWhileEditing_WithoutManagementService_ShowsError()
        {
            var vm = Create(withManagement: false);
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            vm.EditarTrilhaCommand.Execute(null);

            await vm.CriarTrilhaCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
        }

        [Fact]
        public async Task CancelarEdicao_ClearsFormAndEditingState()
        {
            var vm = Create();
            await vm.InitializeAsync();
            vm.SelectedItem = vm.Itens[0];
            vm.EditarTrilhaCommand.Execute(null);

            vm.CancelarEdicaoCommand.Execute(null);

            Assert.False(vm.EditandoTrilha);
            Assert.Equal(string.Empty, vm.NovaTrilhaNome);
            Assert.Equal("Criar trilha", vm.TextoBotaoSalvar);
        }
    }
}
