namespace ReplayManager.Domain.Enums;

/// <summary>
/// Resultado da avaliação das regras de filtro configuradas para a pasta monitorada.
/// </summary>
public enum FilterEvaluationStatus
{
    /// <summary>
    /// O vídeo atende a todos os critérios e está elegível para auto-upload (se habilitado).
    /// </summary>
    Approved = 0,

    /// <summary>
    /// Reprovado por estar abaixo do tamanho mínimo configurado (padrão inicial: 10 MB).
    /// </summary>
    RejectedByMinSize = 1,

    /// <summary>
    /// Reprovado por exceder o tamanho máximo configurado (padrão inicial: 100 MB).
    /// </summary>
    RejectedByMaxSize = 2,

    /// <summary>
    /// Reprovado por ter duração menor que o limite mínimo estipulado.
    /// </summary>
    RejectedByMinDuration = 3,

    /// <summary>
    /// Reprovado por ter duração superior ao limite máximo estipulado.
    /// </summary>
    RejectedByMaxDuration = 4,

    /// <summary>
    /// Reprovado por possuir extensão diferente das aceitas (.mp4, .mkv, .avi).
    /// </summary>
    RejectedByExtension = 5,

    /// <summary>
    /// Reprovado por não corresponder ao padrão/regex de nomenclatura exigido.
    /// </summary>
    RejectedByNamePattern = 6,

    /// <summary>
    /// Ainda não avaliado ou avaliação pendente de metadados FFmpeg.
    /// </summary>
    PendingEvaluation = 7
}
