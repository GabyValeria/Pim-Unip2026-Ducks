using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.Core.Models;
using Moq;

namespace Ducks.Desktop.Tests
{
    public class PlatformSettingsViewModelTests
    {
        private readonly Mock<ISettingsApiService> _service = new();
        private readonly Mock<IDialogService> _dialog = new();

        private PlatformSettingsViewModel Create()
        {
            _service.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lists.Of(new SettingDto { Chave = "site.nome", Valor = "Ducks" }));
            return new PlatformSettingsViewModel(_service.Object, _dialog.Object);
        }

        [Fact]
        public async Task InitializeAsync_LoadsSettings()
        {
            var vm = Create();

            await vm.InitializeAsync();

            Assert.Single(vm.Parametros);
            Assert.False(vm.HasError);
        }

        [Fact]
        public async Task SelectingSetting_FillsEditor()
        {
            var vm = Create();
            await vm.InitializeAsync();

            vm.SelectedParametro = vm.Parametros[0];

            Assert.Equal("site.nome", vm.Chave);
            Assert.Equal("Ducks", vm.Valor);
        }

        [Theory]
        [InlineData("a", "valor", false)]
        [InlineData("site.nome", "", false)]
        [InlineData("site.nome", "   ", false)]
        [InlineData("api_key", "x", false)]
        [InlineData("OpenAI:ApiKey", "x", false)]
        [InlineData("Jwt.Secret", "x", false)]
        [InlineData("db.password", "x", false)]
        [InlineData("auth.Token.ttl", "x", false)]
        [InlineData("DefaultConnection", "x", false)]
        [InlineData("site.nome", "Ducks", true)]
        public void ValidateInput_AppliesApiRules(string chave, string valor, bool valido)
        {
            var vm = Create();
            vm.Chave = chave;
            vm.Valor = valor;

            Assert.Equal(valido, vm.ValidarEntrada() == null);
        }

        [Fact]
        public void ValidateInput_RejectsTooLongValueAndKey()
        {
            var vm = Create();
            vm.Chave = "site.nome";
            vm.Valor = new string('x', 4001);
            Assert.NotNull(vm.ValidarEntrada());

            vm.Chave = new string('k', 101);
            vm.Valor = "ok";
            Assert.NotNull(vm.ValidarEntrada());
        }

