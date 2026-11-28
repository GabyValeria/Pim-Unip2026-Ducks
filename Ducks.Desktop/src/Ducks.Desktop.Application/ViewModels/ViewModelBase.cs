using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Ducks.Desktop.Application.Http;

namespace Ducks.Desktop.Application.ViewModels
{
    /// <summary>
    /// Base dos módulos do painel: estado de carregamento, mensagem de status/erro e execução
    /// protegida de chamadas à API (ApiException vira mensagem amigável, nunca derruba o app).
    /// </summary>
    public abstract partial class ViewModelBase : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        /// <summary>Chamado pela navegação após a tela ser exibida (substitui chamadas no construtor).</summary>
        public virtual Task InitializeAsync() => Task.CompletedTask;

        protected void SetStatus(string message, bool isError = false)
        {
            StatusMessage = message;
            HasError = isError;
        }

        /// <summary>Executa a ação com indicador de carregamento. Retorna true se concluiu sem erro.</summary>
        protected async Task<bool> RunAsync(Func<Task> action, string? loadingMessage = null, string? successMessage = null)
        {
            IsLoading = true;
            HasError = false;
            if (loadingMessage != null) StatusMessage = loadingMessage;

            try
            {
                await action();
                StatusMessage = successMessage ?? string.Empty;
                return true;
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message, true);
                return false;
            }
            catch (OperationCanceledException)
            {
                SetStatus("Operação cancelada.");
                return false;
            }
            catch (Exception ex)
            {
                SetStatus($"Ocorreu um erro inesperado: {ex.Message}", true);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
