using System;
using System.Net.Mail;
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

        private static bool EmailValido(string email)
        {
            return MailAddress.TryCreate(email.Trim(), out var address) && address.Address == email.Trim();
        }

        private async Task ExecutarLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
            {
                Falhar("Por favor, preencha o e-mail e a senha.");
                return;
            }

            if (!EmailValido(Email))
            {
                Falhar("Informe um e-mail válido.");
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
                    Falhar(string.IsNullOrWhiteSpace(resposta?.Mensagem)
                        ? "E-mail ou senha inválidos."
                        : resposta!.Mensagem);
                }
            }
            catch (Exception ex)
            {
                Falhar($"Não foi possível concluir o login: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Falhar(string mensagem)
        {
            MensagemErro = mensagem;
            HasError = true;
        }
    }
}
