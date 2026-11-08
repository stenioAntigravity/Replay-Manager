using System.ComponentModel;
using System.Windows;
using ReplayManager.App.Services;
using ReplayManager.App.ViewModels;
using Serilog;

namespace ReplayManager.App;

public partial class MainWindow : Window
{
    private readonly ISystemTrayService _trayService;
    private readonly IDialogService _dialogService;
    private bool _isExplicitExit;

    public MainWindow(
        MainViewModel viewModel,
        ISystemTrayService trayService,
        IDialogService dialogService)
    {
        Log.Information("MainWindow constructor start");
        InitializeComponent();
        Log.Information("MainWindow InitializeComponent completed");

        DataContext = viewModel;
        _trayService = trayService;
        _dialogService = dialogService;

        Loaded += async (s, e) =>
        {
            if (viewModel.Library.FilteredVideos.Count == 0)
            {
                await viewModel.Library.LoadVideosAsync();
            }
        };

        Closing += OnMainWindowClosing;
        Log.Information("MainWindow constructor end");
    }

    private void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_isExplicitExit) return;
        // Minimize to tray instead of exiting
        e.Cancel = true;
        Hide();
        _trayService.ShowNotification(
            "Replay Manager minimizado",
            "O aplicativo continua em execução na bandeja do sistema para uploads e monitoramento.");
    }

    public void ForceClose()
    {
        _isExplicitExit = true;
        Close();
    }
}
