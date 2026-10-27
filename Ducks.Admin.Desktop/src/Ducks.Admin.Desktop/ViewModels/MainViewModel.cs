using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Ducks.Admin.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private string _applicationTitle = "Ducks Admin Dashboard";
        public string ApplicationTitle
        {
            get => _applicationTitle;
            set => SetProperty(ref _applicationTitle, value);
        }

        private string _statusMessage = "Sistema pronto";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _currentPageTitle = "Dashboard Geral";
        public string CurrentPageTitle
        {
            get => _currentPageTitle;
            set => SetProperty(ref _currentPageTitle, value);
        }

        private string _totalUsuarios = "1.248";
        public string TotalUsuarios
        {
            get => _totalUsuarios;
            set => SetProperty(ref _totalUsuarios, value);
        }

        private string _taxaEngajamento = "87.4%";
        public string TaxaEngajamento
        {
            get => _taxaEngajamento;
            set => SetProperty(ref _taxaEngajamento, value);
        }

        private string _moderacaoPendente = "3 itens";
        public string ModeracaoPendente
        {
            get => _moderacaoPendente;
            set => SetProperty(ref _moderacaoPendente, value);
        }

        private string _cursosAtivos = "14 trilhas";
        public string CursosAtivos
        {
            get => _cursosAtivos;
            set => SetProperty(ref _cursosAtivos, value);
        }

        private string _statusConexao = "🟢 Conectado ao Azure & SignalR Ativo";
        public string StatusConexao
        {
            get => _statusConexao;
            set => SetProperty(ref _statusConexao, value);
        }

        [RelayCommand]
        private async Task RefreshConnectionAsync()
        {
            StatusMessage = "Conexão atualizada com sucesso.";
            await Task.CompletedTask;
        }

        [RelayCommand]
        private void NavigateToDashboard() => CurrentPageTitle = "Dashboard Geral";

        [RelayCommand]
        private void NavigateToUsers() => CurrentPageTitle = "Gerenciamento de Usuários";

        [RelayCommand]
        private void NavigateToModeration() => CurrentPageTitle = "Fila de Moderação";

        [RelayCommand]
        private void NavigateToCourses() => CurrentPageTitle = "Cursos e Trilhas Educacionais";

        [RelayCommand]
        private void NavigateToAI() => CurrentPageTitle = "Assistente de Inteligência Artificial";

        [RelayCommand]
        private void NavigateToSettings() => CurrentPageTitle = "Configurações e Parâmetros";
    }
}