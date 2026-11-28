using System.Windows;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.UI.Views;
using Microsoft.Win32;

namespace Ducks.Desktop.UI.Services
{
    /// <summary>Implementação WPF de IDialogService. Mantém MessageBox/diálogos fora dos ViewModels.</summary>
    public class DialogService : IDialogService
    {
        public bool ShowPrompt(string title, string message, string confirmText, out string text)
        {
            var window = new PromptMotivoWindow(title, message, confirmText);
            AssociarProprietario(window);

            if (window.ShowDialog() == true)
            {
                text = window.Texto;
                return true;
            }

            text = string.Empty;
            return false;
        }

        public bool ShowConfirmation(string title, string message)
        {
            var resultado = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return resultado == MessageBoxResult.Yes;
        }

        public void ShowMessage(string title, string message, bool isError = false)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK,
                isError ? MessageBoxImage.Error : MessageBoxImage.Information);
        }

        public bool ShowSaveFileDialog(string defaultFileName, out string savedFilePath)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Salvar relatório da IA",
                Filter = "Arquivo de texto (*.txt)|*.txt",
                FileName = defaultFileName
            };

            if (dialog.ShowDialog() == true)
            {
                savedFilePath = dialog.FileName;
                return true;
            }

            savedFilePath = string.Empty;
            return false;
        }

        public void SetClipboardText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                Clipboard.SetText(text);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // A área de transferência pode estar bloqueada por outro processo; não deve derrubar o app.
                ShowMessage("Área de transferência", "Não foi possível copiar agora. Tente novamente.", true);
            }
        }

        private static void AssociarProprietario(Window window)
        {
            var principal = System.Windows.Application.Current?.MainWindow;
            if (principal is { IsVisible: true } && !ReferenceEquals(principal, window))
            {
                window.Owner = principal;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
        }
    }
}
