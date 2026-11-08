namespace ReplayManager.Application.Models.FileSystem;

/// <summary>
/// Resultado da operação de reconciliação de uma pasta monitorada.
/// </summary>
public class ReconciliationResult
{
    public Guid MonitoredFolderId { get; set; }
    public string FolderPath { get; set; } = string.Empty;
    public int TotalScannedFiles { get; set; }
    public int NewFilesIngested { get; set; }
    public int ExistingFilesPreserved { get; set; }
    public int MissingFilesMarked { get; set; }
    public List<string> Errors { get; set; } = new();

    public bool Success => Errors.Count == 0;
}

