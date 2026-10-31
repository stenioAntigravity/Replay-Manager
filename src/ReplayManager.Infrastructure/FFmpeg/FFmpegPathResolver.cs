using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.Media;

namespace ReplayManager.Infrastructure.FFmpeg;

/// <summary>
/// Resolve os caminhos dos binários FFmpeg e FFprobe no sistema operacional.
/// Procura em ordem: configuração customizada, pasta da aplicação, PATH do sistema.
/// </summary>
public class FFmpegPathResolver : IFFmpegPathResolver
{
    private const string FFmpegExe = "ffmpeg.exe";
    private const string FFprobeExe = "ffprobe.exe";

    private readonly ILogger<FFmpegPathResolver>? _logger;
    private readonly string? _customPath;

    // Cache dos caminhos resolvidos
    private string? _cachedFFmpegPath;
    private string? _cachedFFprobePath;
    private bool _resolved;

    public FFmpegPathResolver(ILogger<FFmpegPathResolver>? logger = null, string? customPath = null)
    {
        _logger = logger;
        _customPath = customPath;
    }

    public string? ResolveFFmpegPath()
    {
        EnsureResolved();
        return _cachedFFmpegPath;
    }

    public string? ResolveFFprobePath()
    {
        EnsureResolved();
        return _cachedFFprobePath;
    }

    public bool IsFFmpegAvailable()
    {
        EnsureResolved();
        return _cachedFFmpegPath != null && _cachedFFprobePath != null;
    }

    private void EnsureResolved()
    {
        if (_resolved) return;

        _cachedFFmpegPath = FindExecutable(FFmpegExe);
        _cachedFFprobePath = FindExecutable(FFprobeExe);
        _resolved = true;

        if (_cachedFFmpegPath != null)
            _logger?.LogInformation("FFmpeg encontrado em: '{Path}'", _cachedFFmpegPath);
        else
            _logger?.LogWarning("FFmpeg ({Exe}) não encontrado no sistema.", FFmpegExe);

        if (_cachedFFprobePath != null)
            _logger?.LogInformation("FFprobe encontrado em: '{Path}'", _cachedFFprobePath);
        else
            _logger?.LogWarning("FFprobe ({Exe}) não encontrado no sistema.", FFprobeExe);
    }

    private string? FindExecutable(string exeName)
    {
        // 1. Caminho customizado explícito
        if (!string.IsNullOrWhiteSpace(_customPath))
        {
            var customFile = Path.Combine(_customPath, exeName);
            if (File.Exists(customFile))
            {
                _logger?.LogDebug("Binário '{Exe}' encontrado na pasta customizada: '{Path}'", exeName, customFile);
                return customFile;
            }
        }

        // 2. Pasta da aplicação (bin/ffmpeg/)
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidatePaths = new[]
        {
            Path.Combine(appDir, "ffmpeg", exeName),
            Path.Combine(appDir, "bin", "ffmpeg", exeName),
            Path.Combine(appDir, exeName),
        };

        foreach (var candidate in candidatePaths)
        {
            if (File.Exists(candidate))
            {
                _logger?.LogDebug("Binário '{Exe}' encontrado na pasta da aplicação: '{Path}'", exeName, candidate);
                return candidate;
            }
        }

        // 3. %LOCALAPPDATA%/ReplayManager/ffmpeg/
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var replayManagerFFmpeg = Path.Combine(localAppData, "ReplayManager", "ffmpeg", exeName);
        if (File.Exists(replayManagerFFmpeg))
        {
            _logger?.LogDebug("Binário '{Exe}' encontrado em AppData local: '{Path}'", exeName, replayManagerFFmpeg);
            return replayManagerFFmpeg;
        }

        // 4. PATH do sistema
        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathDirs = pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var dir in pathDirs)
        {
            try
            {
                var fullPath = Path.Combine(dir, exeName);
                if (File.Exists(fullPath))
                {
                    _logger?.LogDebug("Binário '{Exe}' encontrado no PATH do sistema: '{Path}'", exeName, fullPath);
                    return fullPath;
                }
            }
            catch
            {
                // Diretórios inválidos no PATH são ignorados silenciosamente
            }
        }

        return null;
    }
}

