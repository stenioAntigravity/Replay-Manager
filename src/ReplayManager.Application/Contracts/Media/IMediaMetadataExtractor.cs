using ReplayManager.Application.Models.Media;

namespace ReplayManager.Application.Contracts.Media;

/// <summary>
/// Contrato para extração de metadados técnicos de arquivos de vídeo via FFprobe.
/// </summary>
public interface IMediaMetadataExtractor
{
    /// <summary>
    /// Extrai os metadados técnicos de um arquivo de vídeo.
    /// </summary>
    /// <param name="videoFilePath">Caminho completo do arquivo de vídeo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Metadados do vídeo ou null se a extração falhar.</returns>
    Task<VideoMetadata?> ExtractMetadataAsync(string videoFilePath, CancellationToken cancellationToken = default);
}

