using Ducks.Admin.Desktop.ViewModels;
using System.Windows;

namespace Ducks.Admin.Desktop.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Sincroniza o PasswordBox com a ViewModel de forma segura
            TxtPassword.PasswordChanged += (s, e) => {
                _viewModel.Senha = TxtPassword.Password;
            };
        }
    }
}