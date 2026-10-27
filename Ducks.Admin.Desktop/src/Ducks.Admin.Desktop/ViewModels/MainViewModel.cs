using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ducks.Admin.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "Ducks Admin Desktop - Painel Administrativo";

        [ObservableProperty]
        private string _statusMessage = "Sistema inicializado com sucesso via Arquitetura MVVM.";

        [RelayCommand]
        private void RefreshConnection()
        {
            StatusMessage = "Conexão verificada com a API backend da plataforma Ducks.";
        }
    }
}