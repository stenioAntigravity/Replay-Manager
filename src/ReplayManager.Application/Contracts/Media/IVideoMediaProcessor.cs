using ReplayManager.Application.Models.Media;

namespace ReplayManager.Application.Contracts.Media;

/// <summary>
/// Contrato do serviço de pós-processamento de mídia que coordena a extração
/// de metadados técnicos e geração de miniatura para um vídeo já ingerido.
/// </summary>
public interface IVideoMediaProcessor
{
    /// <summary>
    /// Processa a mídia do vídeo: extrai metadados (resolução, duração, codec)
    /// e gera a miniatura, atualizando o VideoRecord correspondente no banco.
    /// </summary>
    /// <param name="videoId">Identificador do VideoRecord já persistido.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>True se ao menos os metadados foram extraídos com sucesso.</returns>
    Task<bool> ProcessVideoMediaAsync(Guid videoId, CancellationToken cancellationToken = default);
}

