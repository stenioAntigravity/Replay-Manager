using ReplayManager.Application.Models.Ingestion;

namespace ReplayManager.Application.Contracts.Ingestion;

/// <summary>
/// Contrato do serviço de ingestão de vídeos na biblioteca, orquestrando
/// verificação de estabilidade, cálculo de hash SHA-256 e deduplicação intra-pasta.
/// </summary>
public interface IVideoIngestionService
{
    /// <summary>
    /// Processa e ingere um arquivo de vídeo recém-descoberto ou reconciliado.
    /// </summary>
    /// <param name="monitoredFolderId">Identificador da pasta monitorada proprietária.</param>
    /// <param name="filePath">Caminho físico completo do arquivo de vídeo.</param>
    /// <param name="waitForStability">Se deve aguardar estabilização do arquivo caso esteja em gravação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação de ingestão.</returns>
    Task<IngestionResult> IngestVideoFileAsync(
        Guid monitoredFolderId,
        string filePath,
        bool waitForStability = true,
        CancellationToken cancellationToken = default);
}

