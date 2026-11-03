using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels
{
    public partial class UserManagementViewModel : ObservableObject
    {
        private readonly IUserAdminApiService _userAdminService;
        private readonly IDialogService _dialogService;
        private List<UserDto> _todosUsuarios = new();

        [ObservableProperty]
        private ObservableCollection<UserDto> _usuarios = new();

        [ObservableProperty]
        private UserDto? _selectedUsuario;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _filtroTermo = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _opcoesFiltroPapeis = new() { "Todos", "Aluno", "Moderador", "Curador", "Administrador" };

        [ObservableProperty]
        private string _filtroPapelSelecionado = "Todos";

        [ObservableProperty]
        private string _mensagemStatus = string.Empty;

        public IAsyncRelayCommand CarregarUsuariosCommand { get; }
        public IAsyncRelayCommand<UserDto> ToggleBlockCommand { get; }

        public UserManagementViewModel(IUserAdminApiService userAdminService, IDialogService dialogService)
        {
            _userAdminService = userAdminService;
            _dialogService = dialogService;
            CarregarUsuariosCommand = new AsyncRelayCommand(CarregarUsuariosAsync);
            ToggleBlockCommand = new AsyncRelayCommand<UserDto>(ToggleBlockAsync);

            _ = CarregarUsuariosAsync();
        }

        partial void OnFiltroPapelSelecionadoChanged(string value)
        {
            AplicarFiltros();
        }

        partial void OnFiltroTermoChanged(string value)
        {
            AplicarFiltros();
        }

        public async Task CarregarUsuariosAsync()
        {
            IsLoading = true;
            MensagemStatus = "Carregando usuários...";
            try
            {
                var lista = await _userAdminService.GetUsersAsync();
                _todosUsuarios = lista.ToList();
                AplicarFiltros();
                MensagemStatus = "Usuários carregados com sucesso.";
            }
            catch (Exception ex)
            {
                MensagemStatus = $"Erro ao carregar usuários: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AplicarFiltros()
        {
            var resultado = _todosUsuarios.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(FiltroTermo))
            {
                resultado = resultado.Where(u => 
                    u.NomeUsuario.Contains(FiltroTermo, StringComparison.OrdinalIgnoreCase) || 
                    u.Email.Contains(FiltroTermo, StringComparison.OrdinalIgnoreCase));
            }

            if (FiltroPapelSelecionado != "Todos")
            {
                resultado = resultado.Where(u => string.Equals(u.Role, FiltroPapelSelecionado, StringComparison.OrdinalIgnoreCase));
            }

            Usuarios = new ObservableCollection<UserDto>(resultado);
        }

        private async Task ToggleBlockAsync(UserDto? usuario)
        {
            if (usuario == null) return;

            bool novoStatus = !usuario.IsBlocked;

            if (novoStatus)
            {
                // Solicita motivo e confirmação para bloqueio
                if (_dialogService.ShowPromptMotivo(usuario.NomeUsuario, out string motivo))
                {
                    bool sucesso = await _userAdminService.ToggleBlockUserAsync(usuario.UsuarioID, novoStatus);

                    if (sucesso)
                    {
                        usuario.IsBlocked = novoStatus;
                        MensagemStatus = $"Usuário bloqueado. Motivo: {motivo}";
                        AplicarFiltros();
                    }
                    else
                    {
                        MensagemStatus = "Falha ao processar o bloqueio na API.";
                    }
                }
            }
            else
            {
                // Confirmação para desbloqueio
                bool confirmado = _dialogService.ShowConfirmation("Confirmação de Desbloqueio", $"Tem certeza que deseja desbloquear o usuário '{usuario.NomeUsuario}'?");

                if (confirmado)
                {
                    bool sucesso = await _userAdminService.ToggleBlockUserAsync(usuario.UsuarioID, novoStatus);

                    if (sucesso)
                    {
                        usuario.IsBlocked = novoStatus;
                        MensagemStatus = "Usuário desbloqueado com sucesso.";
                        AplicarFiltros();
                    }
                    else
                    {
                        MensagemStatus = "Falha ao processar o desbloqueio na API.";
                    }
                }
            }
        }
    }
}