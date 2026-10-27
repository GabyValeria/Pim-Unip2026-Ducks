using Ducks.Admin.Desktop.Services;
using Ducks.Admin.Desktop.ViewModels;
using Ducks.Admin.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace Ducks.Admin.Desktop
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        public App()
        {
            // Captura qualquer erro invisível ou falha de renderização do WPF
            this.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show($"Erro de Interface (WPF): {e.Exception.Message}\n\nDetalhes: {e.Exception.InnerException?.Message}", 
                    "Ducks Admin - Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
                Shutdown();
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var services = new ServiceCollection();

                // Registro de Serviços HTTP e API
                services.AddHttpClient<IApiClient, ApiClient>();
                services.AddHttpClient<IAiService, AiService>();
                services.AddSingleton<ISignalRService, SignalRService>();

                // Registro de ViewModels
                services.AddTransient<LoginViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<UsersViewModel>();
                services.AddTransient<AiAssistantViewModel>();
                services.AddTransient<DashboardViewModel>();

                // Registro de Janelas (Views)
                services.AddTransient<LoginWindow>();
                services.AddTransient<MainWindow>();

                ServiceProvider = services.BuildServiceProvider();

                // Instancia e exibe a Janela de Login de forma explícita
                var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
                this.MainWindow = loginWindow;
                loginWindow.Show();
                loginWindow.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar os serviços: {ex.Message}", 
                    "Ducks Admin - Erro de Inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}