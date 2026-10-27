using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Ducks.Admin.Desktop.ViewModels
{
    public partial class ConfiguracoesViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _apiUrl = "https://ducks-api-sistema-hfbndmc0cudwgcg6.brazilsouth-01.azurewebsites.net/";

        [ObservableProperty]
        private bool _modoManutencao;

        [ObservableProperty]
        private bool _notificacoesSignalRAtivas = true;

        [ObservableProperty]
        private string _statusConfig = "Parâmetros carregados.";

        [RelayCommand]
        private async Task SalvarConfiguracoesAsync()
        {
            StatusConfig = "Salvando parâmetros da plataforma no Azure...";
            await Task.Delay(500);
            StatusConfig = "Configurações atualizadas e aplicadas com sucesso!";
        }
    }
}