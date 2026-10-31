using System.Globalization;
using ReplayManager.Application.Contracts.YouTube;
using ReplayManager.Domain.Entities;

namespace ReplayManager.Application.Services;

/// <summary>
/// Motor de renderização de descrições dinâmicas para vídeos do YouTube.
/// </summary>
public class DescriptionTemplateEngine : IDescriptionTemplateEngine
{
    public string RenderDescription(string template, VideoRecord video, string? folderName = null)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        ArgumentNullException.ThrowIfNull(video);

        var date = (video.RecordedAtUtc ?? video.CreatedAtUtc).ToLocalTime();

        var durationStr = "--:--";
        if (video.DurationSeconds.HasValue && video.DurationSeconds.Value > 0)
        {
            var t = TimeSpan.FromSeconds(video.DurationSeconds.Value);
            durationStr = t.TotalHours >= 1
                ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
                : $"{t.Minutes:D2}:{t.Seconds:D2}";
        }

        var resolutionStr = video.ResolutionWidth.HasValue && video.ResolutionHeight.HasValue
            ? $"{video.ResolutionWidth}x{video.ResolutionHeight}"
            : "Desconhecida";

        var fileSizeStr = FormatFileSize(video.FileSizeBytes);

        return template
            .Replace("{FileName}", video.FileName, StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", date.ToString("HH:mm:ss", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{DateTime}", date.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{Duration}", durationStr, StringComparison.OrdinalIgnoreCase)
            .Replace("{Resolution}", resolutionStr, StringComparison.OrdinalIgnoreCase)
            .Replace("{FileSize}", fileSizeStr, StringComparison.OrdinalIgnoreCase)
            .Replace("{FolderName}", folderName ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024)
            return $"{(bytes / (1024.0 * 1024)).ToString("F1", CultureInfo.InvariantCulture)} MB";
        return $"{(bytes / (1024.0 * 1024 * 1024)).ToString("F2", CultureInfo.InvariantCulture)} GB";
    }
}

