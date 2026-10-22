using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Admin.Desktop.Models;
using Ducks.Admin.Desktop.Services;
using System.Collections.ObjectModel;

namespace Ducks.Admin.Desktop.ViewModels;

public partial class ContentGamificationViewModel : ObservableObject
{
    private readonly IAdminApiService _adminApiService;

    [ObservableProperty]
    private ObservableCollection<TrilhaAdminDto> _trilhas = new();

    [ObservableProperty]
    private ObservableCollection<ConquistaAdminDto> _conquistas = new();

    [ObservableProperty]
    private bool _isLoading;

    // Propriedades para cadastro de nova Trilha
    [ObservableProperty] private string _novaTrilhaNome = string.Empty;
    [ObservableProperty] private string _novaTrilhaDescricao = string.Empty;
    [ObservableProperty] private int _novoMateriaId = 1;

    // Propriedades para cadastro de nova Conquista
    [ObservableProperty] private string _novaConquistaTitulo = string.Empty;
    [ObservableProperty] private string _novaConquistaDescricao = string.Empty;
    [ObservableProperty] private int _novoXpNecessario = 100;

    public ContentGamificationViewModel(IAdminApiService adminApiService)
    {
        _adminApiService = adminApiService;
        _ = CarregarDadosAsync();
    }

    [RelayCommand]
    public async Task CarregarDadosAsync()
    {
        try
        {
            IsLoading = true;
            
            var trilhasApi = await _adminApiService.ObterTrilhasAsync();
            Trilhas.Clear();
            foreach (var t in trilhasApi) Trilhas.Add(t);

            var conquistasApi = await _adminApiService.ObterConquistasAsync();
            Conquistas.Clear();
            foreach (var c in conquistasApi) Conquistas.Add(c);
        }
        catch (Exception)
        {
            // Tratamento de erro de API
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CriarTrilhaAsync()
    {
        if (string.IsNullOrWhiteSpace(NovaTrilhaNome)) return;

        try
        {
            await _adminApiService.CriarTrilhaAsync(new CriarTrilhaRequestDto
            {
                NomeTrilha = NovaTrilhaNome,
                Descricao = NovaTrilhaDescricao,
                MateriaId = NovoMateriaId
            });

            NovaTrilhaNome = string.Empty;
            NovaTrilhaDescricao = string.Empty;
            await CarregarDadosAsync();
        }
        catch (Exception) { }
    }

    [RelayCommand]
    private async Task AprovarTrilhaAsync(TrilhaAdminDto? trilha)
    {
        if (trilha == null) return;
        try
        {
            await _adminApiService.AprovarTrilhaAsync(trilha.TrilhaId);
            await CarregarDadosAsync();
        }
        catch (Exception) { }
    }

    [RelayCommand]
    private async Task CriarConquistaAsync()
    {
        if (string.IsNullOrWhiteSpace(NovaConquistaTitulo)) return;

        try
        {
            await _adminApiService.CriarConquistaAsync(new CriarConquistaRequestDto
            {
                Titulo = NovaConquistaTitulo,
                Descricao = NovaConquistaDescricao,
                XpNecessario = NovoXpNecessario
            });

            NovaConquistaTitulo = string.Empty;
            NovaConquistaDescricao = string.Empty;
            await CarregarDadosAsync();
        }
        catch (Exception) { }
    }
}