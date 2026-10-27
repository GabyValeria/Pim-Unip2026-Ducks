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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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

            // Registro de Janelas
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();

            ServiceProvider = services.BuildServiceProvider();

            // Inicia exibindo a Janela de Login
            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }
    }
}