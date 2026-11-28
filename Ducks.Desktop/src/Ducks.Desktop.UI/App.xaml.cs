using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.Services;
using Ducks.Desktop.Application.ViewModels;
using Ducks.Desktop.UI.Services;
using Ducks.Desktop.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ducks.Desktop.UI
{
    public partial class App : System.Windows.Application
    {
        private const string ApiUrlEnvironmentVariable = "https://ducks-api-cxcjgnd2b0hnhnas.brazilsouth-01.azurewebsites.net/";
        private const string DefaultApiUrl = "https://ducks-api-cxcjgnd2b0hnhnas.brazilsouth-01.azurewebsites.net/";
        private readonly IHost _host;
        private bool _encerrando;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient(ApiClient.ClientName, client =>
                    {
                        client.BaseAddress = ResolverUrlDaApi();
                        client.Timeout = TimeSpan.FromSeconds(30);
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                    {
                        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                        SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                        {
                            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
                        }
                    });

                    // 1. Infraestrutura HTTP e serviços de API
                    services.AddSingleton<IAuthApiService, AuthApiService>();
                    services.AddSingleton<IApiClient, ApiClient>();
                    services.AddSingleton<IMetricsApiService, MetricsApiService>();
                    services.AddSingleton<IAuditApiService, AuditApiService>();
                    services.AddSingleton<ISettingsApiService, SettingsApiService>();
                    services.AddSingleton<IAiAutomationApiService, AiAutomationApiService>();
                    services.AddSingleton<IUserAdminApiService, UserAdminApiService>();
                    services.AddSingleton<IRoleApiService, RoleApiService>();
                    services.AddSingleton<IGamificationApiService, GamificationApiService>();
                    services.AddSingleton<IContentApiService, ContentApiService>();
                    services.AddSingleton<IContentManagementApiService, ContentManagementApiService>();
                    services.AddSingleton<IAssessmentApiService, AssessmentApiService>();
                    services.AddSingleton<IXpApiService, XpApiService>();
                    services.AddSingleton<IModerationApiService, ModerationApiService>();
                    services.AddSingleton<IDialogService, DialogService>();

                    // 2. ViewModels (resolvidos pela navegação do MainViewModel)
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<MetricsDashboardViewModel>();
                    services.AddTransient<UserManagementViewModel>();
                    services.AddTransient<RoleManagementViewModel>();
                    services.AddTransient<GamificationManagementViewModel>();
                    services.AddTransient<ModerationViewModel>();
                    services.AddTransient<CoursesAndTrailsViewModel>();
                    services.AddTransient<ContentStructureViewModel>();
                    services.AddTransient<QuestionBankViewModel>();
                    services.AddTransient<SimuladosViewModel>();
                    services.AddTransient<XpAdjustmentViewModel>();
                    services.AddTransient<AiAutomationViewModel>();
                    services.AddTransient<AuditLogsViewModel>();
                    services.AddTransient<PlatformSettingsViewModel>();

                    // 3. Janela principal (as demais views são criadas via DataTemplate no MainWindow.xaml)
                    services.AddTransient<MainWindow>();
                })
                .Build();
        }

        private static Uri ResolverUrlDaApi()
        {
            var configurada = Environment.GetEnvironmentVariable(ApiUrlEnvironmentVariable);
            var url = string.IsNullOrWhiteSpace(configurada) ? DefaultApiUrl : configurada.Trim();
            if (!url.EndsWith('/')) url += "/";

            return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : new Uri(DefaultApiUrl);
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnGlobalUnhandledException;
            TaskScheduler.UnobservedTaskException += (_, args) => args.SetObserved();

            try
            {
                await _host.StartAsync();
                MostrarLogin();
            }
            catch (Exception ex)
            {
                TratarExcecaoGlobal(ex);
                Shutdown();
            }
        }

        // ------------------------------------------------------------------ Fluxo de janelas

        private void MostrarLogin()
        {
            var authService = _host.Services.GetRequiredService<IAuthApiService>();

            LoginView? loginView = null;
            var loginViewModel = new LoginViewModel(authService, () =>
            {
                AbrirJanelaPrincipal();
                loginView?.Close();
            });

            loginView = new LoginView(loginViewModel);
            MainWindow = loginView;
            loginView.Show();
        }

        private void AbrirJanelaPrincipal()
        {
            var janela = _host.Services.GetRequiredService<MainWindow>();
            var viewModel = (MainViewModel)janela.DataContext;
            var dialogService = _host.Services.GetRequiredService<IDialogService>();

            viewModel.LogoutRequested += () => Dispatcher.Invoke(() => VoltarParaLogin(janela));
            viewModel.SessionExpiredRequested += () => Dispatcher.Invoke(() =>
            {
                dialogService.ShowMessage("Sessão expirada", "Sua sessão expirou. Faça login novamente para continuar.", true);
                VoltarParaLogin(janela);
            });
            janela.Closed += (_, _) => viewModel.Dispose();

            MainWindow = janela;
            janela.Show();
        }

        private void VoltarParaLogin(Window janelaPrincipal)
        {
            if (_encerrando || !janelaPrincipal.IsLoaded) return;

            // Abre o login antes de fechar o painel para não encerrar a aplicação (ShutdownMode padrão).
            MostrarLogin();
            janelaPrincipal.Close();
        }

        // ------------------------------------------------------------------ Tratamento global de erros

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            TratarExcecaoGlobal(e.Exception);
            e.Handled = true; // mantém o painel aberto
        }

        private void OnGlobalUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                TratarExcecaoGlobal(ex);
        }

        private void TratarExcecaoGlobal(Exception ex)
        {
            var mensagem = ex switch
            {
                ApiException api => api.Message,
                HttpRequestException => "Não foi possível se conectar à API da Ducks. Verifique sua conexão ou o status do servidor.",
                UnauthorizedAccessException => "Acesso negado: perfil de administrador necessário.",
                _ => $"Ocorreu uma falha inesperada:\n{ex.InnerException?.Message ?? ex.Message}"
            };

            MessageBox.Show(mensagem, "Ducks Admin - Alerta do Sistema", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _encerrando = true;
            try
            {
                using (_host)
                {
                    await _host.StopAsync(TimeSpan.FromSeconds(5));
                }
            }
            catch
            {
                // encerramento best-effort
            }

            base.OnExit(e);
        }
    }
}
