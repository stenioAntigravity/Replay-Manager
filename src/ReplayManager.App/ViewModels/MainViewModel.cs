using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ReplayManager.App.ViewModels;

public enum NavigationTab
{
    Library,
    UploadQueue,
    MonitoredFolders,
    Settings
}

/// <summary>
/// ViewModel da janela principal (ShellWindow), orquestrando as abas de navegação.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private NavigationTab _currentTab = NavigationTab.Library;

    [ObservableProperty]
    private LibraryViewModel _library;

    [ObservableProperty]
    private UploadQueueViewModel _uploadQueue;

    [ObservableProperty]
    private MonitoredFoldersViewModel _monitoredFolders;

    [ObservableProperty]
    private SettingsViewModel _settings;

    public MainViewModel(
        LibraryViewModel library, 
        UploadQueueViewModel uploadQueue, 
        MonitoredFoldersViewModel monitoredFolders,
        SettingsViewModel settings)
    {
        _library = library;
        _uploadQueue = uploadQueue;
        _monitoredFolders = monitoredFolders;
        _settings = settings;

        _library.NavigateToMonitoredFoldersRequested += () =>
        {
            NavigateTo(NavigationTab.MonitoredFolders);
        };
    }

    [RelayCommand]
    public void NavigateTo(NavigationTab tab)
    {
        CurrentTab = tab;
        if (tab == NavigationTab.Library)
        {
            _ = Library.LoadVideosAsync();
        }
        else if (tab == NavigationTab.UploadQueue)
        {
            _ = UploadQueue.LoadQueueAsync();
        }
        else if (tab == NavigationTab.MonitoredFolders)
        {
            _ = MonitoredFolders.LoadFoldersAsync();
        }
        else if (tab == NavigationTab.Settings)
        {
            _ = Settings.InitializeAsync();
        }
    }
}
