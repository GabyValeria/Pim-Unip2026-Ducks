using System.Windows;

namespace Ducks.Desktop.UI.Views
{
    public partial class PromptMotivoWindow : Window
    {
        public string Motivo { get; private set; } = string.Empty;

        public PromptMotivoWindow(string nomeUsuario)
        {
            InitializeComponent();
            TxtTitulo.Text = $"Tem certeza que deseja bloquear o usuário '{nomeUsuario}'?";
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtMotivo.Text))
            {
                MessageBox.Show("Por favor, informe o motivo do bloqueio.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Motivo = TxtMotivo.Text;
            DialogResult = true;
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}