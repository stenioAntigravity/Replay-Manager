using ReplayManager.Application.Models.Filters;
using ReplayManager.Domain.Entities;

namespace ReplayManager.Application.Contracts.Filters;

/// <summary>
/// Contrato do motor de avaliação de filtros para pastas monitoradas.
/// </summary>
public interface IFilterEvaluator
{
    /// <summary>
    /// Avalia um vídeo com base nas regras de filtro configuradas para a pasta.
    /// Se rule for nulo, aplica as validações de extensões padrões permitidas.
    /// </summary>
    /// <param name="video">Vídeo a ser avaliado.</param>
    /// <param name="rule">Regra de filtro da pasta (opcional).</param>
    /// <param name="folderPath">Caminho base da pasta monitorada (para verificar subpastas).</param>
    /// <returns>Resultado da avaliação com status e justificativa.</returns>
    FilterEvaluationResult Evaluate(VideoRecord video, FolderFilterRule? rule, string? folderPath = null);
}

