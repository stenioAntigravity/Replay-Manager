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
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecionar Miniatura de Imagem",
            Filter = "Imagens (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Todos os arquivos (*.*)|*.*",
            Multiselect = false
        };

        return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
    }

    public void ShowInformation(string message, string title = "Informação")
    {
        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowWarning(string message, string title = "Atenção")
    {
        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public bool ShowConfirmation(string message, string title = "Confirmação")
    {
        var result = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public string? ShowFolderBrowserDialog(string description = "Selecione a pasta para monitoramento")
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        var result = dialog.ShowDialog();
        return result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath)
            ? dialog.SelectedPath
            : null;
    }

    public string? ShowOpenJsonFileDialog(string title = "Selecionar arquivo JSON")
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = "Arquivos JSON (*.json)|*.json|Todos os arquivos (*.*)|*.*",
            Multiselect = false
        };

        return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
    }
}

