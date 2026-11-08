using System.Threading;
using System.Threading.Tasks;

namespace ReplayManager.Application.Contracts.Upload;

/// <summary>
/// Service responsible for processing the persistent upload queue sequentially.
/// It pulls pending items, invokes the YouTube uploader, updates progress and status.
/// </summary>
public interface IUploadQueueProcessor
{
    /// <summary>
    /// Process the upload queue until it is empty or the token is cancelled.
    /// </summary>
    Task ProcessQueueAsync(CancellationToken cancellationToken = default);
}

