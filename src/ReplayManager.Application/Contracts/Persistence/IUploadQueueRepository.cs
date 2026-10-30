using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.Application.Contracts.Persistence;

/// <summary>
/// Contrato de persistência para a fila de upload sequencial e resiliente.
/// </summary>
public interface IUploadQueueRepository
{
    Task<UploadQueueItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UploadQueueItem?> GetByVideoRecordIdAsync(Guid videoRecordId, CancellationToken cancellationToken = default);
    Task<UploadQueueItem?> GetNextPendingItemAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UploadQueueItem>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
    Task AddAsync(UploadQueueItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(UploadQueueItem item, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetMaxPriorityAsync(CancellationToken cancellationToken = default);
    Task ResetActiveUploadsToInterruptedAsync(CancellationToken cancellationToken = default);
}
