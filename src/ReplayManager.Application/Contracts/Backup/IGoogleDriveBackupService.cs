using ReplayManager.Application.Models.Backup;

namespace ReplayManager.Application.Contracts.Backup;

/// <summary>
/// Contrato do serviço de backup e retenção de arquivos no Google Drive.
/// </summary>
public interface IGoogleDriveBackupService
{
    /// <summary>
    /// Verifica se o usuário autenticou credenciais válidas com permissão de Google Drive.
    /// </summary>
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia um arquivo de backup local para a pasta do aplicativo no Google Drive.
    /// Valida previamente a integridade SQLite do arquivo antes de iniciar a transmissão.
    /// </summary>
    /// <param name="backupFilePath">Caminho completo do arquivo de backup no disco local.</param>
    /// <param name="folderName">Nome da pasta no Google Drive (padrão: "ReplayManager_Backups").</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Metadados atualizados com o DriveFileId gerado.</returns>
    Task<BackupMetadata> UploadBackupAsync(string backupFilePath, string folderName = "ReplayManager_Backups", CancellationToken cancellationToken = default);

    /// <summary>
    /// Aplica a política de retenção no Google Drive, mantendo apenas os N backups mais recentes
    /// e excluindo da lixeira/removendo versões antigas para economizar cota.
    /// </summary>
    /// <param name="keepCount">Quantidade de backups mais recentes a manter (ex: 5 ou 10).</param>
    /// <param name="folderName">Nome da pasta no Google Drive onde estão os backups.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de backups antigos removidos.</returns>
    Task<int> EnforceRetentionAsync(int keepCount = 5, string folderName = "ReplayManager_Backups", CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista os backups atualmente armazenados no Google Drive na pasta de backups do aplicativo.
    /// </summary>
    /// <param name="folderName">Nome da pasta no Google Drive.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de metadados dos backups remotos encontrados.</returns>
    Task<IReadOnlyList<BackupMetadata>> ListRemoteBackupsAsync(string folderName = "ReplayManager_Backups", CancellationToken cancellationToken = default);
}

