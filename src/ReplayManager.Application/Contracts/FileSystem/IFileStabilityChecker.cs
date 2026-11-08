namespace ReplayManager.Application.Contracts.FileSystem;

/// <summary>
/// Contrato para verificação de estabilidade física de arquivos recém-detectados,
/// garantindo que processos de gravação externa (OBS, jogos, drivers de vídeo)
/// concluíram a escrita e liberaram os locks de acesso exclusivo.
/// </summary>
public interface IFileStabilityChecker
{
    /// <summary>
    /// Aguarda de forma assíncrona até que o arquivo pare de crescer e seu lock de gravação seja liberado.
    /// </summary>
    /// <param name="filePath">Caminho completo do arquivo a inspecionar.</param>
    /// <param name="stabilityDuration">Tempo mínimo necessário sem alteração no tamanho em bytes.</param>
    /// <param name="timeout">Tempo limite total de espera antes de desistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>True se o arquivo se estabilizou e está liberado; False se atingiu timeout.</returns>
    Task<bool> WaitForStabilityAsync(
        string filePath,
        TimeSpan? stabilityDuration = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica imediatamente se o arquivo está com lock de escrita exclusivo retido por outro processo.
    /// </summary>
    /// <param name="filePath">Caminho completo do arquivo.</param>
    /// <returns>True se bloqueado; False se acessível para leitura.</returns>
    bool IsFileLocked(string filePath);
}

