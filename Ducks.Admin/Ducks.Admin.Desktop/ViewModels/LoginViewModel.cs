using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Admin.Desktop.Services;
using Ducks.Admin.Desktop.Stores;

namespace Ducks.Admin.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthApiService _authApiService;
    private readonly UserSessionStore _userSessionStore;
    private readonly NavigationStore _navigationStore;
    private readonly DashboardViewModel _dashboardViewModel;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _senha = string.Empty;

    [ObservableProperty]
    private string? _mensagemErro;

    [ObservableProperty]
    private bool _isLoading;

    public LoginViewModel(
        IAuthApiService authApiService, 
        UserSessionStore userSessionStore, 
        NavigationStore navigationStore,
        DashboardViewModel dashboardViewModel)
    {
        _authApiService = authApiService;
        _userSessionStore = userSessionStore;
        _navigationStore = navigationStore;
        _dashboardViewModel = dashboardViewModel;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            IsLoading = true;
            MensagemErro = null;

            var response = await _authApiService.LoginAsync(new LoginRequestDto(Email, Senha));
            
            _userSessionStore.SetSession(response.Token, Email);
            _navigationStore.CurrentViewModel = _dashboardViewModel;
        }
        catch (Exception)
        {
            MensagemErro = "Falha ao autenticar. Verifique suas credenciais.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}