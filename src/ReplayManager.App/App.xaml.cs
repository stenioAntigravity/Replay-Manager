using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReplayManager.App.Services;
using ReplayManager.App.ViewModels;
using ReplayManager.Infrastructure;
using ReplayManager.Infrastructure.Data;

namespace ReplayManager.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDataDir = Path.Combine(localAppData, "ReplayManager");
        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }

        var dbPath = Path.Combine(appDataDir, "replaymanager.db");

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Infraestrutura de Banco de dados, Sistema de arquivos e YouTube
                services.AddInfrastructurePersistence(dbPath);
                services.AddInfrastructureFileSystem();
                services.AddInfrastructureFFmpeg();
                services.AddInfrastructureYouTube();

                // Serviços da UI
                services.AddSingleton<IMediaPlayerLauncher, MediaPlayerLauncher>();
                services.AddSingleton<IDialogService, WpfDialogService>();

                // ViewModels
                services.AddSingleton<LibraryViewModel>();
                services.AddSingleton<UploadQueueViewModel>();
                services.AddSingleton<MainViewModel>();

                // Janelas
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Inicializar banco de dados e aplicar migrações/seeding
        using (var scope = _host.Services.CreateScope())
        {
            var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await dbInitializer.InitializeAsync();
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
