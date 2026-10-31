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

    public MainViewModel(LibraryViewModel library)
    {
        _library = library;
    }

    [RelayCommand]
    public void NavigateTo(NavigationTab tab)
    {
        CurrentTab = tab;
    }
}

