using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.Media;
using ReplayManager.Application.Models.Media;

namespace ReplayManager.Infrastructure.FFmpeg;

/// <summary>
/// Gera miniaturas de vídeo capturando um frame via FFmpeg.
/// O timestamp padrão é 15% da duração para evitar telas pretas de início de gravação.
/// </summary>
public class FFmpegThumbnailGenerator : IThumbnailGenerator
{
    private readonly IFFmpegPathResolver _pathResolver;
    private readonly ILogger<FFmpegThumbnailGenerator>? _logger;

    public FFmpegThumbnailGenerator(
        IFFmpegPathResolver pathResolver,
        ILogger<FFmpegThumbnailGenerator>? logger = null)
    {
        _pathResolver = pathResolver;
        _logger = logger;
    }

    public async Task<ThumbnailResult> GenerateThumbnailAsync(
        string videoFilePath,
        string outputPath,
        double? timestampSeconds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        if (!File.Exists(videoFilePath))
        {
            return ThumbnailResult.Failed($"Arquivo de vídeo não encontrado: {videoFilePath}");
        }

        var ffmpegPath = _pathResolver.ResolveFFmpegPath();
        if (ffmpegPath == null)
        {
            _logger?.LogWarning("FFmpeg não disponível. Miniatura não será gerada para '{Path}'", videoFilePath);
            return ThumbnailResult.Failed("FFmpeg não encontrado no sistema.");
        }

        try
        {
            // Garantir que o diretório de saída exista
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var ts = timestampSeconds ?? 1.0; // fallback conservador se não fornecido
            var tsFormatted = ts.ToString("F3", CultureInfo.InvariantCulture);

            // -ss antes de -i para seek rápido por keyframe
            // -vframes 1 captura exatamente 1 frame
            // -q:v 2 mantém qualidade alta na saída JPEG
            // -y sobrescreve arquivo existente
            var arguments = $"-ss {tsFormatted} -i \"{videoFilePath}\" -vframes 1 -q:v 2 -y \"{outputPath}\"";

            _logger?.LogDebug("Executando FFmpeg para thumbnail: '{Exe}' {Args}", ffmpegPath, arguments);

            var (exitCode, stderr) = await RunProcessAsync(ffmpegPath, arguments, cancellationToken);

            if (exitCode != 0)
            {
                _logger?.LogWarning("FFmpeg retornou código de saída {Code} ao gerar thumbnail de '{Path}'. Stderr: {Err}",
                    exitCode, videoFilePath, stderr);
                return ThumbnailResult.Failed($"FFmpeg retornou código {exitCode}. {stderr}");
            }

            if (!File.Exists(outputPath))
            {
                return ThumbnailResult.Failed("FFmpeg concluiu sem erros, mas o arquivo de miniatura não foi gerado.");
            }

            var fileInfo = new FileInfo(outputPath);
            if (fileInfo.Length == 0)
            {
                File.Delete(outputPath);
                return ThumbnailResult.Failed("Arquivo de miniatura gerado com 0 bytes.");
            }

            _logger?.LogInformation("Thumbnail gerada com sucesso: '{OutputPath}' (timestamp={Ts}s, {Size} bytes)",
                outputPath, ts, fileInfo.Length);

            return ThumbnailResult.Succeeded(outputPath, ts);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Erro ao gerar miniatura para '{Path}'", videoFilePath);
            return ThumbnailResult.Failed(ex.Message);
        }
    }

    public double CalculateThumbnailTimestamp(double durationSeconds, double percentage = 0.15)
    {
        if (durationSeconds <= 0) return 0;

        percentage = Math.Clamp(percentage, 0.01, 0.99);

        var timestamp = durationSeconds * percentage;

        // Para vídeos muito curtos (< 3s), capturar no meio
        if (durationSeconds < 3.0)
        {
            timestamp = durationSeconds / 2.0;
        }

        return Math.Round(timestamp, 3);
    }

    public async Task<string> ReplaceThumbnailAsync(
        string sourceImagePath,
        Guid videoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceImagePath);

        if (!File.Exists(sourceImagePath))
        {
            throw new FileNotFoundException($"Imagem de origem não encontrada: '{sourceImagePath}'", sourceImagePath);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var targetDir = Path.Combine(localAppData, "ReplayManager", "Thumbnails");
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var ext = Path.GetExtension(sourceImagePath);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
        var targetPath = Path.Combine(targetDir, $"{videoId}{ext.ToLowerInvariant()}");

        await using (var sourceStream = new FileStream(sourceImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
        await using (var destinationStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await sourceStream.CopyToAsync(destinationStream, cancellationToken);
        }

        _logger?.LogInformation("Thumbnail manual copiada para o vídeo {VideoId}: '{Path}'", videoId, targetPath);
        return targetPath;
    }

    private static async Task<(int ExitCode, string Stderr)> RunProcessAsync(
        string exePath,
        string arguments,
        CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
        };

        process.Start();

        // FFmpeg escreve progresso no stderr, não no stdout
        _ = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, stderr);
    }
}

