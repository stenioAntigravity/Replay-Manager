namespace ReplayManager.Application.Contracts.Export;

/// <summary>
/// Contrato do serviço de exportação de dados do catálogo e histórico de replays.
/// </summary>
public interface ILibraryExportService
{
    /// <summary>
    /// Exporta o catálogo completo de vídeos para o formato CSV.
    /// </summary>
    Task<string> ExportToCsvAsync(string destinationFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exporta o catálogo completo de vídeos para o formato JSON formatado.
    /// </summary>
    Task<string> ExportToJsonAsync(string destinationFilePath, CancellationToken cancellationToken = default);
}

