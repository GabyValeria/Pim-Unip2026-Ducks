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
    public partial class UserManagementViewModel : ViewModelBase
    {
        public const int TamanhoPagina = 25;
        private const string TodosPapeis = "Todos";

        private readonly IUserAdminApiService _userAdminService;
        private readonly IDialogService _dialogService;
        private readonly IAuthApiService _authService;
        private List<UserDto> _paginaCarregada = new();

        [ObservableProperty]
        private ObservableCollection<UserDto> _usuarios = new();

        [ObservableProperty]
        private UserDto? _selectedUsuario;

        [ObservableProperty]
        private string _filtroTermo = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _opcoesFiltroPapeis = new() { TodosPapeis };

        [ObservableProperty]
        private string _filtroPapelSelecionado = TodosPapeis;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TextoPaginacao))]
        [NotifyCanExecuteChangedFor(nameof(PaginaAnteriorCommand))]
        [NotifyCanExecuteChangedFor(nameof(ProximaPaginaCommand))]
        private int _paginaAtual = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TextoPaginacao))]
        [NotifyCanExecuteChangedFor(nameof(ProximaPaginaCommand))]
        private int _totalPaginas = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TextoPaginacao))]
        private int _totalRegistros;

        public string TextoPaginacao => $"Página {PaginaAtual} de {TotalPaginas} · {TotalRegistros} usuário(s)";

        public UserManagementViewModel(IUserAdminApiService userAdminService, IDialogService dialogService, IAuthApiService authService)
        {
            _userAdminService = userAdminService;
            _dialogService = dialogService;
            _authService = authService;
        }

        public override Task InitializeAsync() => CarregarPaginaAsync(1);

        partial void OnFiltroPapelSelecionadoChanged(string value) => AplicarFiltroPapel();

        [RelayCommand]
        private Task PesquisarAsync() => CarregarPaginaAsync(1);

        [RelayCommand]
        private Task AtualizarAsync() => CarregarPaginaAsync(PaginaAtual);

        private bool PodeVoltar() => PaginaAtual > 1;
        private bool PodeAvancar() => PaginaAtual < TotalPaginas;

        [RelayCommand(CanExecute = nameof(PodeVoltar))]
        private Task PaginaAnteriorAsync() => CarregarPaginaAsync(PaginaAtual - 1);

        [RelayCommand(CanExecute = nameof(PodeAvancar))]
        private Task ProximaPaginaAsync() => CarregarPaginaAsync(PaginaAtual + 1);

        public async Task CarregarPaginaAsync(int pagina)
        {
            PagedResult<UserDto>? resultado = null;
            var ok = await RunAsync(async () =>
            {
                resultado = await _userAdminService.GetUsersAsync(FiltroTermo, Math.Max(1, pagina), TamanhoPagina);
            }, "Carregando usuários...");

            if (!ok || resultado == null) return;

            _paginaCarregada = resultado.Items;
            PaginaAtual = resultado.Page;
            TotalPaginas = resultado.TotalPages;
            TotalRegistros = resultado.Total;
            AtualizarOpcoesPapel();
            AplicarFiltroPapel();

            SetStatus(TotalRegistros == 0 ? "Nenhum usuário encontrado." : string.Empty);
        }

        private void AtualizarOpcoesPapel()
        {
            var selecionado = FiltroPapelSelecionado;
            var papeis = _paginaCarregada.SelectMany(u => u.Roles ?? Array.Empty<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(r => r);

            OpcoesFiltroPapeis = new ObservableCollection<string>(new[] { TodosPapeis }.Concat(papeis));
            FiltroPapelSelecionado = OpcoesFiltroPapeis.Contains(selecionado) ? selecionado : TodosPapeis;
        }

        private void AplicarFiltroPapel()
        {
            IEnumerable<UserDto> resultado = _paginaCarregada;
            if (!string.IsNullOrEmpty(FiltroPapelSelecionado) && FiltroPapelSelecionado != TodosPapeis)
                resultado = resultado.Where(u => u.HasRole(FiltroPapelSelecionado));

            Usuarios = new ObservableCollection<UserDto>(resultado);
        }

        [RelayCommand]
        private async Task AlternarBloqueioAsync(UserDto? usuario)
        {
            if (usuario == null) return;

            if (usuario.UsuarioID == _authService.CurrentUserId)
            {
                SetStatus("Você não pode bloquear a sua própria conta.", true);
                return;
            }

            var bloquear = !usuario.IsBlocked;
            var confirmado = bloquear
                ? _dialogService.ShowConfirmation("Confirmar bloqueio",
                    $"Deseja bloquear o usuário '{usuario.NomeUsuario}'?\nEle perderá o acesso à plataforma até ser desbloqueado.")
                : _dialogService.ShowConfirmation("Confirmar desbloqueio",
                    $"Deseja desbloquear o usuário '{usuario.NomeUsuario}'?");

            if (!confirmado) return;

            var sucesso = await RunAsync(() => _userAdminService.SetBlockedAsync(usuario.UsuarioID, bloquear));
            if (!sucesso) return;

            await CarregarPaginaAsync(PaginaAtual);
            if (!HasError)
                SetStatus(bloquear ? $"Usuário '{usuario.NomeUsuario}' bloqueado." : $"Usuário '{usuario.NomeUsuario}' desbloqueado.");
        }
    }
}
