using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.Services;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.UI.ViewModels;
using Ducks.Desktop.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;  
using Ducks.Desktop.UI.Services;

namespace Ducks.Desktop.UI
{
    public partial class App : System.Windows.Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    //services.AddHttpClient("DucksApi", client =>
                    //{
                        //client.BaseAddress = new Uri("https://ducks-api-sistema-hfbndmc0cudwgcg6.brazilsouth-01.azurewebsites.net/");
                        //client.DefaultRequestHeaders.Add("Accept", "application/json");
                    //});

                    services.AddHttpClient("DucksApi", client =>
                    {
                        client.BaseAddress = new System.Uri("http://localhost:5058/");
                    });                    

                    // 1. Serviços de API
                    services.AddSingleton<IAuthApiService, AuthApiService>();
                    services.AddSingleton<IMetricsApiService, MetricsApiService>();
                    services.AddSingleton<IUserAdminApiService, UserAdminApiService>();
                    services.AddSingleton<IGamificationApiService, GamificationApiService>();
                    services.AddSingleton<IModerationApiService, ModerationApiService>();
                    services.AddSingleton<IAiAutomationApiService, AiAutomationApiService>();

                    // 2. ViewModels de todos os Módulos (Incluindo Parâmetros e Permissões)
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<MetricsDashboardViewModel>();
                    services.AddTransient<UserManagementViewModel>();
                    services.AddTransient<GamificationManagementViewModel>();
                    services.AddTransient<ModerationViewModel>();
                    services.AddTransient<CoursesAndTrailsViewModel>();
                    services.AddTransient<AiAutomationViewModel>();
                    services.AddTransient<AuditLogsViewModel>();
                    services.AddTransient<PlatformSettingsViewModel>();
                    services.AddTransient<RoleManagementViewModel>();

                    // 3. Views (UserControls / Windows) de todos os Módulos
                    services.AddTransient<MainWindow>();
                    services.AddTransient<LoginView>();
                    services.AddTransient<MetricsDashboardView>();
                    services.AddTransient<UserManagementView>();
                    services.AddTransient<GamificationManagementView>();
                    services.AddTransient<ModerationView>();
                    services.AddTransient<CoursesAndTrailsView>();
                    services.AddTransient<AiAutomationView>();
                    services.AddTransient<AuditLogsView>();
                    services.AddTransient<PlatformSettingsView>();
                    services.AddTransient<RoleManagementView>();

                    // Adicione o registro no ConfigureServices:
                    services.AddSingleton<IDialogService, DialogService>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Registrar manipuladores globais de exceções não tratadas na UI
                AppDomain.CurrentDomain.UnhandledException += OnGlobalUnhandledException;
                DispatcherUnhandledException += OnDispatcherUnhandledException;

                await _host.StartAsync();

                var authService = _host.Services.GetRequiredService<IAuthApiService>();

                var loginViewModel = new LoginViewModel(authService, () =>
                {
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                    mainWindow.Show();

                    foreach (Window window in System.Windows.Application.Current.Windows)
                    {
                        if (window is LoginView)
                        {
                            window.Close();
                        }
                    }
                });

                var loginView = new LoginView(loginViewModel);
                loginView.Show();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                TratarExcecaoGlobal(ex);
                Shutdown();
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            TratarExcecaoGlobal(e.Exception);
            e.Handled = true; // Impede que a aplicação feche abruptamente
        }

        private void OnGlobalUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                TratarExcecaoGlobal(ex);
            }
        }

        private void TratarExcecaoGlobal(Exception ex)
        {
            string mensagem = ex switch
            {
                HttpRequestException httpEx when httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized 
                    => "Sua sessão expirou ou você não tem permissão para esta ação. Faça login novamente.",
                HttpRequestException => "Não foi possível se conectar à API da Ducks. Verifique sua conexão ou o status do servidor backend.",
                UnauthorizedAccessException => "Acesso negado: Perfil de administrador necessário.",
                _ => $"Ocorreu uma falha inesperada:\n{ex.InnerException?.Message ?? ex.Message}"
            };

            MessageBox.Show(mensagem, "🦆 Ducks Admin - Alerta do Sistema", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }

            base.OnExit(e);
        }
    }
}