using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.FileSystem;
using ReplayManager.Application.Contracts.Ingestion;
using ReplayManager.Application.Contracts.Media;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Application.Models.Ingestion;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.Application.Services;

/// <summary>
/// Orquestrador de ingestão de vídeos. Valida estabilidade do arquivo, calcula hash SHA-256
/// e persiste o registro no banco de dados respeitando a regra de deduplicação intra-pasta.
/// Se configurado, dispara o pós-processamento de metadados e miniaturas via FFmpeg.
/// </summary>
public class VideoIngestionService : IVideoIngestionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentHasher _hasher;
    private readonly IFileStabilityChecker _stabilityChecker;
    private readonly IVideoMediaProcessor? _mediaProcessor;
    private readonly ILogger<VideoIngestionService>? _logger;

    public VideoIngestionService(
        IUnitOfWork unitOfWork,
        IContentHasher hasher,
        IFileStabilityChecker stabilityChecker,
        IVideoMediaProcessor? mediaProcessor = null,
        ILogger<VideoIngestionService>? logger = null)
    {
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _stabilityChecker = stabilityChecker;
        _mediaProcessor = mediaProcessor;
        _logger = logger;
    }

    public async Task<IngestionResult> IngestVideoFileAsync(
        Guid monitoredFolderId,
        string filePath,
        bool waitForStability = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            _logger?.LogWarning("Arquivo '{FilePath}' não encontrado no disco para ingestão.", filePath);
            return IngestionResult.NotFound(filePath);
        }

        if (waitForStability)
        {
            _logger?.LogDebug("Aguardando estabilização do arquivo '{FilePath}'...", filePath);
            var isStable = await _stabilityChecker.WaitForStabilityAsync(
                filePath,
                cancellationToken: cancellationToken);

            if (!isStable)
            {
                _logger?.LogWarning("Arquivo '{FilePath}' não se estabilizou a tempo.", filePath);
                return IngestionResult.StabilityFailed(filePath);
            }
        }

        try
        {
            var fileInfo = new FileInfo(filePath);
            var hash = await _hasher.ComputeSha256Async(filePath, cancellationToken);

            _logger?.LogDebug("Hash SHA-256 calculado para '{FilePath}': {Hash}", filePath, hash);

            // 1. Verificar se o hash já existe na mesma pasta monitorada (Deduplicação Intra-Pasta)
            var existingByHash = await _unitOfWork.Videos.GetByFolderAndHashAsync(
                monitoredFolderId,
                hash,
                cancellationToken);

            if (existingByHash != null)
            {
                // Se o caminho for diferente, o arquivo foi renomeado ou movido de subpasta
                if (!string.Equals(existingByHash.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogInformation("Arquivo renomeado detectado na pasta {FolderId}: de '{Old}' para '{New}'",
                        monitoredFolderId, existingByHash.FilePath, filePath);

                    existingByHash.FilePath = filePath;
                    existingByHash.FileName = Path.GetFileName(filePath);
                    existingByHash.LocalStatus = LocalFileStatus.Available;
                    existingByHash.FileSizeBytes = fileInfo.Length;

                    await _unitOfWork.Videos.UpdateAsync(existingByHash, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return IngestionResult.Renamed(existingByHash);
                }

                // Mesmo caminho e mesmo hash: apenas assegura status Available
                if (existingByHash.LocalStatus != LocalFileStatus.Available)
                {
                    existingByHash.LocalStatus = LocalFileStatus.Available;
                    await _unitOfWork.Videos.UpdateAsync(existingByHash, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                _logger?.LogInformation("Arquivo idêntico já cadastrado na pasta {FolderId}: '{FilePath}'",
                    monitoredFolderId, filePath);
                return IngestionResult.Duplicate(existingByHash);
            }

            // 2. Verificar se existe registro no mesmo caminho com hash diferente (arquivo sobrescrito)
            var existingByPath = await _unitOfWork.Videos.GetByFilePathAsync(filePath, cancellationToken);
            if (existingByPath != null && existingByPath.MonitoredFolderId == monitoredFolderId)
            {
                _logger?.LogInformation("Arquivo sobrescrito detectado em '{FilePath}'. Atualizando hash e metadados.", filePath);

                existingByPath.ContentSha256 = hash;
                existingByPath.FileSizeBytes = fileInfo.Length;
                existingByPath.LocalStatus = LocalFileStatus.Available;
                existingByPath.RecordedAtUtc = ResolveRecordedAtUtc(fileInfo);
                existingByPath.FilterStatus = FilterEvaluationStatus.PendingEvaluation;

                await _unitOfWork.Videos.UpdateAsync(existingByPath, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return IngestionResult.Success(existingByPath);
            }

            // 3. Novo registro canônico
            var recordedAt = ResolveRecordedAtUtc(fileInfo);

            var newVideo = new VideoRecord
            {
                Id = Guid.NewGuid(),
                MonitoredFolderId = monitoredFolderId,
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                ContentSha256 = hash,
                FileSizeBytes = fileInfo.Length,
                RecordedAtUtc = recordedAt,
                LocalStatus = LocalFileStatus.Available,
                UploadStatus = UploadStatus.None,
                FilterStatus = FilterEvaluationStatus.PendingEvaluation,
                PrivacyStatus = YouTubePrivacyStatus.Unlisted
            };

            await _unitOfWork.Videos.AddAsync(newVideo, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation("Novo vídeo indexado com sucesso: '{FileName}' (ID: {VideoId}, Pasta: {FolderId})",
                newVideo.FileName, newVideo.Id, monitoredFolderId);

            if (_mediaProcessor != null)
            {
                _logger?.LogDebug("Iniciando extração de metadados e miniatura para o vídeo {VideoId}...", newVideo.Id);
                await _mediaProcessor.ProcessVideoMediaAsync(newVideo.Id, cancellationToken);
            }

            return IngestionResult.Success(newVideo);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Erro inesperado ao ingerir o arquivo '{FilePath}'", filePath);
            return IngestionResult.Failed(ex.Message);
        }
    }

    private static DateTime ResolveRecordedAtUtc(FileInfo fileInfo)
    {
        var creation = fileInfo.CreationTimeUtc;
        var write = fileInfo.LastWriteTimeUtc;
        return creation < write ? creation : write;
    }
}

