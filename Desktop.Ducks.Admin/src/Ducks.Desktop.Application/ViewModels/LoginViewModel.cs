using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;

namespace Ducks.Desktop.Application.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthApiService _authApiService;
        private readonly Action? _onLoginSuccess;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _senha = string.Empty;

        [ObservableProperty]
        private string _mensagemErro = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasError;

        public IAsyncRelayCommand LoginCommand { get; }

        public LoginViewModel(IAuthApiService authApiService, Action? onLoginSuccess = null)
        {
            _authApiService = authApiService;
            _onLoginSuccess = onLoginSuccess;
            LoginCommand = new AsyncRelayCommand(ExecutarLoginAsync);
        }

        private async Task ExecutarLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
            {
                MensagemErro = "Por favor, preencha o e-mail e a senha.";
                HasError = true;
                return;
            }

            IsLoading = true;
            HasError = false;
            MensagemErro = string.Empty;

            try
            {
                var resposta = await _authApiService.LoginAsync(Email, Senha);

                if (resposta != null && resposta.Sucesso)
                {
                    _onLoginSuccess?.Invoke();
                }
                else
                {
                    MensagemErro = resposta?.Mensagem ?? "Credenciais inválidas ou acesso não autorizado.";
                    HasError = true;
                }
            }
            catch (Exception ex)
            {
                MensagemErro = $"Detalhe: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}