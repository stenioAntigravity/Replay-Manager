using ReplayManager.Application.Models.Media;

namespace ReplayManager.Application.Contracts.Media;

/// <summary>
/// Contrato para geração de miniaturas (thumbnails) de arquivos de vídeo via FFmpeg.
/// </summary>
public interface IThumbnailGenerator
{
    /// <summary>
    /// Gera uma miniatura capturando um frame no timestamp especificado.
    /// </summary>
    /// <param name="videoFilePath">Caminho completo do arquivo de vídeo.</param>
    /// <param name="outputPath">Caminho onde a miniatura será salva.</param>
    /// <param name="timestampSeconds">Segundo exato para captura do frame (se null, calcula automaticamente a 15% da duração).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da geração da miniatura.</returns>
    Task<ThumbnailResult> GenerateThumbnailAsync(
        string videoFilePath,
        string outputPath,
        double? timestampSeconds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calcula o timestamp ideal para captura de thumbnail (padrão: 15% da duração total).
    /// </summary>
    /// <param name="durationSeconds">Duração total do vídeo em segundos.</param>
    /// <param name="percentage">Percentual da duração para o timestamp (padrão: 0.15).</param>
    /// <returns>Timestamp em segundos.</returns>
    double CalculateThumbnailTimestamp(double durationSeconds, double percentage = 0.15);

    /// <summary>
    /// Substitui manualmente a miniatura de um vídeo por um arquivo de imagem selecionado pelo usuário.
    /// A imagem é copiada e padronizada no diretório de miniaturas da aplicação.
    /// </summary>
    /// <param name="sourceImagePath">Caminho do arquivo de imagem de origem.</param>
    /// <param name="videoId">ID do vídeo proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Caminho final da imagem no cache de miniaturas.</returns>
    Task<string> ReplaceThumbnailAsync(
        string sourceImagePath,
        Guid videoId,
        CancellationToken cancellationToken = default);
}

