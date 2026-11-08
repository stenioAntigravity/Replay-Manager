namespace ReplayManager.App.Services;

/// <summary>
/// Contrato para serviços de diálogo de interface do usuário (abertura de arquivo, mensagens).
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Exibe diálogo para seleção de imagem (PNG, JPG, JPEG).
    /// </summary>
    string? ShowOpenImageFileDialog();

    /// <summary>
    /// Exibe mensagem de informação ao usuário.
    /// </summary>
    void ShowInformation(string message, string title = "Informação");

    /// <summary>
    /// Exibe mensagem de aviso ou erro ao usuário.
    /// </summary>
    void ShowWarning(string message, string title = "Atenção");

    /// <summary>
    /// Solicita confirmação ao usuário.
    /// </summary>
    bool ShowConfirmation(string message, string title = "Confirmação");

    /// <summary>
    /// Exibe diálogo nativo para seleção de pastas do Windows.
    /// </summary>
    string? ShowFolderBrowserDialog(string description = "Selecione a pasta para monitoramento");

    /// <summary>
    /// Exibe diálogo nativo para seleção de arquivos genéricos ou JSON (ex: client_secrets.json).
    /// </summary>
    string? ShowOpenJsonFileDialog(string title = "Selecionar arquivo JSON");
}

