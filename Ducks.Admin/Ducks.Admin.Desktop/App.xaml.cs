using System.Windows;
using Ducks.Admin.Desktop.Services;
using Ducks.Admin.Desktop.Stores;
using Ducks.Admin.Desktop.ViewModels;
using Ducks.Admin.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;

namespace Ducks.Admin.Desktop;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Stores (Singleton para manter estado global)
                services.AddSingleton<NavigationStore>();
                services.AddSingleton<UserSessionStore>();

                // Services / Refit Client (Apontando para a Ducks.Api local ou em nuvem)
                services.AddRefitClient<IAuthApiService>()
                        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://localhost:5001"));

                // ViewModels
                services.AddTransient<LoginViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<DashboardViewModel>();

                // Views
                services.AddSingleton<MainWindow>(provider => new MainWindow
                {
                    DataContext = provider.GetRequiredService<MainViewModel>()
                });
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        
        // Define o ViewModel inicial como Login
        var navigationStore = _host.Services.GetRequiredService<NavigationStore>();
        navigationStore.CurrentViewModel = _host.Services.GetRequiredService<LoginViewModel>();

        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}