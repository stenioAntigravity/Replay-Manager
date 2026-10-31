using System.Windows;
using Microsoft.Win32;

namespace ReplayManager.App.Services;

/// <summary>
/// Implementação concreta do serviço de diálogo utilizando componentes nativos do Windows / WPF.
/// </summary>
public class WpfDialogService : IDialogService
{
    public string? ShowOpenImageFileDialog()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Selecionar Miniatura de Imagem",
            Filter = "Imagens (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Todos os arquivos (*.*)|*.*",
            Multiselect = false
        };

        return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
    }

    public void ShowInformation(string message, string title = "Informação")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowWarning(string message, string title = "Atenção")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public bool ShowConfirmation(string message, string title = "Confirmação")
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }
}

