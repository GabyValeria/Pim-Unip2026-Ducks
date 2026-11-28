using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels
{
    public partial class RoleManagementViewModel : ViewModelBase
    {
        private readonly IUserAdminApiService _userService;
        private readonly IRoleApiService _roleService;
        private readonly IDialogService _dialogService;
        private readonly IAuthApiService _authService;

        [ObservableProperty]
        private ObservableCollection<UserDto> _usuarios = new();

        [ObservableProperty]
        private ObservableCollection<RoleDto> _papeis = new();

        [ObservableProperty]
        private UserDto? _selectedUsuario;

        [ObservableProperty]
        private RoleDto? _selectedPapel;

        [ObservableProperty]
        private string _filtroTermo = string.Empty;

        public RoleManagementViewModel(
            IUserAdminApiService userService,
            IRoleApiService roleService,
            IDialogService dialogService,
            IAuthApiService authService)
        {
            _userService = userService;
            _roleService = roleService;
            _dialogService = dialogService;
            _authService = authService;
        }

        public override Task InitializeAsync() => CarregarDadosAsync();

        [RelayCommand]
        private Task PesquisarAsync() => CarregarDadosAsync();

        public async Task CarregarDadosAsync()
        {
            PagedResult<UserDto>? usuarios = null;
            System.Collections.Generic.IReadOnlyList<RoleDto>? papeis = null;

            var ok = await RunAsync(async () =>
            {
                papeis = await _roleService.GetRolesAsync();
                usuarios = await _userService.GetUsersAsync(FiltroTermo, 1, 100);
            }, "Carregando usuários e papéis de acesso...");

            if (!ok) return;

            Papeis = new ObservableCollection<RoleDto>(papeis ?? new System.Collections.Generic.List<RoleDto>());
            Usuarios = new ObservableCollection<UserDto>(usuarios?.Items ?? new System.Collections.Generic.List<UserDto>());

            SetStatus(usuarios != null && usuarios.Total > usuarios.Items.Count
                ? $"Exibindo {usuarios.Items.Count} de {usuarios.Total} usuários. Use a busca para refinar."
                : string.Empty);
        }

        [RelayCommand]
        private async Task AtribuirPapelAsync()
        {
            if (SelectedUsuario == null || SelectedPapel == null)
            {
                SetStatus("Selecione um usuário na tabela e o novo papel desejado.", true);
                return;
            }

            var usuario = SelectedUsuario;
            var papel = SelectedPapel;

            if (usuario.UsuarioID == _authService.CurrentUserId)
            {
                SetStatus("Você não pode alterar o seu próprio papel de acesso.", true);
                return;
            }

            var confirmado = _dialogService.ShowConfirmation(
                "Confirmar alteração de papel",
                $"Atribuir o papel '{papel.NomePapel}' ao usuário '{usuario.NomeUsuario}'?\n\n" +
                $"Papéis atuais ({usuario.RolesDisplay}) serão substituídos.");

            if (!confirmado) return;

            var sucesso = await RunAsync(() => _roleService.AssignRoleAsync(usuario.UsuarioID, papel.PapelID));
            if (!sucesso) return;

            await CarregarDadosAsync();
            if (!HasError)
                SetStatus($"Papel '{papel.NomePapel}' atribuído a '{usuario.NomeUsuario}'.");
        }
    }
}
