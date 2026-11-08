namespace ReplayManager.Application.Models.Backup;

/// <summary>
/// Informações e metadados sobre um backup de banco de dados gerado ou sincronizado.
/// </summary>
public record BackupMetadata
{
    /// <summary>
    /// Nome do arquivo de backup (ex: replaymanager_backup_20261031_143000.db).
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Caminho completo do arquivo local (se presente no disco local).
    /// </summary>
    public string? LocalFilePath { get; init; }

    /// <summary>
    /// Tamanho do arquivo em bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Data e hora UTC da criação do backup.
    /// </summary>
    public DateTime CreatedUtc { get; init; }

    /// <summary>
    /// ID do arquivo no Google Drive (se já enviado para a nuvem).
    /// </summary>
    public string? DriveFileId { get; init; }

    /// <summary>
    /// Indica se a verificação de integridade SQLite (PRAGMA integrity_check) foi bem-sucedida.
    /// </summary>
    public bool IsConsistent { get; init; }

    /// <summary>
    /// Mensagem de erro ou detalhes caso a validação ou upload tenha falhado.
    /// </summary>
    public string? Details { get; init; }
}

