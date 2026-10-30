using Microsoft.EntityFrameworkCore;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using ReplayManager.Infrastructure.Data;

namespace ReplayManager.Infrastructure.Repositories;

public class UploadQueueRepository : IUploadQueueRepository
{
    private readonly ReplayManagerDbContext _context;

    public UploadQueueRepository(ReplayManagerDbContext context)
    {
        _context = context;
    }

    public async Task<UploadQueueItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UploadQueue
            .Include(q => q.VideoRecord)
                .ThenInclude(v => v!.MonitoredFolder)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<UploadQueueItem?> GetByVideoRecordIdAsync(Guid videoRecordId, CancellationToken cancellationToken = default)
    {
        return await _context.UploadQueue
            .Include(q => q.VideoRecord)
            .FirstOrDefaultAsync(q => q.VideoRecordId == videoRecordId, cancellationToken);
    }

    public async Task<UploadQueueItem?> GetNextPendingItemAsync(CancellationToken cancellationToken = default)
    {
        // Prioriza itens em 'Uploading' ou 'Preparing' (em caso de retomada) ou 'Queued'
        // Ordenado por menor prioridade numérica e data de enfileiramento mais antiga
        return await _context.UploadQueue
            .Include(q => q.VideoRecord)
                .ThenInclude(v => v!.MonitoredFolder)
            .Where(q => q.Status == UploadStatus.Queued ||
                        q.Status == UploadStatus.Interrupted ||
                        q.Status == UploadStatus.Preparing ||
                        q.Status == UploadStatus.Uploading)
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.QueuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UploadQueueItem>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UploadQueue
            .AsNoTracking()
            .Include(q => q.VideoRecord)
                .ThenInclude(v => v!.MonitoredFolder)
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.QueuedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UploadQueueItem item, CancellationToken cancellationToken = default)
    {
        await _context.UploadQueue.AddAsync(item, cancellationToken);
    }

    public Task UpdateAsync(UploadQueueItem item, CancellationToken cancellationToken = default)
    {
        item.UpdatedAtUtc = DateTime.UtcNow;
        _context.UploadQueue.Update(item);
        return Task.CompletedTask;
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _context.UploadQueue.FindAsync(new object[] { id }, cancellationToken);
        if (item != null)
        {
            _context.UploadQueue.Remove(item);
        }
    }

    public async Task<int> GetMaxPriorityAsync(CancellationToken cancellationToken = default)
    {
        if (!await _context.UploadQueue.AnyAsync(cancellationToken))
        {
            return 0;
        }

        return await _context.UploadQueue.MaxAsync(q => q.Priority, cancellationToken);
    }

    public async Task ResetActiveUploadsToInterruptedAsync(CancellationToken cancellationToken = default)
    {
        var activeItems = await _context.UploadQueue
            .Where(q => q.Status == UploadStatus.Uploading || q.Status == UploadStatus.Preparing)
            .ToListAsync(cancellationToken);

        foreach (var item in activeItems)
        {
            item.Status = UploadStatus.Interrupted;
            item.LastError = "Envio interrompido por reinicialização ou encerramento da aplicação.";
            item.UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
