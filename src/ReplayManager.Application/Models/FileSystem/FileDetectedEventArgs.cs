namespace ReplayManager.Application.Models.FileSystem;

/// <summary>
/// Tipo de modificação detectada pelo monitor de arquivos.
/// </summary>
public enum FileDetectionChangeType
{
    Created,
    Changed,
    Renamed
}

/// <summary>
/// Argumentos de evento disparados quando um novo arquivo de vídeo é detectado em uma pasta monitorada.
/// </summary>
public class FileDetectedEventArgs : EventArgs
{
    public Guid MonitoredFolderId { get; }
    public string FilePath { get; }
    public string? OldFilePath { get; }
    public FileDetectionChangeType ChangeType { get; }
    public DateTime DetectedAtUtc { get; }

    public FileDetectedEventArgs(
        Guid monitoredFolderId,
        string filePath,
        FileDetectionChangeType changeType,
        string? oldFilePath = null)
    {
        MonitoredFolderId = monitoredFolderId;
        FilePath = filePath;
        ChangeType = changeType;
        OldFilePath = oldFilePath;
        DetectedAtUtc = DateTime.UtcNow;
    }
}

