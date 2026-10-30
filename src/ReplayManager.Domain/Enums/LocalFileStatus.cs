namespace ReplayManager.Domain.Enums;

/// <summary>
/// Representa o estado do arquivo de vídeo no sistema de arquivos local.
/// </summary>
public enum LocalFileStatus
{
    /// <summary>
    /// Arquivo presente no disco e acessível para leitura.
    /// </summary>
    Available = 0,

    /// <summary>
    /// Arquivo não encontrado no caminho original (apagado ou movido externamente pelo usuário).
    /// </summary>
    Missing = 1,

    /// <summary>
    /// Arquivo detectado, porém ainda em processo de gravação (tamanho instável ou stream bloqueado).
    /// </summary>
    LockedInRecording = 2,

    /// <summary>
    /// Arquivo ilegível ou corrompido segundo análise de container/FFmpeg.
    /// </summary>
    Corrupted = 3
}
