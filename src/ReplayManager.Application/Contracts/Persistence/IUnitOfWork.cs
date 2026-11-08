namespace ReplayManager.Application.Contracts.Persistence;

/// <summary>
/// Contrato para transações atômicas e persistência coordenada de alterações.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IVideoRecordRepository Videos { get; }
    IMonitoredFolderRepository MonitoredFolders { get; }
    IUploadQueueRepository UploadQueue { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

