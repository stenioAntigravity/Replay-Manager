using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.Filters;
using ReplayManager.Application.Contracts.Media;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.Application.Services;

/// <summary>
/// Serviço de pós-processamento de mídia que extrai metadados via FFprobe,
/// gera thumbnails via FFmpeg e avalia filtros para auto-upload na pasta monitorada.
/// </summary>
public class VideoMediaProcessor : IVideoMediaProcessor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaMetadataExtractor _metadataExtractor;
    private readonly IThumbnailGenerator _thumbnailGenerator;
    private readonly IFilterEvaluator? _filterEvaluator;
    private readonly ILogger<VideoMediaProcessor>? _logger;

    /// <summary>
    /// Diretório base para armazenamento de thumbnails.
    /// Padrão: %LOCALAPPDATA%/ReplayManager/Thumbnails
    /// </summary>
    private readonly string _thumbnailDirectory;

    public VideoMediaProcessor(
        IUnitOfWork unitOfWork,
        IMediaMetadataExtractor metadataExtractor,
        IThumbnailGenerator thumbnailGenerator,
        IFilterEvaluator? filterEvaluator = null,
        ILogger<VideoMediaProcessor>? logger = null,
        string? thumbnailDirectory = null)
    {
        _unitOfWork = unitOfWork;
        _metadataExtractor = metadataExtractor;
        _thumbnailGenerator = thumbnailGenerator;
        _filterEvaluator = filterEvaluator;
        _logger = logger;
        _thumbnailDirectory = thumbnailDirectory ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ReplayManager",
                "Thumbnails");
    }

    public async Task<bool> ProcessVideoMediaAsync(
        Guid videoId,
        CancellationToken cancellationToken = default)
    {
        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken);
        if (video == null)
        {
            _logger?.LogWarning("VideoRecord com ID '{VideoId}' não encontrado para processamento de mídia.", videoId);
            return false;
        }

        if (!File.Exists(video.FilePath))
        {
            _logger?.LogWarning("Arquivo físico '{Path}' não encontrado no disco. Pulando processamento de mídia.", video.FilePath);
            return false;
        }

        var anyUpdated = false;

        // 1. Extrair metadados via FFprobe
        try
        {
            var metadata = await _metadataExtractor.ExtractMetadataAsync(video.FilePath, cancellationToken);
            if (metadata != null)
            {
                if (metadata.DurationSeconds > 0)
                    video.DurationSeconds = metadata.DurationSeconds;

                if (metadata.Width > 0 && metadata.Height > 0)
                {
                    video.ResolutionWidth = metadata.Width;
                    video.ResolutionHeight = metadata.Height;
                }

                anyUpdated = true;

                _logger?.LogInformation(
                    "Metadados extraídos para '{FileName}': {Dur}s, {W}x{H}, codec={Codec}",
                    video.FileName, metadata.DurationSeconds, metadata.Width, metadata.Height, metadata.VideoCodec);

                // 2. Gerar thumbnail com timestamp calculado a 15%
                await GenerateThumbnailForVideoAsync(video, metadata.DurationSeconds, cancellationToken);
            }
            else
            {
                _logger?.LogWarning("Não foi possível extrair metadados de '{FileName}'. FFprobe pode não estar disponível.", video.FileName);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Erro durante extração de metadados de '{FileName}'", video.FileName);
        }

        // 3. Avaliação de Filtros e Automação (Fase 5)
        if (_filterEvaluator != null)
        {
            try
            {
                var folder = await _unitOfWork.MonitoredFolders.GetByIdAsync(video.MonitoredFolderId, cancellationToken);
                if (folder != null)
                {
                    var evalResult = _filterEvaluator.Evaluate(video, folder.FilterRule, folder.Path);
                    video.FilterStatus = evalResult.Status;
                    video.RejectionReason = evalResult.Reason;
                    anyUpdated = true;

                    _logger?.LogInformation("Filtro avaliado para '{FileName}': {Status} - {Reason}",
                        video.FileName, evalResult.Status, evalResult.Reason);

                    // Se a pasta possui AutoUpload ativo e o vídeo foi aprovado
                    if (folder.AutoUploadEnabled && evalResult.IsApproved)
                    {
                        if (video.UploadStatus == UploadStatus.None)
                        {
                            var queueItem = new UploadQueueItem
                            {
                                Id = Guid.NewGuid(),
                                VideoRecordId = video.Id,
                                Status = UploadStatus.Queued,
                                Priority = 10,
                                QueuedAtUtc = DateTime.UtcNow
                            };

                            await _unitOfWork.UploadQueue.AddAsync(queueItem, cancellationToken);
                            video.UploadStatus = UploadStatus.Queued;

                            _logger?.LogInformation("Vídeo '{FileName}' aprovado nos filtros e enfileirado automaticamente para upload (AutoUploadEnabled=true).", video.FileName);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogError(ex, "Erro ao avaliar filtros ou enfileirar auto-upload para '{FileName}'", video.FileName);
            }
        }

        if (anyUpdated)
        {
            await _unitOfWork.Videos.UpdateAsync(video, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return anyUpdated;
    }

    private async Task GenerateThumbnailForVideoAsync(
        Domain.Entities.VideoRecord video,
        double durationSeconds,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Directory.Exists(_thumbnailDirectory))
            {
                Directory.CreateDirectory(_thumbnailDirectory);
            }

            var thumbnailFileName = $"{video.Id}.jpg";
            var outputPath = Path.Combine(_thumbnailDirectory, thumbnailFileName);

            var timestamp = _thumbnailGenerator.CalculateThumbnailTimestamp(durationSeconds);

            var result = await _thumbnailGenerator.GenerateThumbnailAsync(
                video.FilePath,
                outputPath,
                timestamp,
                cancellationToken);

            if (result.Success)
            {
                video.ThumbnailPath = result.OutputPath;
                _logger?.LogInformation("Thumbnail gerada para '{FileName}': '{ThumbPath}' (ts={Ts}s)",
                    video.FileName, result.OutputPath, result.TimestampSeconds);
            }
            else
            {
                _logger?.LogWarning("Falha ao gerar thumbnail para '{FileName}': {Error}",
                    video.FileName, result.Error);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Erro na geração de thumbnail para '{FileName}'", video.FileName);
        }
    }
}

