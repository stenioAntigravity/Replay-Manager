using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace ReplayManager.App.Services;

/// <summary>
/// Implements a system tray (NotifyIcon) service for a WPF application.
/// It creates a tray icon, a context menu with Open and Exit commands,
/// and raises events that the host can subscribe to.
/// </summary>
public class WindowsSystemTrayService : ISystemTrayService
{
    private readonly ILogger<WindowsSystemTrayService>? _logger;
    private NotifyIcon? _notifyIcon;
    private bool _isDisposed;

    public event Action? OpenRequested;
    public event Action? ExitRequested;

    public WindowsSystemTrayService(ILogger<WindowsSystemTrayService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the tray icon and context menu.
    /// </summary>
    public void Initialize()
    {
        if (_notifyIcon != null) return; // already initialized

        var icon = SystemIcons.Application; // replace with custom .ico if desired

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Text = "Replay Manager",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();
        var openItem = new ToolStripMenuItem("Abrir Replay Manager") { Image = SystemIcons.Information.ToBitmap() };
        openItem.Click += (s, e) => OpenRequested?.Invoke();

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Sair do Replay Manager") { Image = SystemIcons.Error.ToBitmap() };
        exitItem.Click += (s, e) => ExitRequested?.Invoke();

        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => OpenRequested?.Invoke();

        _logger?.LogInformation("System tray icon initialized.");
    }

    /// <summary>
    /// Shows a balloon tip notification.
    /// </summary>
    public void ShowNotification(string title, string message, bool isError = false)
    {
        if (_notifyIcon == null)
        {
            _logger?.LogWarning("ShowNotification called before tray initialization.");
            return;
        }

        var tipIcon = isError ? ToolTipIcon.Error : ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(3000, title, message, tipIcon);
        _logger?.LogInformation("Tray notification shown: {Title} - {Message}", title, message);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        GC.SuppressFinalize(this);
    }
}
