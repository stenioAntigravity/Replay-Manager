namespace ReplayManager.App.Services;

/// <summary>
/// Ações disponíveis ao fechar a janela principal da aplicação.
/// </summary>
public enum AppCloseAction
{
    Prompt,
    MinimizeToTray,
    CloseWindowKeepBackground,
    Terminate
}

/// <summary>
/// Contrato do serviço de gerenciamento do ícone de bandeja (System Tray) e notificações.
/// </summary>
public interface ISystemTrayService : IDisposable
{
    /// <summary>
    /// Inicializa e exibe o ícone na bandeja do sistema.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Exibe balão de notificação (Toast/BalloonTip) na bandeja do sistema.
    /// </summary>
    void ShowNotification(string title, string message, bool isError = false);

    /// <summary>
    /// Evento disparado quando o usuário solicita abrir/restaurar a janela principal.
    /// </summary>
    event Action? OpenRequested;

    /// <summary>
    /// Evento disparado quando o usuário solicita encerrar o aplicativo pela bandeja.
    /// </summary>
    event Action? ExitRequested;
}

