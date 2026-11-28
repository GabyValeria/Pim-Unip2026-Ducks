namespace Ducks.Desktop.Application.Interfaces
{
    public interface IDialogService
    {
        /// <summary>Solicita um texto obrigatório (ex.: justificativa). Retorna false se cancelado.</summary>
        bool ShowPrompt(string title, string message, string confirmText, out string text);
        bool ShowConfirmation(string title, string message);
        void ShowMessage(string title, string message, bool isError = false);
        bool ShowSaveFileDialog(string defaultFileName, out string savedFilePath);
        void SetClipboardText(string text);
    }
}
