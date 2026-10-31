using Microsoft.EntityFrameworkCore;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Domain.Entities;
using ReplayManager.Infrastructure.Data;

namespace ReplayManager.Infrastructure.Repositories;

public class MonitoredFolderRepository : IMonitoredFolderRepository
{
    private readonly ReplayManagerDbContext _context;

    public MonitoredFolderRepository(ReplayManagerDbContext context)
    {
        _context = context;
    }

    public async Task<MonitoredFolder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MonitoredFolders
            .Include(f => f.FilterRule)
            .Include(f => f.DescriptionTemplates)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<MonitoredFolder?> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = Path.GetFullPath(path).TrimEnd('\\', '/');
        return await _context.MonitoredFolders
            .Include(f => f.FilterRule)
            .Include(f => f.DescriptionTemplates)
            .FirstOrDefaultAsync(f => f.Path.ToLower() == normalizedPath.ToLower(), cancellationToken);
    }

    public async Task<IReadOnlyList<MonitoredFolder>> GetAllAsync(bool onlyActive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.MonitoredFolders
            .AsNoTracking()
            .Include(f => f.FilterRule)
            .Include(f => f.DescriptionTemplates)
            .AsQueryable();

        if (onlyActive)
        {
            query = query.Where(f => f.IsActive);
        }

        return await query.OrderBy(f => f.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MonitoredFolder folder, CancellationToken cancellationToken = default)
    {
        folder.Path = Path.GetFullPath(folder.Path).TrimEnd('\\', '/');
        await _context.MonitoredFolders.AddAsync(folder, cancellationToken);
    }

    public Task UpdateAsync(MonitoredFolder folder, CancellationToken cancellationToken = default)
    {
        folder.Path = Path.GetFullPath(folder.Path).TrimEnd('\\', '/');
        folder.UpdatedAtUtc = DateTime.UtcNow;

        var tracked = _context.MonitoredFolders.Local.FirstOrDefault(f => f.Id == folder.Id);
        if (tracked != null)
        {
            if (!ReferenceEquals(tracked, folder))
            {
                _context.Entry(tracked).CurrentValues.SetValues(folder);
            }
        }
        else
        {
            _context.MonitoredFolders.Update(folder);
        }

        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.MonitoredFolders.FindAsync(new object[] { id }, cancellationToken);
        if (entity != null)
        {
            _context.MonitoredFolders.Remove(entity);
        }
    }

    public async Task<bool> ExistsPathAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = Path.GetFullPath(path).TrimEnd('\\', '/').ToLower();
        return await _context.MonitoredFolders
            .AnyAsync(f => f.Path.ToLower() == normalizedPath, cancellationToken);
    }
}

