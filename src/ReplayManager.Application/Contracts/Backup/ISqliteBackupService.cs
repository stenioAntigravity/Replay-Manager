using ReplayManager.Application.Models.Backup;

namespace ReplayManager.Application.Contracts.Backup;

/// <summary>
/// Contrato do motor de clonagem atômica consistente do banco SQLite.
/// Utiliza a SQLite Backup API nativa para criar cópias íntegras sem bloquear o banco em produção.
/// </summary>
public interface ISqliteBackupService
{
    /// <summary>
    /// Cria uma cópia consistente do banco de dados ativo no caminho de destino especificado.
    /// </summary>
    /// <param name="destinationFilePath">Caminho completo do arquivo de banco de backup de destino.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Metadados da cópia atômica criada.</returns>
    Task<BackupMetadata> CreateBackupAsync(string destinationFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa verificação de integridade formal (PRAGMA integrity_check) em um arquivo de banco SQLite.
    /// </summary>
    /// <param name="sqliteFilePath">Caminho completo do arquivo SQLite a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>True se o banco de dados estiver 100% íntegro (retorno 'ok'); caso contrário, False.</returns>
    Task<(bool IsValid, string Message)> VerifyDatabaseIntegrityAsync(string sqliteFilePath, CancellationToken cancellationToken = default);
}

