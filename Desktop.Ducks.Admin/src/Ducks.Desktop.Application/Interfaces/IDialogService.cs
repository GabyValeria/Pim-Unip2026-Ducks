namespace Ducks.Desktop.Application.Interfaces
{
    public interface IDialogService
    {
        bool ShowPromptMotivo(string nomeUsuario, out string motivo);
        bool ShowConfirmation(string title, string message);
        void ShowMessage(string title, string message, bool isError = false);
        bool ShowSaveFileDialog(string defaultFileName, out string savedFilePath);
        void SetClipboardText(string text);
    }
}