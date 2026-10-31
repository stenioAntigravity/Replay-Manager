using ReplayManager.Domain.Enums;

namespace ReplayManager.Application.Models.Filters;

/// <summary>
/// Resultado detalhado da avaliação de filtros para um vídeo.
/// </summary>
public class FilterEvaluationResult
{
    /// <summary>
    /// Se o vídeo atende integralmente a todas as regras da pasta.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Status enumerado correspondente ao resultado da avaliação.
    /// </summary>
    public FilterEvaluationStatus Status { get; set; }

    /// <summary>
    /// Motivo amigável e detalhado em caso de reprovação (ou mensagem de aprovação).
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Lista de critérios individuais que falharam durante a avaliação.
    /// </summary>
    public IReadOnlyList<string> FailedCriteria { get; set; } = Array.Empty<string>();

    public static FilterEvaluationResult Approved() => new()
    {
        IsApproved = true,
        Status = FilterEvaluationStatus.Approved,
        Reason = "Aprovado em todos os critérios de filtro."
    };

    public static FilterEvaluationResult Rejected(
        FilterEvaluationStatus status,
        string reason,
        IReadOnlyList<string>? failedCriteria = null) => new()
    {
        IsApproved = false,
        Status = status,
        Reason = reason,
        FailedCriteria = failedCriteria ?? new[] { reason }
    };
}

