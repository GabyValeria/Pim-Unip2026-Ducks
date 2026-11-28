using System.Windows;
using Ducks.Desktop.Application.ViewModels;

namespace Ducks.Desktop.UI.Views
{
    public partial class LoginView : Window
    {
        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // PasswordBox não suporta binding por segurança; apenas repassa o valor ao ViewModel.
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Senha = PasswordBox.Password;
            }
        }
    }
}
