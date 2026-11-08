using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.YouTube;
using ReplayManager.Application.Models.YouTube;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.Infrastructure.YouTube;

/// <summary>
/// Executa o upload resumable de arquivos de vídeo para o YouTube API v3 com suporte a throttling e miniaturas.
/// </summary>
public class YouTubeUploader : IYouTubeUploader
{
    private readonly YouTubeAuthService _authService;
    private readonly ITitleFormatter _titleFormatter;
    private readonly IDescriptionTemplateEngine _templateEngine;
    private readonly ILogger<YouTubeUploader>? _logger;

    public YouTubeUploader(
        YouTubeAuthService authService,
        ITitleFormatter titleFormatter,
        IDescriptionTemplateEngine templateEngine,
        ILogger<YouTubeUploader>? logger = null)
    {
        _authService = authService;
        _titleFormatter = titleFormatter;
        _templateEngine = templateEngine;
        _logger = logger;
    }

    public async Task<YouTubeUploadResult> UploadVideoAsync(
        VideoRecord video,
        IProgress<UploadProgressNotification>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await UploadVideoInternalAsync(video, maxBytesPerSecond: 0, progress: progress, cancellationToken: cancellationToken);
    }

    public async Task<YouTubeUploadResult> UploadVideoWithThrottlingAsync(
        VideoRecord video,
        long maxBytesPerSecond,
        IProgress<UploadProgressNotification>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await UploadVideoInternalAsync(video, maxBytesPerSecond: maxBytesPerSecond, progress: progress, cancellationToken: cancellationToken);
    }

    private async Task<YouTubeUploadResult> UploadVideoInternalAsync(
        VideoRecord video,
        long maxBytesPerSecond,
        IProgress<UploadProgressNotification>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(video);

        if (!File.Exists(video.FilePath))
        {
            return YouTubeUploadResult.Failed($"Arquivo de vídeo não encontrado no disco: '{video.FilePath}'.");
        }

        var credential = await _authService.GetUserCredentialAsync(cancellationToken);
        if (credential == null)
        {
            return YouTubeUploadResult.Failed("Conta do YouTube não autenticada.");
        }

        try
        {
            using var youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "ReplayManager"
            });

            // Resolver título (customizado ou gerador canônico PUBG)
            var title = !string.IsNullOrWhiteSpace(video.CustomTitle)
                ? video.CustomTitle
                : _titleFormatter.FormatTitle(video);

            // Resolver descrição
            var description = !string.IsNullOrWhiteSpace(video.CustomDescription)
                ? video.CustomDescription
                : _templateEngine.RenderDescription(
                    "Replay gravado pelo Replay Manager.\nData: {DateTime}\nDuração: {Duration}\nResolução: {Resolution}",
                    video);

            var privacyStatus = video.PrivacyStatus switch
            {
                YouTubePrivacyStatus.Public => "public",
                YouTubePrivacyStatus.Private => "private",
                _ => "unlisted" // Padrão: Não listado
            };

            var youtubeVideo = new Video
            {
                Snippet = new VideoSnippet
                {
                    Title = title,
                    Description = description,
                    CategoryId = "20" // Gaming category
                },
                Status = new VideoStatus
                {
                    PrivacyStatus = privacyStatus
                }
            };

            const int chunkSize = ResumableUpload.MinimumChunkSize * 4; // 1 MB chunk
            await using var rawFileStream = new FileStream(video.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var streamToUpload = maxBytesPerSecond > 0 ? (Stream)new ThrottledStream(rawFileStream, maxBytesPerSecond) : rawFileStream;

            var videosInsertRequest = youtubeService.Videos.Insert(
                youtubeVideo,
                "snippet,status",
                streamToUpload,
                "video/*");

            videosInsertRequest.ChunkSize = chunkSize;

            string? uploadedVideoId = null;
            string? errorMessage = null;

            videosInsertRequest.ProgressChanged += (IUploadProgress p) =>
            {
                progress?.Report(new UploadProgressNotification
                {
                    VideoId = video.Id,
                    BytesSent = p.BytesSent,
                    TotalBytes = rawFileStream.Length,
                    Status = p.Status.ToString()
                });

                if (p.Status == Google.Apis.Upload.UploadStatus.Failed)
                {
                    errorMessage = p.Exception?.Message ?? "Falha no envio dos bytes para o YouTube.";
                }
            };

            videosInsertRequest.ResponseReceived += (Video v) =>
            {
                uploadedVideoId = v.Id;
            };

            _logger?.LogInformation("Iniciando upload resumable para o YouTube: '{Title}' ({Size} bytes)", title, rawFileStream.Length);

            var uploadResult = await videosInsertRequest.UploadAsync(cancellationToken);

            if (uploadResult.Status == Google.Apis.Upload.UploadStatus.Completed && !string.IsNullOrWhiteSpace(uploadedVideoId))
            {
                _logger?.LogInformation("Upload concluído com sucesso. YouTube Video ID: {Id}", uploadedVideoId);

                // Enviar miniatura personalizada se existir
                if (!string.IsNullOrWhiteSpace(video.ThumbnailPath) && File.Exists(video.ThumbnailPath))
                {
                    await TryUploadThumbnailAsync(youtubeService, uploadedVideoId, video.ThumbnailPath, cancellationToken);
                }

                return YouTubeUploadResult.Succeeded(uploadedVideoId);
            }

            var err = errorMessage ?? uploadResult.Exception?.Message ?? "Erro desconhecido durante o upload.";
            _logger?.LogWarning("Upload finalizado sem sucesso: {Error}", err);
            return YouTubeUploadResult.Failed(err);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Exceção ao realizar upload do vídeo '{Path}' para o YouTube.", video.FilePath);
            return YouTubeUploadResult.Failed(ex.Message);
        }
    }

    public async Task<bool> UpdateVideoMetadataAsync(
        string youTubeVideoId,
        string title,
        string description,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(youTubeVideoId);

        var credential = await _authService.GetUserCredentialAsync(cancellationToken);
        if (credential == null) return false;

        try
        {
            using var youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "ReplayManager"
            });

            var video = new Video
            {
                Id = youTubeVideoId,
                Snippet = new VideoSnippet
                {
                    Title = title,
                    Description = description,
                    CategoryId = "20"
                }
            };

            var request = youtubeService.Videos.Update(video, "snippet");
            var updated = await request.ExecuteAsync(cancellationToken);

            return updated != null && updated.Id == youTubeVideoId;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Erro ao atualizar metadados do vídeo {Id} no YouTube.", youTubeVideoId);
            return false;
        }
    }

    private async Task TryUploadThumbnailAsync(
        YouTubeService youtubeService,
        string videoId,
        string thumbnailPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var thumbStream = new FileStream(thumbnailPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var mimeType = thumbnailPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";

            var setRequest = youtubeService.Thumbnails.Set(videoId, thumbStream, mimeType);
            var thumbResult = await setRequest.UploadAsync(cancellationToken);

            if (thumbResult.Status == Google.Apis.Upload.UploadStatus.Completed)
            {
                _logger?.LogInformation("Miniatura customizada enviada com sucesso para o vídeo {Id}.", videoId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Não foi possível definir miniatura no YouTube para o vídeo {Id}.", videoId);
        }
    }
}
