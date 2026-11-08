namespace ReplayManager.Application.Contracts.Media;

/// <summary>
/// Contrato para resolução dos caminhos de binários FFmpeg e FFprobe no sistema.
/// Procura em: PATH do sistema, pasta da aplicação e configuração customizada.
/// </summary>
public interface IFFmpegPathResolver
{
    /// <summary>
    /// Resolve o caminho completo para o executável ffprobe.
    /// </summary>
    /// <returns>Caminho absoluto do ffprobe.exe, ou null se não encontrado.</returns>
    string? ResolveFFprobePath();

    /// <summary>
    /// Resolve o caminho completo para o executável ffmpeg.
    /// </summary>
    /// <returns>Caminho absoluto do ffmpeg.exe, ou null se não encontrado.</returns>
    string? ResolveFFmpegPath();

    /// <summary>
    /// Verifica se os binários FFmpeg e FFprobe estão disponíveis no sistema.
    /// </summary>
    bool IsFFmpegAvailable();
}