        [Fact]
        public async Task Save_WithSensitiveKey_IsBlockedBeforeCallingApi()
        {
            var vm = Create();
            vm.Chave = "OpenAI__ApiKey";
            vm.Valor = "sk-123";

            await vm.SalvarParametroCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.SaveSettingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _dialog.Verify(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Save_WhenNotConfirmed_DoesNothing()
        {
            var vm = Create();
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            vm.Chave = "site.nome";
            vm.Valor = "Novo";

            await vm.SalvarParametroCommand.ExecuteAsync(null);

            _service.Verify(s => s.SaveSettingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Save_Confirmed_SendsTrimmedKeyAndReloads()
        {
            var vm = Create();
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            vm.Chave = "  site.nome ";
            vm.Valor = "Ducks Novo";

            await vm.SalvarParametroCommand.ExecuteAsync(null);

            _service.Verify(s => s.SaveSettingAsync("site.nome", "Ducks Novo", It.IsAny<CancellationToken>()), Times.Once);
            _service.Verify(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains("salvo com sucesso", vm.StatusMessage);
        }

        [Fact]
        public async Task Save_WhenApiRejects_ShowsApiMessage()
        {
            var vm = Create();
            _dialog.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _service.Setup(s => s.SaveSettingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Validation, "Chave protegida."));
            vm.Chave = "site.nome";
            vm.Valor = "x";

            await vm.SalvarParametroCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Equal("Chave protegida.", vm.StatusMessage);
        }

        [Fact]
        public void NewSetting_ClearsEditorAndSelection()
        {
            var vm = Create();
            vm.Chave = "x";
            vm.Valor = "y";

            vm.NovoParametroCommand.Execute(null);

            Assert.Equal(string.Empty, vm.Chave);
            Assert.Equal(string.Empty, vm.Valor);
            Assert.Null(vm.SelectedParametro);
        }
    }

    public class AiAutomationViewModelTests
    {
        private readonly Mock<IAiAutomationApiService> _service = new();
        private readonly Mock<IDialogService> _dialog = new();

        private AiAutomationViewModel Create() => new(_service.Object, _dialog.Object);

        [Fact]
        public async Task Execute_WithEmptyContext_ShowsErrorAndSkipsApi()
        {
            var vm = Create();
            vm.ContextoEntrada = "   ";

            await vm.ExecutarAssistenteCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            _service.Verify(s => s.ProcessarTarefaAdminAsync(It.IsAny<AiPromptRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Execute_WithContextAboveLimit_ShowsErrorAndSkipsApi()
        {
            var vm = Create();
            vm.ContextoEntrada = new string('x', AiAutomationViewModel.LimiteContexto + 1);

            await vm.ExecutarAssistenteCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Contains("limite", vm.StatusMessage);
            _service.Verify(s => s.ProcessarTarefaAdminAsync(It.IsAny<AiPromptRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Execute_Success_ShowsGeneratedText()
        {
            _service.Setup(s => s.ProcessarTarefaAdminAsync(
                    It.Is<AiPromptRequestDto>(r => r.Contexto == "dados de engajamento" && r.TarefaAdmin == "Análise de Engajamento"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiResponseDto { Sucesso = true, ResultadoGerado = "Resumo gerado" });
            var vm = Create();
            vm.ContextoEntrada = "  dados de engajamento ";

            await vm.ExecutarAssistenteCommand.ExecuteAsync(null);

            Assert.Equal("Resumo gerado", vm.ResultadoIa);
            Assert.True(vm.PossuiResultado);
            Assert.False(vm.HasError);
        }

        [Fact]
        public async Task Execute_WhenAiReportsFailure_ShowsServerMessage()
        {
            _service.Setup(s => s.ProcessarTarefaAdminAsync(It.IsAny<AiPromptRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiResponseDto { Sucesso = false, MensagemErro = "Integração de IA não configurada no servidor." });
            var vm = Create();
            vm.ContextoEntrada = "x";

            await vm.ExecutarAssistenteCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Equal("Integração de IA não configurada no servidor.", vm.StatusMessage);
            Assert.False(vm.PossuiResultado);
        }

        [Fact]
        public async Task Execute_WhenRequestThrows_ShowsFriendlyError()
        {
            _service.Setup(s => s.ProcessarTarefaAdminAsync(It.IsAny<AiPromptRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Timeout, "A API demorou demais para responder."));
            var vm = Create();
            vm.ContextoEntrada = "x";

            await vm.ExecutarAssistenteCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.Equal("A API demorou demais para responder.", vm.StatusMessage);
            Assert.False(vm.IsLoading);
        }

        [Fact]
        public void ContextCounter_ReflectsTypedLength()
        {
            var vm = Create();
            vm.ContextoEntrada = "abc";

            Assert.Equal("3/4000", vm.ContadorContexto);
        }

        [Fact]
        public void SaveReport_WithoutResult_ShowsMessageOnly()
        {
            var vm = Create();

            vm.SalvarRelatorioCommand.Execute(null);

            _dialog.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public async Task SaveReport_WritesResultToChosenFile()
        {
            _service.Setup(s => s.ProcessarTarefaAdminAsync(It.IsAny<AiPromptRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiResponseDto { Sucesso = true, ResultadoGerado = "Conteúdo do relatório" });
            var caminho = Path.Combine(Path.GetTempPath(), $"ducks-relatorio-{Guid.NewGuid():N}.txt");
            _dialog.Setup(d => d.ShowSaveFileDialog(It.IsAny<string>(), out caminho)).Returns(true);
            var vm = Create();
            vm.ContextoEntrada = "x";
            await vm.ExecutarAssistenteCommand.ExecuteAsync(null);

            try
            {
                vm.SalvarRelatorioCommand.Execute(null);

                Assert.Equal("Conteúdo do relatório", File.ReadAllText(caminho));
                Assert.Contains("salvo", vm.StatusMessage);
            }
            finally
            {
                if (File.Exists(caminho)) File.Delete(caminho);
            }
        }

        [Fact]
        public void SaveReport_WhenFileCannotBeWritten_ShowsErrorInsteadOfCrashing()
        {
            var vm = Create();
            vm.ResultadoIa = "texto";
            var caminhoInvalido = Path.Combine(Path.GetTempPath(), $"nao-existe-{Guid.NewGuid():N}", "relatorio.txt");
            _dialog.Setup(d => d.ShowSaveFileDialog(It.IsAny<string>(), out caminhoInvalido)).Returns(true);

            vm.SalvarRelatorioCommand.Execute(null);

            Assert.True(vm.HasError);
        }

        [Fact]
        public void CopyResult_SendsTextToClipboard()
        {
            var vm = Create();
            vm.ResultadoIa = "resultado";

            vm.CopiarResultadoCommand.Execute(null);

            _dialog.Verify(d => d.SetClipboardText("resultado"), Times.Once);
        }

        [Fact]
        public void CopyResult_WithoutResult_DoesNothing()
        {
            var vm = Create();

            vm.CopiarResultadoCommand.Execute(null);

            _dialog.Verify(d => d.SetClipboardText(It.IsAny<string>()), Times.Never);
        }
    }

    public class AuditLogsViewModelTests
    {
        private readonly Mock<IAuditApiService> _service = new();

        private AuditLogsViewModel Create(params AuditLogDto[] logs)
        {
            _service.Setup(s => s.GetLogsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Lists.Of(logs));
            return new AuditLogsViewModel(_service.Object);
        }

        private static AuditLogDto Log(int id, string usuario, string acao, string detalhes) =>
            new() { LogID = id, Usuario = usuario, Acao = acao, Detalhes = detalhes, DataHora = DateTime.UtcNow };

        [Fact]
        public async Task InitializeAsync_LoadsLogs()
        {
            var vm = Create(Log(1, "1", "USUARIO_BLOQUEADO", "UsuarioID: 5"), Log(2, "1", "TRILHA_CRIADA", "Matemática"));

            await vm.InitializeAsync();

            Assert.Equal(2, vm.Logs.Count);
            Assert.Contains("2 registro", vm.StatusMessage);
        }

        [Fact]
        public async Task InitializeAsync_WithoutLogs_ShowsEmptyMessage()
        {
            var vm = Create();

            await vm.InitializeAsync();

            Assert.Equal("Nenhum registro de auditoria encontrado.", vm.StatusMessage);
        }

        [Theory]
        [InlineData("bloqueado", 1)]
        [InlineData("MATEMÁTICA", 1)]
        [InlineData("1", 2)]
        [InlineData("inexistente", 0)]
        public async Task Filter_MatchesUserActionOrDetailsIgnoringCase(string termo, int esperados)
        {
            var vm = Create(Log(1, "1", "USUARIO_BLOQUEADO", "UsuarioID: 5"), Log(2, "1", "TRILHA_CRIADA", "Matemática"));
            await vm.InitializeAsync();

            vm.FiltroTermo = termo;

            Assert.Equal(esperados, vm.Logs.Count);
        }

        [Fact]
        public async Task ClearingFilter_RestoresAllLogs()
        {
            var vm = Create(Log(1, "1", "A", "x"), Log(2, "2", "B", "y"));
            await vm.InitializeAsync();
            vm.FiltroTermo = "B";

            vm.FiltroTermo = string.Empty;

            Assert.Equal(2, vm.Logs.Count);
        }

        [Fact]
        public async Task Load_WhenApiFails_ShowsError()
        {
            var vm = Create();
            _service.Setup(s => s.GetLogsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(ApiErrorKind.Forbidden, "Você não tem permissão para realizar esta ação."));

            await vm.InitializeAsync();

            Assert.True(vm.HasError);
        }
    }
}
