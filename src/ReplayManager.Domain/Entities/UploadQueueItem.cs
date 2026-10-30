using ReplayManager.Domain.Common;
using ReplayManager.Domain.Enums;

namespace ReplayManager.Domain.Entities;

/// <summary>
/// Representa uma entrada persistente na fila de upload para o YouTube.
/// </summary>
public class UploadQueueItem : BaseEntity
{
    /// <summary>
    /// ID do registro do vídeo a ser enviado.
    /// </summary>
    public Guid VideoRecordId { get; set; }

    /// <summary>
    /// Referência de navegação para o vídeo.
    /// </summary>
    public VideoRecord? VideoRecord { get; set; }

    /// <summary>
    /// Ordem de prioridade na fila (itens com menor valor de prioridade são processados primeiro).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Situação atual do item na fila (Queued, Preparing, Uploading, Paused, Interrupted, Failed, Completed).
    /// </summary>
    public UploadStatus Status { get; set; } = UploadStatus.Queued;

    /// <summary>
    /// Quantidade de bytes já transmitidos com sucesso.
    /// </summary>
    public long BytesUploaded { get; set; } = 0;

    /// <summary>
    /// Tamanho total do arquivo a transmitir em bytes.
    /// </summary>
    public long TotalBytes { get; set; } = 0;

    /// <summary>
    /// Velocidade instantânea estimada de upload em bytes por segundo.
    /// </summary>
    public long UploadSpeedBytesPerSec { get; set; } = 0;

    /// <summary>
    /// Número de tentativas já realizadas para este envio.
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Mensagem do último erro ou exceção ocorrida durante a transmissão.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Momento UTC em que o vídeo foi enfileirado.
    /// </summary>
    public DateTime QueuedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Momento UTC do início efetivo do envio.
    /// </summary>
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// Momento UTC da conclusão do upload com confirmação do YouTube.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// URI de sessão de upload retomável (Resumable Upload URI fornecida pela API do Google).
    /// Permite reiniciar o upload do ponto exato onde foi interrompido.
    /// </summary>
    public string? ResumableSessionUri { get; set; }
}
