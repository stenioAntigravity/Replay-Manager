using Microsoft.EntityFrameworkCore.Storage;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Infrastructure.Data;

namespace ReplayManager.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ReplayManagerDbContext _context;
    private IDbContextTransaction? _transaction;

    public IVideoRecordRepository Videos { get; }
    public IMonitoredFolderRepository MonitoredFolders { get; }
    public IUploadQueueRepository UploadQueue { get; }

    public UnitOfWork(
        ReplayManagerDbContext context,
        IVideoRecordRepository videos,
        IMonitoredFolderRepository monitoredFolders,
        IUploadQueueRepository uploadQueue)
    {
        _context = context;
        Videos = videos;
        MonitoredFolders = monitoredFolders;
        UploadQueue = uploadQueue;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            return;
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            return;
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}

