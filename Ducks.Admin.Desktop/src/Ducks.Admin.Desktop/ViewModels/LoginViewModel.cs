using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Admin.Desktop.Services;
using Ducks.Admin.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Ducks.Admin.Desktop.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IApiClient _apiClient;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _senha = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public LoginViewModel(IApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
            {
                ErrorMessage = "Preencha o e-mail e a senha.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _apiClient.LoginAsync(Email, Senha);
                
                if (result != null && (result.Sucesso || !string.IsNullOrEmpty(result.Token)))
                {
                    _apiClient.Token = result.Token;
                    ErrorMessage = "Login realizado com sucesso!";

                    // Abre o Dashboard Principal (MainWindow) e fecha a tela de login atual
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var mainWindow = App.ServiceProvider?.GetRequiredService<MainWindow>();
                        mainWindow?.Show();

                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is LoginWindow)
                            {
                                window.Close();
                                break;
                            }
                        }
                    });
                }
                else
                {
                    ErrorMessage = result?.Mensagem ?? "Credenciais inválidas.";
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Erro de conexão com o servidor backend.";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}