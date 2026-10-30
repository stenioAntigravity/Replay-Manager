namespace ReplayManager.Domain.Enums;

/// <summary>
/// Representa o ciclo de vida e estado do upload de um vídeo para o YouTube.
/// </summary>
public enum UploadStatus
{
    /// <summary>
    /// Vídeo presente na biblioteca, sem solicitação de upload.
    /// </summary>
    None = 0,

    /// <summary>
    /// Vídeo posicionado na fila de upload aguardando sua vez.
    /// </summary>
    Queued = 1,

    /// <summary>
    /// Em preparação (autenticação, geração de metadados, inicialização de sessão resumable).
    /// </summary>
    Preparing = 2,

    /// <summary>
    /// Transmissão de bytes ativa em andamento.
    /// </summary>
    Uploading = 3,

    /// <summary>
    /// Upload pausado manualmente pelo usuário.
    /// </summary>
    Paused = 4,

    /// <summary>
    /// Upload interrompido por desconexão, fechamento do app ou erro transitório de rede.
    /// </summary>
    Interrupted = 5,

    /// <summary>
    /// Falha permanente após tentativas esgotadas ou rejeição da API do YouTube.
    /// </summary>
    Failed = 6,

    /// <summary>
    /// Upload concluído com sucesso, vídeo processado e link retornado.
    /// </summary>
    Completed = 7
}

