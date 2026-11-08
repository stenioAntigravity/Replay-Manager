using ReplayManager.Application.Models.YouTube;
using ReplayManager.Domain.Entities;

namespace ReplayManager.Application.Contracts.YouTube;

/// <summary>
/// Contrato do serviço de upload resumable de vídeos para o YouTube.
/// </summary>
public interface IYouTubeUploader
{
    /// <summary>
    /// Realiza o upload resumable de um vídeo para o YouTube.
    /// </summary>
    /// <param name="video">Registro do vídeo com metadados.</param>
    /// <param name="progress">Callback para notificação de progresso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com o ID e URL do vídeo publicado.</returns>
    Task<YouTubeUploadResult> UploadVideoAsync(
        VideoRecord video,
        IProgress<UploadProgressNotification>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza títulos e descrições de um vídeo já existente no YouTube sem alterar o arquivo.
    /// </summary>
    Task<bool> UpdateVideoMetadataAsync(
        string youTubeVideoId,
        string title,
        string description,
        CancellationToken cancellationToken = default);
}

