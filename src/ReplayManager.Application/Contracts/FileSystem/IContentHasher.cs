namespace ReplayManager.Application.Contracts.FileSystem;

/// <summary>
/// Contrato para cálculo de hash criptográfico de conteúdo de arquivos via streaming assíncrono.
/// </summary>
public interface IContentHasher
{
    /// <summary>
    /// Calcula o hash SHA-256 do arquivo informado via streaming, sem carregar o arquivo integralmente na memória.
    /// </summary>
    /// <param name="filePath">Caminho completo do arquivo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>String hex em caixa baixa com 64 caracteres.</returns>
    Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calcula o hash SHA-256 do fluxo informado via streaming.
    /// </summary>
    /// <param name="stream">Fluxo de leitura.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>String hex em caixa baixa com 64 caracteres.</returns>
    Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken = default);
}

