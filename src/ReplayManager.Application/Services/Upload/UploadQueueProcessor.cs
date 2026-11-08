using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Application.Contracts.Upload;
using ReplayManager.Application.Contracts.YouTube;
using ReplayManager.Application.Models.YouTube;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.Application.Services.Upload;

/// <summary>
/// Implements a sequential, resilient upload queue processor.
/// Pulls pending items, invokes the YouTube uploader with progress tracking and bandwidth throttling,
/// and persists state transitions immediately into SQLite.
/// </summary>
public class UploadQueueProcessor : IUploadQueueProcessor
{
    private readonly IUploadQueueRepository _queueRepo;
    private readonly IYouTubeUploader _uploader;
    private readonly IUploadQueueService _queueService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadQueueProcessor>? _logger;

    public UploadQueueProcessor(
        IUploadQueueRepository queueRepo,
        IYouTubeUploader uploader,
        IUploadQueueService queueService,
        IUnitOfWork unitOfWork,
        ILogger<UploadQueueProcessor>? logger = null)
    {
        _queueRepo = queueRepo;
        _uploader = uploader;
        _queueService = queueService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessQueueAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var item = await _queueRepo.GetNextPendingItemAsync(cancellationToken);
            if (item == null)
            {
                _logger?.LogDebug("Fila de upload vazia ou sem itens pendentes.");
                break;
            }

            // Ignorar se o item estiver pausado
            if (item.Status == UploadStatus.Paused)
            {
                await Task.Delay(1000, cancellationToken);
                continue;
            }

            await ProcessItemAsync(item, cancellationToken);
        }
    }

    private async Task ProcessItemAsync(UploadQueueItem item, CancellationToken cancellationToken)
    {
        // 1. Marcar como Preparing
        item.Status = UploadStatus.Preparing;
        item.StartedAtUtc ??= DateTime.UtcNow;
        item.UpdatedAtUtc = DateTime.UtcNow;
        item.AttemptCount++;
        await _queueRepo.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _queueService.NotifyStatusChanged(new QueueItemStatusChangedEventArgs
        {
            QueueItemId = item.Id,
            NewStatus = UploadStatus.Preparing
        });

        // 2. Preparar progresso e métricas de velocidade
        var sw = Stopwatch.StartNew();
        long lastReportedBytes = 0;
        long lastTicks = sw.ElapsedMilliseconds;

        var progress = new Progress<UploadProgressNotification>(notification =>
        {
            var currentTicks = sw.ElapsedMilliseconds;
            var elapsedSec = (currentTicks - lastTicks) / 1000.0;
            long speed = 0;

            if (elapsedSec >= 0.5)
            {
                var deltaBytes = notification.BytesSent - lastReportedBytes;
                speed = (long)(deltaBytes / elapsedSec);
                lastReportedBytes = notification.BytesSent;
                lastTicks = currentTicks;
            }

            item.BytesUploaded = notification.BytesSent;
            item.TotalBytes = notification.TotalBytes > 0 ? notification.TotalBytes : item.TotalBytes;
            item.UploadSpeedBytesPerSec = speed;
            item.Status = UploadStatus.Uploading;

            _queueService.NotifyProgress(new UploadProgressUpdatedEventArgs
            {
                QueueItemId = item.Id,
                VideoRecordId = item.VideoRecordId,
                BytesUploaded = notification.BytesSent,
                TotalBytes = item.TotalBytes,
                Percentage = notification.Percentage,
                SpeedBytesPerSecond = speed,
                Status = notification.Status
            });
        });

        // 3. Executar Upload com Throttling configurado
        item.Status = UploadStatus.Uploading;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _queueRepo.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _queueService.NotifyStatusChanged(new QueueItemStatusChangedEventArgs
        {
            QueueItemId = item.Id,
            NewStatus = UploadStatus.Uploading
        });

        var result = await _uploader.UploadVideoAsync(item.VideoRecord!, progress, cancellationToken);

        // 4. Conclusão ou Falha
        if (result.Success && !string.IsNullOrWhiteSpace(result.VideoId))
        {
            item.Status = UploadStatus.Completed;
            item.CompletedAtUtc = DateTime.UtcNow;
            item.UploadSpeedBytesPerSec = 0;
            item.BytesUploaded = item.TotalBytes;

            if (item.VideoRecord != null)
            {
                item.VideoRecord.UploadStatus = UploadStatus.Completed;
                item.VideoRecord.YouTubeVideoId = result.VideoId;
                item.VideoRecord.YouTubeUrl = result.YouTubeUrl;
            }

            _logger?.LogInformation("Upload concluído com sucesso para o item {ItemId} (YouTube ID: {YtId}).", item.Id, result.VideoId);
        }
        else
        {
            item.Status = UploadStatus.Failed;
            item.LastError = result.ErrorMessage;
            item.UploadSpeedBytesPerSec = 0;

            if (item.VideoRecord != null)
            {
                item.VideoRecord.UploadStatus = UploadStatus.Failed;
            }

            _logger?.LogWarning("Upload falhou para o item {ItemId}: {Error}", item.Id, result.ErrorMessage);
        }

        item.UpdatedAtUtc = DateTime.UtcNow;
        await _queueRepo.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _queueService.NotifyStatusChanged(new QueueItemStatusChangedEventArgs
        {
            QueueItemId = item.Id,
            NewStatus = item.Status,
            ErrorMessage = item.LastError
        });
    }
}
