using ReplayManager.Domain.Common;
using ReplayManager.Domain.Enums;

namespace ReplayManager.Domain.Entities;

/// <summary>
/// Representa um registro de vídeo indexado na biblioteca local do Replay Manager.
/// </summary>
public class VideoRecord : BaseEntity
{
    /// <summary>
    /// Chave estrangeira para a pasta monitorada onde o vídeo foi detectado.
    /// </summary>
    public Guid MonitoredFolderId { get; set; }

    /// <summary>
    /// Referência de navegação para a pasta monitorada.
    /// </summary>
    public MonitoredFolder? MonitoredFolder { get; set; }

    /// <summary>
    /// Caminho completo do arquivo de vídeo no sistema de arquivos local.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Nome do arquivo com extensão (ex: "Replay_2026-10-30.mp4").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Hash SHA-256 do conteúdo do arquivo (64 caracteres hexadecimais em caixa baixa).
    /// Utilizado para deduplicação exclusivamente dentro da mesma pasta.
    /// </summary>
    public string ContentSha256 { get; set; } = string.Empty;

    /// <summary>
    /// Tamanho do arquivo em bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Duração do vídeo em segundos (obtida via FFmpeg).
    /// </summary>
    public double? DurationSeconds { get; set; }

    /// <summary>
    /// Largura do vídeo em pixels (ex: 1920).
    /// </summary>
    public int? ResolutionWidth { get; set; }

    /// <summary>
    /// Altura do vídeo em pixels (ex: 1080).
    /// </summary>
    public int? ResolutionHeight { get; set; }

    /// <summary>
    /// Data/hora de gravação ou criação do arquivo original.
    /// </summary>
    public DateTime? RecordedAtUtc { get; set; }

    /// <summary>
    /// Estado do arquivo no disco local (Available, Missing, LockedInRecording, Corrupted).
    /// </summary>
    public LocalFileStatus LocalStatus { get; set; } = LocalFileStatus.Available;

    /// <summary>
    /// Estado do processo de upload para o YouTube.
    /// </summary>
    public UploadStatus UploadStatus { get; set; } = UploadStatus.None;

    /// <summary>
    /// Resultado da avaliação dos filtros da pasta (Approved, RejectedByMinSize, etc.).
    /// </summary>
    public FilterEvaluationStatus FilterStatus { get; set; } = FilterEvaluationStatus.PendingEvaluation;

    /// <summary>
    /// Mensagem descritiva explicando o motivo da reprovação nos filtros, se houver.
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Caminho do arquivo de miniatura (thumbnail) salvo no cache local do aplicativo.
    /// </summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// ID do vídeo retornado pelo YouTube após a inserção (ex: "dQw4w9WgXcQ").
    /// </summary>
    public string? YouTubeVideoId { get; set; }

    /// <summary>
    /// URL completa do vídeo no YouTube (ex: "https://youtu.be/dQw4w9WgXcQ").
    /// </summary>
    public string? YouTubeUrl { get; set; }

    /// <summary>
    /// Título customizado gerado ou editado pelo usuário para o YouTube.
    /// </summary>
    public string? CustomTitle { get; set; }

    /// <summary>
    /// Descrição customizada gerada por template ou editada pelo usuário.
    /// </summary>
    public string? CustomDescription { get; set; }

    /// <summary>
    /// Configuração de privacidade para o YouTube (Padrão: Unlisted / Não listado).
    /// </summary>
    public YouTubePrivacyStatus PrivacyStatus { get; set; } = YouTubePrivacyStatus.Unlisted;

    /// <summary>
    /// Item correspondente na fila de upload (1:0..1).
    /// </summary>
    public UploadQueueItem? UploadQueueItem { get; set; }
}

