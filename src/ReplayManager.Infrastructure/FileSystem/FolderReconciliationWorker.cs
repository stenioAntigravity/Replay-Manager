using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.FileSystem;
using ReplayManager.Application.Contracts.Ingestion;
using ReplayManager.Application.Contracts.Media;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Application.Models.FileSystem;
using ReplayManager.Application.Models.Ingestion;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.Infrastructure.FileSystem;

/// <summary>
/// Worker de reconciliação de diretórios. Sincroniza periodicamente ou sob demanda
/// o estado do sistema de arquivos físico com os registros persistidos no SQLite.
/// </summary>
public class FolderReconciliationWorker : IFolderReconciliationWorker
{
    private static readonly HashSet<string> DefaultExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".mkv",
        ".avi"
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IVideoIngestionService _ingestionService;
    private readonly IVideoMediaProcessor? _mediaProcessor;
    private readonly ILogger<FolderReconciliationWorker>? _logger;

    public FolderReconciliationWorker(
        IUnitOfWork unitOfWork,
        IVideoIngestionService ingestionService,
        IVideoMediaProcessor? mediaProcessor = null,
        ILogger<FolderReconciliationWorker>? logger = null)
    {
        _unitOfWork = unitOfWork;
        _ingestionService = ingestionService;
        _mediaProcessor = mediaProcessor;
        _logger = logger;
    }

