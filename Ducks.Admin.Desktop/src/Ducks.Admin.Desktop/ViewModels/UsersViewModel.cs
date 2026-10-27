using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Admin.Desktop.Models;
using Ducks.Admin.Desktop.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Ducks.Admin.Desktop.ViewModels
{
    public partial class UsersViewModel : ObservableObject
    {
        private readonly IApiClient _apiClient;

        [ObservableProperty]
        private ObservableCollection<UserAdminDto> _usuarios = new();

        [ObservableProperty]
        private UserAdminDto? _usuarioSelecionado;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public UsersViewModel(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [RelayCommand]
        public async Task LoadUsersAsync()
        {
            IsLoading = true;
            StatusMessage = "Carregando usuários...";
            try
            {
                var lista = await _apiClient.GetUsersAsync();
                Usuarios = new ObservableCollection<UserAdminDto>(lista);
                StatusMessage = $"{Usuarios.Count} usuários carregados.";
            }
            catch
            {
                StatusMessage = "Erro ao carregar usuários da API.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ToggleUserStatusAsync(UserAdminDto usuario)
        {
            if (usuario == null) return;

            var sucesso = await _apiClient.ToggleUserStatusAsync(usuario.UsuarioID);
            if (sucesso)
            {
                usuario.EstaAtivo = !usuario.EstaAtivo;
                StatusMessage = $"Status do usuário {usuario.NomeUsuario} atualizado.";
            }
            else
            {
                StatusMessage = "Falha ao alterar status do usuário.";
            }
        }
    }
}