using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReplayManager.Application.Models.YouTube;
using ReplayManager.Domain.Entities;

namespace ReplayManager.Application.Contracts.Upload;

public class UploadProgressUpdatedEventArgs : EventArgs
{
    public Guid QueueItemId { get; init; }
    public Guid VideoRecordId { get; init; }
    public long BytesUploaded { get; init; }
    public long TotalBytes { get; init; }
    public double Percentage { get; init; }
    public long SpeedBytesPerSecond { get; init; }
    public string Status { get; init; } = string.Empty;
}

public class QueueItemStatusChangedEventArgs : EventArgs
{
    public Guid QueueItemId { get; init; }
    public Domain.Enums.UploadStatus NewStatus { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Contrato do serviço aplicacional de gerenciamento e controle da fila persistente de uploads.
/// </summary>
public interface IUploadQueueService
{
    event EventHandler<UploadProgressUpdatedEventArgs>? ProgressUpdated;
    event EventHandler<QueueItemStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Limite de velocidade de upload em bytes por segundo (0 = ilimitado).
    /// </summary>
    long BandwidthLimitBytesPerSecond { get; set; }

    /// <summary>
    /// Obtém todos os itens da fila ordenados por prioridade e data.
    /// </summary>
    Task<IReadOnlyList<UploadQueueItem>> GetQueueItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enfileira um vídeo para upload.
    /// </summary>
    Task<UploadQueueItem> EnqueueVideoAsync(Guid videoRecordId, int? priority = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pausa um item na fila de upload.
    /// </summary>
    Task<bool> PauseItemAsync(Guid queueItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retoma um item pausado ou interrompido.
    /// </summary>
    Task<bool> ResumeItemAsync(Guid queueItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancela e remove um item da fila.
    /// </summary>
    Task<bool> CancelItemAsync(Guid queueItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenta reenviar manualmente um item com falha.
    /// </summary>
    Task<bool> RetryItemAsync(Guid queueItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reordena a prioridade de um item na fila.
    /// </summary>
    Task<bool> ChangePriorityAsync(Guid queueItemId, int newPriority, CancellationToken cancellationToken = default);

    /// <summary>
    /// Move um item uma posição para cima na fila.
    /// </summary>
    Task<bool> MoveUpAsync(Guid queueItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Move um item uma posição para baixo na fila.
    /// </summary>
    Task<bool> MoveDownAsync(Guid queueItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispara a notificação de progresso da fila para inscritos.
    /// </summary>
    void NotifyProgress(UploadProgressUpdatedEventArgs e);

    /// <summary>
    /// Dispara a notificação de alteração de status para inscritos.
    /// </summary>
    void NotifyStatusChanged(QueueItemStatusChangedEventArgs e);
}
