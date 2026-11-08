namespace ReplayManager.Application.Models.FileSystem;

/// <summary>
/// Argumentos de evento disparados quando um arquivo monitorado é removido do disco.
/// </summary>
public class FileDeletedEventArgs : EventArgs
{
    public Guid MonitoredFolderId { get; }
    public string FilePath { get; }
    public DateTime DeletedAtUtc { get; }

    public FileDeletedEventArgs(Guid monitoredFolderId, string filePath)
    {
        MonitoredFolderId = monitoredFolderId;
        FilePath = filePath;
        DeletedAtUtc = DateTime.UtcNow;
    }
}

