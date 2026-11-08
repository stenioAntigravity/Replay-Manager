using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Application.Contracts.Upload;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.Application.Services.Upload;

public class UploadQueueService : IUploadQueueService
{
    private readonly IUploadQueueRepository _queueRepo;
    private readonly IVideoRecordRepository _videoRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadQueueService>? _logger;

    public event EventHandler<UploadProgressUpdatedEventArgs>? ProgressUpdated;
    public event EventHandler<QueueItemStatusChangedEventArgs>? StatusChanged;

    public long BandwidthLimitBytesPerSecond { get; set; } = 0;

    public UploadQueueService(
        IUploadQueueRepository queueRepo,
        IVideoRecordRepository videoRepo,
        IUnitOfWork unitOfWork,
        ILogger<UploadQueueService>? logger = null)
    {
        _queueRepo = queueRepo;
        _videoRepo = videoRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UploadQueueItem>> GetQueueItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _queueRepo.GetAllOrderedAsync(cancellationToken);
    }

    public async Task<UploadQueueItem> EnqueueVideoAsync(Guid videoRecordId, int? priority = null, CancellationToken cancellationToken = default)
    {
        var existing = await _queueRepo.GetByVideoRecordIdAsync(videoRecordId, cancellationToken);
        if (existing != null)
        {
            if (existing.Status == UploadStatus.Failed || existing.Status == UploadStatus.Interrupted || existing.Status == UploadStatus.Paused)
            {
                existing.Status = UploadStatus.Queued;
                existing.LastError = null;
                existing.UpdatedAtUtc = DateTime.UtcNow;
                await _queueRepo.UpdateAsync(existing, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                NotifyStatusChanged(new QueueItemStatusChangedEventArgs
                {
                    QueueItemId = existing.Id,
                    NewStatus = UploadStatus.Queued
                });
            }
            return existing;
        }

        var video = await _videoRepo.GetByIdAsync(videoRecordId, cancellationToken);
        if (video == null)
        {
            throw new InvalidOperationException($"Vídeo com ID '{videoRecordId}' não encontrado no catálogo.");
        }

        var nextPriority = priority ?? (await _queueRepo.GetMaxPriorityAsync(cancellationToken) + 1);

        var queueItem = new UploadQueueItem
        {
            VideoRecordId = videoRecordId,
            Priority = nextPriority,
            Status = UploadStatus.Queued,
            TotalBytes = video.FileSizeBytes,
            BytesUploaded = 0,
            QueuedAtUtc = DateTime.UtcNow
        };

        video.UploadStatus = UploadStatus.Queued;
        await _queueRepo.AddAsync(queueItem, cancellationToken);
        await _videoRepo.UpdateAsync(video, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Vídeo '{FileName}' enfileirado com sucesso (Prioridade: {Priority}).", video.FileName, queueItem.Priority);

        NotifyStatusChanged(new QueueItemStatusChangedEventArgs
        {
            QueueItemId = queueItem.Id,
            NewStatus = UploadStatus.Queued
        });

        return queueItem;
    }

    public async Task<bool> PauseItemAsync(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        var item = await _queueRepo.GetByIdAsync(queueItemId, cancellationToken);
        if (item == null || item.Status == UploadStatus.Completed) return false;

        item.Status = UploadStatus.Paused;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _queueRepo.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        NotifyStatusChanged(new QueueItemStatusChangedEventArgs
        {
            QueueItemId = item.Id,
            NewStatus = UploadStatus.Paused
        });

        return true;
    }

    public async Task<bool> ResumeItemAsync(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        var item = await _queueRepo.GetByIdAsync(queueItemId, cancellationToken);
        if (item == null || (item.Status != UploadStatus.Paused && item.Status != UploadStatus.Interrupted && item.Status != UploadStatus.Failed))
            return false;

        item.Status = UploadStatus.Queued;
        item.LastError = null;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _queueRepo.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        NotifyStatusChanged(new QueueItemStatusChangedEventArgs
        {
            QueueItemId = item.Id,
            NewStatus = UploadStatus.Queued
        });

        return true;
    }

    public async Task<bool> CancelItemAsync(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        var item = await _queueRepo.GetByIdAsync(queueItemId, cancellationToken);
        if (item == null) return false;

        if (item.VideoRecord != null)
        {
            item.VideoRecord.UploadStatus = UploadStatus.None;
            await _videoRepo.UpdateAsync(item.VideoRecord, cancellationToken);
        }

        await _queueRepo.RemoveAsync(queueItemId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Item da fila '{Id}' cancelado e removido.", queueItemId);
        return true;
    }

    public async Task<bool> RetryItemAsync(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        return await ResumeItemAsync(queueItemId, cancellationToken);
    }

    public async Task<bool> ChangePriorityAsync(Guid queueItemId, int newPriority, CancellationToken cancellationToken = default)
    {
        var item = await _queueRepo.GetByIdAsync(queueItemId, cancellationToken);
        if (item == null) return false;

        item.Priority = newPriority;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _queueRepo.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> MoveUpAsync(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        var items = (await _queueRepo.GetAllOrderedAsync(cancellationToken)).ToList();
        var index = items.FindIndex(i => i.Id == queueItemId);
        if (index <= 0) return false;

        var current = items[index];
        var above = items[index - 1];

        // Troca de prioridades
        var tempPriority = current.Priority;
        current.Priority = above.Priority;
        above.Priority = tempPriority;

        if (current.Priority == above.Priority)
        {
            current.Priority--;
        }

        await _queueRepo.UpdateAsync(current, cancellationToken);
        await _queueRepo.UpdateAsync(above, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> MoveDownAsync(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        var items = (await _queueRepo.GetAllOrderedAsync(cancellationToken)).ToList();
        var index = items.FindIndex(i => i.Id == queueItemId);
        if (index < 0 || index >= items.Count - 1) return false;

        var current = items[index];
        var below = items[index + 1];

        var tempPriority = current.Priority;
        current.Priority = below.Priority;
        below.Priority = tempPriority;

        if (current.Priority == below.Priority)
        {
            current.Priority++;
        }

        await _queueRepo.UpdateAsync(current, cancellationToken);
        await _queueRepo.UpdateAsync(below, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public void NotifyProgress(UploadProgressUpdatedEventArgs e)
    {
        ProgressUpdated?.Invoke(this, e);
    }

    public void NotifyStatusChanged(QueueItemStatusChangedEventArgs e)
    {
        StatusChanged?.Invoke(this, e);
    }
}
