using ReplayManager.Application.Models.FileSystem;
using ReplayManager.Domain.Entities;

namespace ReplayManager.Application.Contracts.FileSystem;

/// <summary>
/// Contrato do serviço de monitoramento ativo do sistema de arquivos via FileSystemWatcher.
/// </summary>
public interface IFileWatcherService : IDisposable
{
    /// <summary>
    /// Disparado quando um arquivo de vídeo candidato é criado, alterado ou renomeado após estabilização inicial.
    /// </summary>
    event EventHandler<FileDetectedEventArgs>? FileDiscovered;

    /// <summary>
    /// Disparado quando um arquivo é deletado na pasta monitorada.
    /// </summary>
    event EventHandler<FileDeletedEventArgs>? FileDeleted;

    /// <summary>
    /// Inicia o monitoramento de uma pasta cadastrada.
    /// </summary>
    void StartMonitoring(MonitoredFolder folder);

    /// <summary>
    /// Interrompe o monitoramento de uma pasta específica.
    /// </summary>
    void StopMonitoring(Guid monitoredFolderId);

    /// <summary>
    /// Interrompe o monitoramento de todas as pastas ativas.
    /// </summary>
    void StopAll();

    /// <summary>
    /// Verifica se uma pasta está sob monitoramento ativo.
    /// </summary>
    bool IsMonitoring(Guid monitoredFolderId);

    /// <summary>
    /// Obtém a lista dos identificadores de pastas sob monitoramento ativo.
    /// </summary>
    IReadOnlyCollection<Guid> GetActiveMonitoredFolderIds();
}

