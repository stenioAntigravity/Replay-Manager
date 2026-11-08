using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.Export;
using ReplayManager.Infrastructure.Data;

namespace ReplayManager.Infrastructure.Export;

/// <summary>
/// Implementação do serviço de exportação de catálogo para formatos abertos (CSV e JSON).
/// </summary>
public class LibraryExportService : ILibraryExportService
{
    private readonly ReplayManagerDbContext _dbContext;
    private readonly ILogger<LibraryExportService>? _logger;

    public LibraryExportService(
        ReplayManagerDbContext dbContext,
        ILogger<LibraryExportService>? logger = null)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> ExportToCsvAsync(string destinationFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFilePath);

        var destinationDir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrWhiteSpace(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        var videos = await _dbContext.Videos
            .Include(v => v.MonitoredFolder)
            .AsNoTracking()
            .OrderByDescending(v => v.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        // Cabeçalho CSV
        sb.AppendLine("Id;FileName;RecordedAtUtc;DurationSeconds;FileSizeBytes;UploadStatus;LocalStatus;FilterStatus;YouTubeVideoId;YouTubeUrl;FolderPath");

        foreach (var v in videos)
        {
            var line = string.Join(";",
                EscapeCsv(v.Id.ToString()),
                EscapeCsv(v.FileName),
                v.RecordedAtUtc.HasValue ? v.RecordedAtUtc.Value.ToString("o", CultureInfo.InvariantCulture) : "",
                v.DurationSeconds.HasValue ? v.DurationSeconds.Value.ToString("F2", CultureInfo.InvariantCulture) : "",
                v.FileSizeBytes.ToString(CultureInfo.InvariantCulture),
                v.UploadStatus.ToString(),
                v.LocalStatus.ToString(),
                v.FilterStatus.ToString(),
                EscapeCsv(v.YouTubeVideoId ?? ""),
                EscapeCsv(v.YouTubeUrl ?? ""),
                EscapeCsv(v.MonitoredFolder?.Path ?? "")
            );
            sb.AppendLine(line);
        }

        await File.WriteAllTextAsync(destinationFilePath, sb.ToString(), Encoding.UTF8, cancellationToken);
        _logger?.LogInformation("Catálogo exportado para CSV com {Count} registros em '{Path}'", videos.Count, destinationFilePath);
        return destinationFilePath;
    }

    public async Task<string> ExportToJsonAsync(string destinationFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFilePath);

        var destinationDir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrWhiteSpace(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        var videos = await _dbContext.Videos
            .Include(v => v.MonitoredFolder)
            .AsNoTracking()
            .OrderByDescending(v => v.RecordedAtUtc)
            .Select(v => new
            {
                v.Id,
                v.FileName,
                v.FilePath,
                v.FileSizeBytes,
                v.DurationSeconds,
                v.ContentSha256,
                RecordedAtUtc = v.RecordedAtUtc.HasValue ? v.RecordedAtUtc.Value.ToString("o") : null,
                UploadStatus = v.UploadStatus.ToString(),
                LocalStatus = v.LocalStatus.ToString(),
                FilterStatus = v.FilterStatus.ToString(),
                v.CustomTitle,
                v.CustomDescription,
                v.YouTubeVideoId,
                v.YouTubeUrl,
                MonitoredFolder = v.MonitoredFolder != null ? v.MonitoredFolder.Name : null,
                MonitoredFolderPath = v.MonitoredFolder != null ? v.MonitoredFolder.Path : null
            })
            .ToListAsync(cancellationToken);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(videos, options);
        await File.WriteAllTextAsync(destinationFilePath, json, Encoding.UTF8, cancellationToken);

        _logger?.LogInformation("Catálogo exportado para JSON com {Count} registros em '{Path}'", videos.Count, destinationFilePath);
        return destinationFilePath;
    }

    private static string EscapeCsv(string field)
    {
        if (field.Contains(';') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
