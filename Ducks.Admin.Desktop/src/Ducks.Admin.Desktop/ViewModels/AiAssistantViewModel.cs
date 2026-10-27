using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Admin.Desktop.Services;
using System;
using System.Threading.Tasks;

namespace Ducks.Admin.Desktop.ViewModels
{
    public partial class AiAssistantViewModel : ObservableObject
    {
        private readonly IAiService _aiService;

        [ObservableProperty]
        private string _inputPrompt = string.Empty;

        [ObservableProperty]
        private string _aiResponse = "Assistente IA Ducks pronto para auxiliar nas tarefas administrativas.";

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public AiAssistantViewModel(IAiService aiService)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        }

        [RelayCommand]
        private async Task SummarizeModerationAsync()
        {
            IsProcessing = true;
            StatusMessage = "Processando sumarização da fila de moderação...";
            try
            {
                AiResponse = await _aiService.SummarizeModerationQueueAsync("Fila de teste contendo 5 itens pendentes.");
                StatusMessage = "Sumarização concluída com sucesso.";
            }
            catch
            {
                StatusMessage = "Erro ao executar IA.";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private async Task GenerateSuggestionsAsync()
        {
            if (string.IsNullOrWhiteSpace(InputPrompt))
            {
                StatusMessage = "Digite um tema para gerar sugestões.";
                return;
            }

            IsProcessing = true;
            StatusMessage = $"Gerando sugestões para '{InputPrompt}'...";
            try
            {
                AiResponse = await _aiService.GenerateActivitySuggestionsAsync(InputPrompt);
                StatusMessage = "Sugestões geradas com sucesso.";
            }
            catch
            {
                StatusMessage = "Erro ao conectar com o serviço de IA.";
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}