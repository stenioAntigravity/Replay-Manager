using System.Windows;
using ReplayManager.App.ViewModels;

namespace ReplayManager.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (s, e) =>
        {
            if (viewModel.Library.FilteredVideos.Count == 0)
            {
                await viewModel.Library.LoadVideosAsync();
            }
        };
    }
}
