using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Ducks.Desktop.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _title = "Ducks Admin - Painel Principal";

        [ObservableProperty]
        private object? _viewModelAtual;

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModelAtual = null; // Tela inicial de boas-vindas
        }

        /// <summary>
        /// Método blindado que tenta carregar o módulo. Se ocorrer qualquer falha ou ausência de registro,
        /// a exceção é capturada, um aviso é exibido ao usuário e o sistema permanece aberto e estável.
        /// </summary>
        private void ExecutarNavegacao<TViewModel>() where TViewModel : class
        {
            try
            {
                ViewModelAtual = _serviceProvider.GetRequiredService<TViewModel>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Não foi possível carregar o módulo selecionado.\n\nDetalhes do erro:\n{ex.InnerException?.Message ?? ex.Message}",
                    "Ducks Admin - Aviso de Navegação",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                ViewModelAtual = null; // Retorna para a tela de boas-vindas sem crashar
            }
        }

        [RelayCommand]
        private void NavegarVisaoGeral() => ExecutarNavegacao<MetricsDashboardViewModel>();

        [RelayCommand]
        private void NavegarUsuarios() => ExecutarNavegacao<UserManagementViewModel>();

        [RelayCommand]
        private void NavegarAutores() => ExecutarNavegacao<UserManagementViewModel>();

        [RelayCommand]
        private void NavegarModeracao() => ExecutarNavegacao<ModerationViewModel>();

        [RelayCommand]
        private void NavegarCursosTrilhas() => ExecutarNavegacao<CoursesAndTrailsViewModel>();

        [RelayCommand]
        private void NavegarXpMedalhas() => ExecutarNavegacao<GamificationManagementViewModel>();

        [RelayCommand]
        private void NavegarAssistenteIa() => ExecutarNavegacao<AiAutomationViewModel>();

        [RelayCommand]
        private void NavegarAuditoriaLogs() => ExecutarNavegacao<AuditLogsViewModel>();

        [RelayCommand]
        private void NavegarParametros()
        {
            ViewModelAtual = _serviceProvider.GetRequiredService<PlatformSettingsViewModel>();
        }

        [RelayCommand]
        private void NavegarPermissoes()
        {
            ViewModelAtual = _serviceProvider.GetRequiredService<RoleManagementViewModel>();
        }
    }
}