using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Ducks.Desktop.Application.ViewModels
{
    /// <summary>Shell do painel: navegação entre módulos, cabeçalho, busca global e sessão.</summary>
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthApiService _authService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _title = "Ducks Admin";

        [ObservableProperty]
        private object? _viewModelAtual;

        [ObservableProperty]
        private string _tituloPagina = "Painel Administrativo";

        [ObservableProperty]
        private string _subtituloPagina = "Gerencie usuários, métricas e relatórios da plataforma.";

        [ObservableProperty]
        private string _termoBusca = string.Empty;

        /// <summary>Nome do tipo do ViewModel exibido; usado para destacar o item do menu lateral.</summary>
        [ObservableProperty]
        private string _moduloAtivo = string.Empty;

        public string NomeUsuario => _authService.CurrentUserName ?? "Administrador";

        public string InicialUsuario =>
            string.IsNullOrWhiteSpace(_authService.CurrentUserName) ? "A" : _authService.CurrentUserName.Trim()[..1].ToUpperInvariant();

        /// <summary>Usuário clicou em sair: a UI deve voltar para a tela de login.</summary>
        public event Action? LogoutRequested;

        /// <summary>A sessão expirou e não pôde ser renovada: a UI deve avisar e voltar ao login.</summary>
        public event Action? SessionExpiredRequested;

        public MainViewModel(IServiceProvider serviceProvider, IAuthApiService authService, IDialogService dialogService)
        {
            _serviceProvider = serviceProvider;
            _authService = authService;
            _dialogService = dialogService;
            _authService.SessionExpired += OnSessionExpired;
        }

        private void OnSessionExpired() => SessionExpiredRequested?.Invoke();

        public void Dispose()
        {
            _authService.SessionExpired -= OnSessionExpired;
            DescartarAtual();
        }

        private void DescartarAtual()
        {
            if (ViewModelAtual is IDisposable disposable)
            {
                try { disposable.Dispose(); } catch { /* encerramento best-effort */ }
            }
        }

        /// <summary>
        /// Carrega o módulo. Qualquer falha de resolução/inicialização é exibida ao usuário e o painel continua estável.
        /// </summary>
        public async Task NavegarAsync<TViewModel>(string titulo, string subtitulo, Action<TViewModel>? configurar = null)
            where TViewModel : ViewModelBase
        {
            try
            {
                var vm = _serviceProvider.GetRequiredService<TViewModel>();
                configurar?.Invoke(vm);

                DescartarAtual();
                TituloPagina = titulo;
                SubtituloPagina = subtitulo;
                ViewModelAtual = vm;
                ModuloAtivo = typeof(TViewModel).Name;

                await vm.InitializeAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(
                    "Ducks Admin - Aviso de Navegação",
                    $"Não foi possível carregar o módulo selecionado.\n\nDetalhes: {ex.InnerException?.Message ?? ex.Message}",
                    true);
            }
        }

        [RelayCommand]
        private Task NavegarVisaoGeralAsync() => NavegarAsync<MetricsDashboardViewModel>(
            "Painel Administrativo", "Gerencie usuários, métricas e relatórios da plataforma.");

        [RelayCommand]
        private Task NavegarUsuariosAsync() => NavegarAsync<UserManagementViewModel>(
            "Usuários", "Consulte cadastros e bloqueie ou desbloqueie contas.");

        [RelayCommand]
        private Task NavegarModeracaoAsync() => NavegarAsync<ModerationViewModel>(
            "Fila de Moderação", "Revise, aprove, rejeite ou remova conteúdos enviados.");

        [RelayCommand]
        private Task NavegarCursosTrilhasAsync() => NavegarAsync<CoursesAndTrailsViewModel>(
            "Cursos e Trilhas", "Gerencie o catálogo de trilhas de aprendizagem.");

        [RelayCommand]
        private Task NavegarModulosAulasAsync() => NavegarAsync<ContentStructureViewModel>(
            "Módulos e Aulas", "Organize a estrutura de cada trilha: módulos, aulas e ordem de exibição.");

        [RelayCommand]
        private Task NavegarQuestoesAsync() => NavegarAsync<QuestionBankViewModel>(
            "Banco de Questões", "Cadastre, edite e envie questões para revisão.");

        [RelayCommand]
        private Task NavegarSimuladosAsync() => NavegarAsync<SimuladosViewModel>(
            "Simulados", "Monte simulados com seleção manual ou sorteio de questões.");

        [RelayCommand]
        private Task NavegarAjusteXpAsync() => NavegarAsync<XpAdjustmentViewModel>(
            "Ajuste de XP", "Conceda ou remova XP de um usuário, com motivo registrado.");

        [RelayCommand]
        private Task NavegarXpMedalhasAsync() => NavegarAsync<GamificationManagementViewModel>(
            "XP e Medalhas", "Configure medalhas e desafios da gamificação.");

        [RelayCommand]
        private Task NavegarAssistenteIaAsync() => NavegarAsync<AiAutomationViewModel>(
            "Assistente de IA", "Automatize relatórios e análises administrativas.");

        [RelayCommand]
        private Task NavegarAuditoriaLogsAsync() => NavegarAsync<AuditLogsViewModel>(
            "Relatórios e Auditoria", "Acompanhe as últimas ações administrativas.");

        [RelayCommand]
        private Task NavegarParametrosAsync() => NavegarAsync<PlatformSettingsViewModel>(
            "Parâmetros Globais", "Ajuste as configurações da plataforma.");

        [RelayCommand]
        private Task NavegarPermissoesAsync() => NavegarAsync<RoleManagementViewModel>(
            "Permissões", "Atribua papéis de acesso aos usuários.");

        /// <summary>Busca global do cabeçalho: abre Usuários já filtrando pelo termo digitado.</summary>
        [RelayCommand]
        private Task PesquisarUsuarioAsync()
        {
            var termo = TermoBusca?.Trim() ?? string.Empty;
            return NavegarAsync<UserManagementViewModel>(
                "Usuários", "Consulte cadastros e bloqueie ou desbloqueie contas.",
                vm => vm.FiltroTermo = termo);
        }

        [RelayCommand]
        private void Sair()
        {
            if (!_dialogService.ShowConfirmation("Sair", "Deseja encerrar a sessão?")) return;
            _authService.Logout();
            LogoutRequested?.Invoke();
        }
    }
}
