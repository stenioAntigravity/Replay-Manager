using System.Windows.Controls;
using System.Windows.Input;
using ReplayManager.App.ViewModels;

namespace ReplayManager.App.Views;

/// <summary>
/// Interaction logic for LibraryView.xaml
/// </summary>
public partial class LibraryView : System.Windows.Controls.UserControl
{
    public LibraryView()
    {
        InitializeComponent();
    }

    private void OnListBoxItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem item && item.DataContext is VideoCardViewModel videoVm)
        {
            if (DataContext is LibraryViewModel vm)
            {
                vm.PlayVideoCommand.Execute(videoVm);
                e.Handled = true;
            }
        }
    }
}

