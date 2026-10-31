using ReplayManager.Application.Models.FileSystem;

namespace ReplayManager.Application.Contracts.FileSystem;

/// <summary>
/// Contrato do worker de reconciliação de diretórios físicos com o banco de dados.
/// Compensa perdas de eventos do FileSystemWatcher por overflow de buffer do SO
/// ou arquivos inseridos/removidos com a aplicação fechada.
/// </summary>
public interface IFolderReconciliationWorker
{
    /// <summary>
    /// Executa a reconciliação completa para a pasta monitorada especificada.
    /// </summary>
    Task<ReconciliationResult> ReconcileFolderAsync(Guid monitoredFolderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa a reconciliação em lote para todas as pastas ativas no sistema.
    /// </summary>
    Task<IReadOnlyList<ReconciliationResult>> ReconcileAllActiveFoldersAsync(CancellationToken cancellationToken = default);
}

