using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.Filters;
using ReplayManager.Application.Models.Filters;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.Application.Services;

/// <summary>
/// Implementação pura e determinística do motor de avaliação de filtros.
/// Valida extensões, tamanho min/max, duração min/max, expressões regulares no nome e subdiretórios.
/// </summary>
public class FilterEvaluator : IFilterEvaluator
{
    private static readonly string[] DefaultAllowedExtensions = [".mp4", ".mkv", ".avi"];
    private readonly ILogger<FilterEvaluator>? _logger;

    public FilterEvaluator(ILogger<FilterEvaluator>? logger = null)
    {
        _logger = logger;
    }

    public FilterEvaluationResult Evaluate(VideoRecord video, FolderFilterRule? rule, string? folderPath = null)
    {
        ArgumentNullException.ThrowIfNull(video);

        var failed = new List<string>();

        // 1. Validação de Extensão
        var extension = Path.GetExtension(video.FileName);
        var allowedExtensions = GetAllowedExtensions(rule);

        if (string.IsNullOrWhiteSpace(extension) ||
            !allowedExtensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
        {
            var msg = $"Extensão '{extension}' não permitida. Válidas: {string.Join(", ", allowedExtensions)}.";
            _logger?.LogDebug("Vídeo {FileName} reprovado: {Msg}", video.FileName, msg);
            return FilterEvaluationResult.Rejected(FilterEvaluationStatus.RejectedByExtension, msg);
        }

        // Se não houver regra customizada, a extensão padrão aprovou o arquivo
        if (rule == null)
        {
            return FilterEvaluationResult.Approved();
        }

        // 2. Validação de Subpastas
        if (!rule.IncludeSubdirectories && !string.IsNullOrWhiteSpace(folderPath))
        {
            var videoDir = Path.GetDirectoryName(Path.GetFullPath(video.FilePath));
            var baseDir = Path.GetFullPath(folderPath).TrimEnd('\\', '/');

            if (!string.Equals(videoDir, baseDir, StringComparison.OrdinalIgnoreCase))
            {
                var msg = "O arquivo está em uma subpasta e a pasta monitorada não permite subdiretórios.";
                _logger?.LogDebug("Vídeo {FileName} reprovado: {Msg}", video.FileName, msg);
                return FilterEvaluationResult.Rejected(FilterEvaluationStatus.RejectedByNamePattern, msg);
            }
        }

        // 3. Validação de Tamanho Mínimo
        if (rule.MinSizeBytes > 0 && video.FileSizeBytes < rule.MinSizeBytes)
        {
            var msg = $"Tamanho de {FormatBytes(video.FileSizeBytes)} é inferior ao mínimo de {FormatBytes(rule.MinSizeBytes)}.";
            _logger?.LogDebug("Vídeo {FileName} reprovado: {Msg}", video.FileName, msg);
            return FilterEvaluationResult.Rejected(FilterEvaluationStatus.RejectedByMinSize, msg);
        }

        // 4. Validação de Tamanho Máximo (se ativado)
        if (rule.IsMaxSizeEnabled && rule.MaxSizeBytes.HasValue && rule.MaxSizeBytes.Value > 0)
        {
            if (video.FileSizeBytes > rule.MaxSizeBytes.Value)
            {
                var msg = $"Tamanho de {FormatBytes(video.FileSizeBytes)} excede o máximo de {FormatBytes(rule.MaxSizeBytes.Value)}.";
                _logger?.LogDebug("Vídeo {FileName} reprovado: {Msg}", video.FileName, msg);
                return FilterEvaluationResult.Rejected(FilterEvaluationStatus.RejectedByMaxSize, msg);
            }
        }

        // 5. Validação de Duração Mínima
        if (rule.MinDurationSeconds.HasValue && rule.MinDurationSeconds.Value > 0)
        {
            if (video.DurationSeconds.HasValue && video.DurationSeconds.Value < rule.MinDurationSeconds.Value)
            {
                var msg = $"Duração de {video.DurationSeconds.Value:F1}s é inferior à duração mínima de {rule.MinDurationSeconds.Value}s.";
                _logger?.LogDebug("Vídeo {FileName} reprovado: {Msg}", video.FileName, msg);
                return FilterEvaluationResult.Rejected(FilterEvaluationStatus.RejectedByMinDuration, msg);
            }
        }

        // 6. Validação de Duração Máxima
        if (rule.MaxDurationSeconds.HasValue && rule.MaxDurationSeconds.Value > 0)
        {
            if (video.DurationSeconds.HasValue && video.DurationSeconds.Value > rule.MaxDurationSeconds.Value)
            {
                var msg = $"Duração de {video.DurationSeconds.Value:F1}s excede a duração máxima de {rule.MaxDurationSeconds.Value}s.";
                _logger?.LogDebug("Vídeo {FileName} reprovado: {Msg}", video.FileName, msg);
                return FilterEvaluationResult.Rejected(FilterEvaluationStatus.RejectedByMaxDuration, msg);
            }
        }

        // 7. Validação de Padrão de Nome (Regex)
        if (!string.IsNullOrWhiteSpace(rule.NamePatternRegex))
        {
            try
            {
                if (!Regex.IsMatch(video.FileName, rule.NamePatternRegex, RegexOptions.IgnoreCase))
                {
                    var msg = $"O nome do arquivo não corresponde ao padrão regex '{rule.NamePatternRegex}'.";
                    _logger?.LogDebug("Vídeo {FileName} reprovado: {Msg}", video.FileName, msg);
                    return FilterEvaluationResult.Rejected(FilterEvaluationStatus.RejectedByNamePattern, msg);
                }
            }
            catch (ArgumentException ex)
            {
                _logger?.LogWarning(ex, "Regex de filtro inválido: '{Regex}'", rule.NamePatternRegex);
            }
        }

        return FilterEvaluationResult.Approved();
    }

    private static string[] GetAllowedExtensions(FolderFilterRule? rule)
    {
        if (rule == null || string.IsNullOrWhiteSpace(rule.AllowedExtensions))
            return DefaultAllowedExtensions;

        return rule.AllowedExtensions
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ext => ext.StartsWith('.') ? ext : $".{ext}")
            .ToArray();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}