    public async Task<ReconciliationResult> ReconcileFolderAsync(
        Guid monitoredFolderId,
        CancellationToken cancellationToken = default)
    {
        var result = new ReconciliationResult
        {
            MonitoredFolderId = monitoredFolderId
        };

        var folder = await _unitOfWork.MonitoredFolders.GetByIdAsync(monitoredFolderId, cancellationToken);
        if (folder == null)
        {
            var msg = $"Pasta monitorada com ID '{monitoredFolderId}' não encontrada no banco.";
            _logger?.LogWarning(msg);
            result.Errors.Add(msg);
            return result;
        }

        result.FolderPath = folder.Path;

        if (!Directory.Exists(folder.Path))
        {
            var msg = $"Diretório físico não existe no disco: '{folder.Path}'";
            _logger?.LogWarning(msg);
            result.Errors.Add(msg);

            // Marca todos os vídeos desta pasta como Missing (preservando o catálogo)
            var videos = await _unitOfWork.Videos.GetByFolderIdAsync(monitoredFolderId, cancellationToken);
            foreach (var video in videos.Where(v => v.LocalStatus != LocalFileStatus.Missing))
            {
                video.LocalStatus = LocalFileStatus.Missing;
                await _unitOfWork.Videos.UpdateAsync(video, cancellationToken);
                result.MissingFilesMarked++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }

        try
        {
            var allowedExtensions = ResolveExtensions(folder);
            var searchOption = (folder.FilterRule?.IncludeSubdirectories ?? false)
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var physicalFiles = Directory.EnumerateFiles(folder.Path, "*.*", searchOption)
                .Where(f => allowedExtensions.Contains(Path.GetExtension(f)))
                .ToList();

            result.TotalScannedFiles = physicalFiles.Count;

            var existingVideos = await _unitOfWork.Videos.GetByFolderIdAsync(monitoredFolderId, cancellationToken);
            var existingByPath = existingVideos.ToDictionary(v => v.FilePath, StringComparer.OrdinalIgnoreCase);
            var physicalFileSet = new HashSet<string>(physicalFiles, StringComparer.OrdinalIgnoreCase);

            // 1. Detectar arquivos que constam no banco mas sumiram do disco
            foreach (var video in existingVideos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var existsOnDisk = physicalFileSet.Contains(video.FilePath) || File.Exists(video.FilePath);
                if (!existsOnDisk)
                {
                    if (video.LocalStatus != LocalFileStatus.Missing)
                    {
                        _logger?.LogInformation("Arquivo ausente no disco detectado na reconciliação: '{Path}'. Marcando como Missing.", video.FilePath);
                        video.LocalStatus = LocalFileStatus.Missing;
                        await _unitOfWork.Videos.UpdateAsync(video, cancellationToken);
                        result.MissingFilesMarked++;
                    }
                }
                else
                {
                    if (video.LocalStatus == LocalFileStatus.Missing)
                    {
                        _logger?.LogInformation("Arquivo previamente ausente voltou a ficar disponível: '{Path}'.", video.FilePath);
                        video.LocalStatus = LocalFileStatus.Available;
                        await _unitOfWork.Videos.UpdateAsync(video, cancellationToken);
                    }
                    result.ExistingFilesPreserved++;
                }
            }

            // 2. Ingerir novos arquivos físicos encontrados no disco
            foreach (var physicalFile in physicalFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!existingByPath.ContainsKey(physicalFile))
                {
                    var ingestResult = await _ingestionService.IngestVideoFileAsync(
                        monitoredFolderId,
                        physicalFile,
                        waitForStability: false,
                        cancellationToken: cancellationToken);

                    if (ingestResult.Status == IngestionStatus.Ingested)
                    {
                        result.NewFilesIngested++;
                    }
                    else if (ingestResult.Status == IngestionStatus.RenamedUpdated)
                    {
                        result.ExistingFilesPreserved++;
                    }
                    else if (ingestResult.Status == IngestionStatus.DuplicateSameFolder)
                    {
                        result.ExistingFilesPreserved++;
                    }
                    else if (ingestResult.Status == IngestionStatus.Error)
                    {
                        result.Errors.Add($"Falha ao ingerir '{physicalFile}': {ingestResult.Message}");
                    }
                }
            }

            // 3. Garantir pós-processamento de metadados e miniaturas para vídeos que ainda não possuem thumbnail
            if (_mediaProcessor != null)
            {
                var videosWithoutThumbs = await _unitOfWork.Videos.GetByFolderIdAsync(monitoredFolderId, cancellationToken);
                foreach (var v in videosWithoutThumbs.Where(x => string.IsNullOrWhiteSpace(x.ThumbnailPath) || !File.Exists(x.ThumbnailPath)))
                {
                    if (v.LocalStatus != LocalFileStatus.Missing && File.Exists(v.FilePath))
                    {
                        try
                        {
                            await _mediaProcessor.ProcessVideoMediaAsync(v.Id, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Erro ao pós-processar miniatura de '{File}' durante varredura.", v.FileName);
                        }
                    }
                }
            }

            folder.LastScannedAtUtc = DateTime.UtcNow;
            await _unitOfWork.MonitoredFolders.UpdateAsync(folder, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation(
                "Reconciliação da pasta '{Path}' concluída. Total: {Total}, Novos: {New}, Preservados: {Preserved}, Ausentes: {Missing}",
                folder.Path, result.TotalScannedFiles, result.NewFilesIngested, result.ExistingFilesPreserved, result.MissingFilesMarked);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Erro durante a reconciliação da pasta '{Path}'", folder.Path);
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<IReadOnlyList<ReconciliationResult>> ReconcileAllActiveFoldersAsync(
        CancellationToken cancellationToken = default)
    {
        var activeFolders = await _unitOfWork.MonitoredFolders.GetAllAsync(onlyActive: true, cancellationToken);
        var results = new List<ReconciliationResult>();

        foreach (var folder in activeFolders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var res = await ReconcileFolderAsync(folder.Id, cancellationToken);
            results.Add(res);
        }

        return results.AsReadOnly();
    }

    private static HashSet<string> ResolveExtensions(MonitoredFolder folder)
    {
        if (folder.FilterRule != null && !string.IsNullOrWhiteSpace(folder.FilterRule.AllowedExtensions))
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parts = folder.FilterRule.AllowedExtensions.Split(
                new[] { ',', ';', ' ' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                var clean = part.StartsWith('.') ? part : $".{part}";
                set.Add(clean);
            }

            if (set.Count > 0)
            {
                return set;
            }
        }

        return new HashSet<string>(DefaultExtensions, StringComparer.OrdinalIgnoreCase);
    }
}
