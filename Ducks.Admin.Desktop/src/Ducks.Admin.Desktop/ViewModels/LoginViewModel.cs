using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Admin.Desktop.Services;
using System;
using System.Threading.Tasks;

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
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    _apiClient.Token = result.Token;
                    ErrorMessage = "Login realizado com sucesso!";
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