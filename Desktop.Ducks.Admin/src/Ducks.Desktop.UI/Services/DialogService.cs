using System.IO;
using System.Windows;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.UI.Views;
using Microsoft.Win32;

namespace Ducks.Desktop.UI.Services
{
    public class DialogService : IDialogService
    {
        public bool ShowPromptMotivo(string nomeUsuario, out string motivo)
        {
            var window = new PromptMotivoWindow(nomeUsuario);
            if (window.ShowDialog() == true)
            {
                motivo = window.Motivo;
                return true;
            }
            motivo = string.Empty;
            return false;
        }

        public bool ShowConfirmation(string title, string message)
        {
            var resultado = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return resultado == MessageBoxResult.Yes;
        }

        public void ShowMessage(string title, string message, bool isError = false)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, isError ? MessageBoxImage.Error : MessageBoxImage.Information);
        }

        public bool ShowSaveFileDialog(string defaultFileName, out string savedFilePath)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Salvar Relatório da IA",
                Filter = "Arquivo de Texto (*.txt)|*.txt|Documento PDF (*.pdf)|*.pdf",
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
            if (!string.IsNullOrEmpty(text))
            {
                Clipboard.SetText(text);
            }
        }
    }
}