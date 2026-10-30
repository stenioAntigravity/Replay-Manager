using Microsoft.EntityFrameworkCore;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using ReplayManager.Infrastructure.Data;

namespace ReplayManager.Infrastructure.Repositories;

public class VideoRecordRepository : IVideoRecordRepository
{
    private readonly ReplayManagerDbContext _context;

    public VideoRecordRepository(ReplayManagerDbContext context)
    {
        _context = context;
    }

    public async Task<VideoRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Videos
            .Include(v => v.MonitoredFolder)
            .Include(v => v.UploadQueueItem)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<VideoRecord?> GetByFolderAndHashAsync(Guid folderId, string sha256Hash, CancellationToken cancellationToken = default)
    {
        return await _context.Videos
            .Include(v => v.MonitoredFolder)
            .Include(v => v.UploadQueueItem)
            .FirstOrDefaultAsync(v => v.MonitoredFolderId == folderId && v.ContentSha256 == sha256Hash, cancellationToken);
    }

    public async Task<VideoRecord?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await _context.Videos
            .Include(v => v.MonitoredFolder)
            .Include(v => v.UploadQueueItem)
            .FirstOrDefaultAsync(v => v.FilePath.ToLower() == filePath.ToLower(), cancellationToken);
    }

    public async Task<IReadOnlyList<VideoRecord>> GetByFolderIdAsync(Guid folderId, CancellationToken cancellationToken = default)
    {
        return await _context.Videos
            .AsNoTracking()
            .Where(v => v.MonitoredFolderId == folderId)
            .OrderByDescending(v => v.RecordedAtUtc ?? v.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoRecord>> GetAllAsync(
        string? searchTerm = null,
        LocalFileStatus? localStatus = null,
        UploadStatus? uploadStatus = null,
        FilterEvaluationStatus? filterStatus = null,
        Guid? folderId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Videos
            .AsNoTracking()
            .Include(v => v.MonitoredFolder)
            .Include(v => v.UploadQueueItem)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(v => v.FileName.ToLower().Contains(term) ||
                                     (v.CustomTitle != null && v.CustomTitle.ToLower().Contains(term)));
        }

        if (localStatus.HasValue)
        {
            query = query.Where(v => v.LocalStatus == localStatus.Value);
        }

        if (uploadStatus.HasValue)
        {
            query = query.Where(v => v.UploadStatus == uploadStatus.Value);
        }

        if (filterStatus.HasValue)
        {
            query = query.Where(v => v.FilterStatus == filterStatus.Value);
        }

        if (folderId.HasValue)
        {
            query = query.Where(v => v.MonitoredFolderId == folderId.Value);
        }

        return await query
            .OrderByDescending(v => v.RecordedAtUtc ?? v.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(VideoRecord videoRecord, CancellationToken cancellationToken = default)
    {
        await _context.Videos.AddAsync(videoRecord, cancellationToken);
    }

    public Task UpdateAsync(VideoRecord videoRecord, CancellationToken cancellationToken = default)
    {
        videoRecord.UpdatedAtUtc = DateTime.UtcNow;
        _context.Videos.Update(videoRecord);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsInFolderAsync(Guid folderId, string sha256Hash, CancellationToken cancellationToken = default)
    {
        return await _context.Videos
            .AnyAsync(v => v.MonitoredFolderId == folderId && v.ContentSha256 == sha256Hash, cancellationToken);
    }
}
