using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Admin.Desktop.Models;
using Ducks.Admin.Desktop.Services;
using System.Collections.ObjectModel;

namespace Ducks.Admin.Desktop.ViewModels;

public partial class UserManagementViewModel : ObservableObject
{
    private readonly IAdminApiService _adminApiService;

    [ObservableProperty]
    private ObservableCollection<UsuarioAdminDto> _usuarios = new();

    [ObservableProperty]
    private string _termoBusca = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _paginaAtual = 1;

    [ObservableProperty]
    private int _totalPaginas = 1;

    [ObservableProperty]
    private UsuarioAdminDto? _usuarioSelecionado;

    public UserManagementViewModel(IAdminApiService adminApiService)
    {
        _adminApiService = adminApiService;
        _ = CarregarUsuariosAsync();
    }

    [RelayCommand]
    public async Task CarregarUsuariosAsync()
    {
        try
        {
            IsLoading = true;
            var resultado = await _adminApiService.ObterUsuariosAsync(TermoBusca, PaginaAtual, 10);
            
            Usuarios.Clear();
            foreach (var usuario in resultado.Items)
            {
                Usuarios.Add(usuario);
            }

            TotalPaginas = Math.Max(1, (int)Math.Ceiling((double)resultado.TotalCount / resultado.PageSize));
        }
        catch (Exception)
        {
            // Tratamento de erro de comunicação com a API
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AlternarStatusAsync(UsuarioAdminDto? usuario)
    {
        if (usuario == null) return;

        try
        {
            var novoStatus = !usuario.EstaAtivo;
            await _adminApiService.AlterarStatusUsuarioAsync(usuario.UsuarioId, new AlterarStatusRequestDto { EstaAtivo = novoStatus });
            usuario.EstaAtivo = novoStatus;
            
            // Notifica alteração na coleção
            int index = Usuarios.IndexOf(usuario);
            if (index >= 0) Usuarios[index] = usuario;
        }
        catch (Exception)
        {
            // Tratamento de erro ao moderar usuário
        }
    }

    [RelayCommand]
    private async Task ProximaPaginaAsync()
    {
        if (PaginaAtual < TotalPaginas)
        {
            PaginaAtual++;
            await CarregarUsuariosAsync();
        }
    }

    [RelayCommand]
    private async Task PaginaAnteriorAsync()
    {
        if (PaginaAtual > 1)
        {
            PaginaAtual--;
            await CarregarUsuariosAsync();
        }
    }
}