using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ReplayManager.App.Services;

/// <summary>
/// Implementação para inicialização de reprodutor de mídia com detecção de VLC e fallback para player padrão.
/// </summary>
public class MediaPlayerLauncher : IMediaPlayerLauncher
{
    private readonly ILogger<MediaPlayerLauncher>? _logger;
    private string? _cachedVlcPath;
    private bool _vlcResolved;

    public MediaPlayerLauncher(ILogger<MediaPlayerLauncher>? logger = null)
    {
        _logger = logger;
    }

    public bool IsVlcInstalled()
    {
        EnsureVlcResolved();
        return !string.IsNullOrWhiteSpace(_cachedVlcPath);
    }

    public string? GetVlcExecutablePath()
    {
        EnsureVlcResolved();
        return _cachedVlcPath;
    }

    public bool PlayVideo(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            _logger?.LogWarning("Arquivo de vídeo '{Path}' não existe para reprodução.", filePath);
            return false;
        }

        var vlc = GetVlcExecutablePath();
        try
        {
            if (!string.IsNullOrWhiteSpace(vlc) && File.Exists(vlc))
            {
                _logger?.LogInformation("Reproduzindo via VLC: '{Vlc}' \"{Path}\"", vlc, filePath);
                Process.Start(new ProcessStartInfo
                {
                    FileName = vlc,
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = false
                });
                return true;
            }

            _logger?.LogInformation("VLC não detectado. Usando player padrão do Windows para '{Path}'", filePath);
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao iniciar reprodutor de vídeo para '{Path}'", filePath);
            return false;
        }
    }

    private void EnsureVlcResolved()
    {
        if (_vlcResolved) return;
        _cachedVlcPath = DetectVlcPath();
        _vlcResolved = true;
    }

    private string? DetectVlcPath()
    {
        // 1. Diretórios padrões comuns
        var candidatePaths = new[]
        {
            @"C:\Program Files\VideoLAN\VLC\vlc.exe",
            @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\VideoLAN\VLC\vlc.exe")
        };

        foreach (var candidate in candidatePaths)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        // 2. Registro do Windows
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VideoLAN\VLC") ??
                            Registry.CurrentUser.OpenSubKey(@"SOFTWARE\VideoLAN\VLC");
            if (key != null)
            {
                var installDir = key.GetValue("InstallDir") as string;
                if (!string.IsNullOrWhiteSpace(installDir))
                {
                    var exePath = Path.Combine(installDir, "vlc.exe");
                    if (File.Exists(exePath))
                    {
                        return exePath;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Exceção ao consultar registro do Windows para o VLC.");
        }

        return null;
    }
}
