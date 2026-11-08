namespace ReplayManager.App.Services;

/// <summary>
/// Contrato para execução de player de mídia externo (VLC ou player padrão do Windows).
/// </summary>
public interface IMediaPlayerLauncher
{
    /// <summary>
    /// Executa a reprodução do arquivo de vídeo.
    /// Tenta VLC primeiro; caso não esteja instalado, usa o player padrão do sistema.
    /// </summary>
    /// <param name="filePath">Caminho do vídeo a ser reproduzido.</param>
    /// <returns>True se o processo foi iniciado com sucesso.</returns>
    bool PlayVideo(string filePath);

    /// <summary>
    /// Verifica se o player VLC está instalado no sistema.
    /// </summary>
    bool IsVlcInstalled();

    /// <summary>
    /// Retorna o caminho do executável do VLC se encontrado.
    /// </summary>
    string? GetVlcExecutablePath();
}

