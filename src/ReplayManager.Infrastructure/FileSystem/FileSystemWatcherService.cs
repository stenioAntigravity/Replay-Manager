using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.FileSystem;
using ReplayManager.Application.Models.FileSystem;
using ReplayManager.Domain.Entities;

namespace ReplayManager.Infrastructure.FileSystem;

/// <summary>
/// Serviço de monitoramento do sistema de arquivos com suporte a múltiplas pastas,
/// debounce inteligente de eventos e tratamento de renomeações/deleções.
/// </summary>
public class FileSystemWatcherService : IFileWatcherService
{
    private static readonly HashSet<string> DefaultAllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".mkv",
        ".avi"
    };

    private const int DebounceDelayMs = 1200;

    private readonly ILogger<FileSystemWatcherService>? _logger;
    private readonly ConcurrentDictionary<Guid, FileSystemWatcher> _watchers = new();
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _folderExtensions = new();
    private readonly ConcurrentDictionary<string, Timer> _debounceTimers = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public event EventHandler<FileDetectedEventArgs>? FileDiscovered;
    public event EventHandler<FileDeletedEventArgs>? FileDeleted;

    public FileSystemWatcherService(ILogger<FileSystemWatcherService>? logger = null)
    {
        _logger = logger;
    }

    public void StartMonitoring(MonitoredFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(folder.Path))
        {
            _logger?.LogWarning("Tentativa de monitorar pasta com caminho vazio (ID: {FolderId})", folder.Id);
            return;
        }

        if (!Directory.Exists(folder.Path))
        {
            _logger?.LogWarning("Diretório físico não existe: '{Path}' (Pasta: '{Name}', ID: {FolderId})",
                folder.Path, folder.Name, folder.Id);
            return;
        }

        StopMonitoring(folder.Id);

        try
        {
            var allowedExts = ResolveAllowedExtensions(folder);
            _folderExtensions[folder.Id] = allowedExts;

            var includeSubdirs = folder.FilterRule?.IncludeSubdirectories ?? false;

            var watcher = new FileSystemWatcher(folder.Path)
            {
                IncludeSubdirectories = includeSubdirs,
                NotifyFilter = NotifyFilters.FileName
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Size
                             | NotifyFilters.CreationTime,
                EnableRaisingEvents = false
            };

            var folderId = folder.Id;

            watcher.Created += (_, e) => OnFileCreatedOrChanged(folderId, e.FullPath, FileDetectionChangeType.Created);
            watcher.Changed += (_, e) => OnFileCreatedOrChanged(folderId, e.FullPath, FileDetectionChangeType.Changed);
            watcher.Renamed += (_, e) => OnFileRenamed(folderId, e.FullPath, e.OldFullPath);
            watcher.Deleted += (_, e) => OnFileDeleted(folderId, e.FullPath);
            watcher.Error += (_, e) => OnWatcherError(folderId, e);

            watcher.EnableRaisingEvents = true;
            _watchers[folderId] = watcher;

            _logger?.LogInformation("Monitoramento iniciado para a pasta '{Path}' (ID: {FolderId}, Subdiretórios: {Subdirs})",
                folder.Path, folderId, includeSubdirs);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Falha ao iniciar FileSystemWatcher para a pasta '{Path}' (ID: {FolderId})",
                folder.Path, folder.Id);
            throw;
        }
    }

    public void StopMonitoring(Guid monitoredFolderId)
    {
        ThrowIfDisposed();

        if (_watchers.TryRemove(monitoredFolderId, out var watcher))
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                _logger?.LogInformation("Monitoramento encerrado para a pasta ID: {FolderId}", monitoredFolderId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Erro ao descartar FileSystemWatcher da pasta ID: {FolderId}", monitoredFolderId);
            }
        }

        _folderExtensions.TryRemove(monitoredFolderId, out _);
    }

    public void StopAll()
    {
        foreach (var folderId in _watchers.Keys.ToList())
        {
            StopMonitoring(folderId);
        }

        ClearAllTimers();
    }

    public bool IsMonitoring(Guid monitoredFolderId)
    {
        return _watchers.ContainsKey(monitoredFolderId);
    }

    public IReadOnlyCollection<Guid> GetActiveMonitoredFolderIds()
    {
        return _watchers.Keys.ToList().AsReadOnly();
    }

    private void OnFileCreatedOrChanged(Guid folderId, string fullPath, FileDetectionChangeType changeType)
    {
        if (!IsAllowedVideoExtension(folderId, fullPath))
        {
            return;
        }

        DebounceEvent(fullPath, () =>
        {
            if (File.Exists(fullPath))
            {
                _logger?.LogDebug("Evento de arquivo detectado ({ChangeType}): '{FilePath}' (Pasta ID: {FolderId})",
                    changeType, fullPath, folderId);

                FileDiscovered?.Invoke(this, new FileDetectedEventArgs(folderId, fullPath, changeType));
            }
        });
    }

    private void OnFileRenamed(Guid folderId, string newFullPath, string oldFullPath)
    {
        CancelDebounce(oldFullPath);

        if (!IsAllowedVideoExtension(folderId, newFullPath))
        {
            return;
        }

        DebounceEvent(newFullPath, () =>
        {
            if (File.Exists(newFullPath))
            {
                _logger?.LogInformation("Arquivo renomeado: '{OldPath}' -> '{NewPath}' (Pasta ID: {FolderId})",
                    oldFullPath, newFullPath, folderId);

                FileDiscovered?.Invoke(this, new FileDetectedEventArgs(
                    folderId,
                    newFullPath,
                    FileDetectionChangeType.Renamed,
                    oldFullPath));
            }
        });
    }

    private void OnFileDeleted(Guid folderId, string fullPath)
    {
        CancelDebounce(fullPath);

        if (!IsAllowedVideoExtension(folderId, fullPath))
        {
            return;
        }

        _logger?.LogInformation("Arquivo deletado no disco: '{FilePath}' (Pasta ID: {FolderId})", fullPath, folderId);
        FileDeleted?.Invoke(this, new FileDeletedEventArgs(folderId, fullPath));
    }

    private void OnWatcherError(Guid folderId, ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger?.LogError(ex, "Erro no FileSystemWatcher da pasta ID {FolderId}. Possível buffer overflow. Reconciliação recomendada.", folderId);
    }

    private void DebounceEvent(string filePath, Action action)
    {
        _debounceTimers.AddOrUpdate(
            filePath,
            path => new Timer(_ =>
            {
                RemoveDebounceTimer(path);
                action();
            }, null, DebounceDelayMs, Timeout.Infinite),
            (path, existingTimer) =>
            {
                existingTimer.Change(DebounceDelayMs, Timeout.Infinite);
                return existingTimer;
            });
    }

    private void CancelDebounce(string filePath)
    {
        if (_debounceTimers.TryRemove(filePath, out var timer))
        {
            try
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Ignorar se já descartado
            }
        }
    }

    private void RemoveDebounceTimer(string filePath)
    {
        if (_debounceTimers.TryRemove(filePath, out var timer))
        {
            try
            {
                timer.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Ignorar
            }
        }
    }

    private void ClearAllTimers()
    {
        foreach (var key in _debounceTimers.Keys.ToList())
        {
            CancelDebounce(key);
        }
    }

    private bool IsAllowedVideoExtension(Guid folderId, string filePath)
    {
        try
        {
            var ext = Path.GetExtension(filePath);
            if (string.IsNullOrWhiteSpace(ext)) return false;

            if (_folderExtensions.TryGetValue(folderId, out var customExts) && customExts.Count > 0)
            {
                return customExts.Contains(ext);
            }

            return DefaultAllowedExtensions.Contains(ext);
        }
        catch
        {
            return false;
        }
    }

    private static HashSet<string> ResolveAllowedExtensions(MonitoredFolder folder)
    {
        if (folder.FilterRule != null && !string.IsNullOrWhiteSpace(folder.FilterRule.AllowedExtensions))
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parts = folder.FilterRule.AllowedExtensions.Split(
                new[] { ',', ';', ' ' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                var clean = part.StartsWith('.') ? part : $".{part}";
                set.Add(clean);
            }

            if (set.Count > 0)
            {
                return set;
            }
        }

        return new HashSet<string>(DefaultAllowedExtensions, StringComparer.OrdinalIgnoreCase);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var watcher in _watchers.Values)
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogTrace(ex, "Erro ignorado ao descartar watcher durante dispose.");
            }
        }

        _watchers.Clear();
        _folderExtensions.Clear();
        ClearAllTimers();

        GC.SuppressFinalize(this);
    }
}
