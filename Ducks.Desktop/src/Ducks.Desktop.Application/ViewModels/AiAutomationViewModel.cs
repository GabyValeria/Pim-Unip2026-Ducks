using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels
{
    public partial class AiAutomationViewModel : ViewModelBase
    {
        public const int LimiteContexto = 4000; // limite do AiPromptRequestDto na API

        private readonly IAiAutomationApiService _aiService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ContadorContexto))]
        private string _contextoEntrada = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _tarefasDisponiveis = new()
        {
            "Análise de Engajamento",
            "Triagem & Resumo de Moderação",
            "Relatório de Desempenho de Cursos",
            "Sugestão de Resposta a Denúncias",
            "Redação de Comunicado Oficial"
        };

        [ObservableProperty]
        private string _tarefaSelecionada = "Análise de Engajamento";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PossuiResultado))]
        private string _resultadoIa = string.Empty;

        public bool PossuiResultado => !string.IsNullOrWhiteSpace(ResultadoIa);
        public string ContadorContexto => $"{ContextoEntrada?.Length ?? 0}/{LimiteContexto}";

        public AiAutomationViewModel(IAiAutomationApiService aiService, IDialogService dialogService)
        {
            _aiService = aiService;
            _dialogService = dialogService;
        }

        [RelayCommand]
        private async Task ExecutarAssistenteAsync()
        {
            var contexto = ContextoEntrada?.Trim() ?? string.Empty;
            if (contexto.Length == 0)
            {
                SetStatus("Insira um contexto, dados ou texto para a IA analisar.", true);
                return;
            }
            if (contexto.Length > LimiteContexto)
            {
                SetStatus($"O contexto excede o limite de {LimiteContexto} caracteres ({contexto.Length}).", true);
                return;
            }

            AiResponseDto? resposta = null;
            var ok = await RunAsync(async () =>
            {
                resposta = await _aiService.ProcessarTarefaAdminAsync(new AiPromptRequestDto
                {
                    Contexto = contexto,
                    TarefaAdmin = TarefaSelecionada
                });
            }, "Processando solicitação com a IA...");

            if (!ok || resposta == null) return;

            if (resposta.Sucesso && !string.IsNullOrWhiteSpace(resposta.ResultadoGerado))
            {
                ResultadoIa = resposta.ResultadoGerado;
                SetStatus("Análise concluída com sucesso.");
            }
            else
            {
                SetStatus(string.IsNullOrWhiteSpace(resposta.MensagemErro)
                    ? "A IA não retornou um resultado. Tente novamente."
                    : resposta.MensagemErro, true);
            }
        }

        [RelayCommand]
        private void SalvarRelatorio()
        {
            if (!PossuiResultado)
            {
                _dialogService.ShowMessage("Aviso", "Execute uma análise primeiro para poder salvar o relatório.");
                return;
            }

            var nomePadrao = $"Relatorio_IA_Ducks_{DateTime.Now:yyyyMMdd_HHmm}.txt";
            if (!_dialogService.ShowSaveFileDialog(nomePadrao, out var caminho)) return;

            try
            {
                File.WriteAllText(caminho, ResultadoIa);
                SetStatus($"Relatório salvo em: {caminho}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                SetStatus($"Não foi possível salvar o arquivo: {ex.Message}", true);
            }
        }

        [RelayCommand]
        private void CopiarResultado()
        {
            if (!PossuiResultado) return;
            _dialogService.SetClipboardText(ResultadoIa);
            SetStatus("Resultado copiado para a área de transferência.");
        }
    }
}
