using System.Windows;

namespace Ducks.Desktop.UI.Views
{
    /// <summary>Diálogo genérico que exige um texto (ex.: justificativa de rejeição/remoção).</summary>
    public partial class PromptMotivoWindow : Window
    {
        public string Texto { get; private set; } = string.Empty;

        public PromptMotivoWindow(string titulo, string mensagem, string textoConfirmar)
        {
            InitializeComponent();
            Title = titulo;
            TxtTitulo.Text = titulo;
            TxtMensagem.Text = mensagem;
            BtnConfirmar.Content = textoConfirmar;
            Loaded += (_, _) => TxtTexto.Focus();
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            var texto = TxtTexto.Text?.Trim() ?? string.Empty;
            if (texto.Length == 0)
            {
                TxtErro.Text = "Este campo é obrigatório.";
                TxtErro.Visibility = Visibility.Visible;
                return;
            }

            Texto = texto;
            DialogResult = true;
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
