using ReplayManager.Domain.Entities;

namespace ReplayManager.Application.Contracts.Persistence;

/// <summary>
/// Contrato de persistência para pastas monitoradas.
/// </summary>
public interface IMonitoredFolderRepository
{
    Task<MonitoredFolder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MonitoredFolder?> GetByPathAsync(string path, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonitoredFolder>> GetAllAsync(bool onlyActive = false, CancellationToken cancellationToken = default);
    Task AddAsync(MonitoredFolder folder, CancellationToken cancellationToken = default);
    Task UpdateAsync(MonitoredFolder folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsPathAsync(string path, CancellationToken cancellationToken = default);
}
