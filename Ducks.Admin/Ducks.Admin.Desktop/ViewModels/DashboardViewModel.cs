using CommunityToolkit.Mvvm.ComponentModel;

namespace Ducks.Admin.Desktop.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string _welcomeMessage = "Bem-vindo ao Painel Administrativo do Ecossistema Ducks 🦆";
}