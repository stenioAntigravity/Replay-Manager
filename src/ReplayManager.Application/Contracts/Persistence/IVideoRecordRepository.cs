using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.Application.Contracts.Persistence;

/// <summary>
/// Contrato de persistência para registros de vídeos na biblioteca.
/// </summary>
public interface IVideoRecordRepository
{
    Task<VideoRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VideoRecord?> GetByFolderAndHashAsync(Guid folderId, string sha256Hash, CancellationToken cancellationToken = default);
    Task<VideoRecord?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VideoRecord>> GetByFolderIdAsync(Guid folderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VideoRecord>> GetAllAsync(
        string? searchTerm = null,
        LocalFileStatus? localStatus = null,
        UploadStatus? uploadStatus = null,
        FilterEvaluationStatus? filterStatus = null,
        Guid? folderId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(VideoRecord videoRecord, CancellationToken cancellationToken = default);
    Task UpdateAsync(VideoRecord videoRecord, CancellationToken cancellationToken = default);
    Task<bool> ExistsInFolderAsync(Guid folderId, string sha256Hash, CancellationToken cancellationToken = default);
}

