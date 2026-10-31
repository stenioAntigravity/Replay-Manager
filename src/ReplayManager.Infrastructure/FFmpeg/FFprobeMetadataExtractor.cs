using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.Media;
using ReplayManager.Application.Models.Media;

namespace ReplayManager.Infrastructure.FFmpeg;

/// <summary>
/// Extrai metadados técnicos de arquivos de vídeo utilizando FFprobe com saída JSON.
/// </summary>
public class FFprobeMetadataExtractor : IMediaMetadataExtractor
{
    private readonly IFFmpegPathResolver _pathResolver;
    private readonly ILogger<FFprobeMetadataExtractor>? _logger;

    public FFprobeMetadataExtractor(
        IFFmpegPathResolver pathResolver,
        ILogger<FFprobeMetadataExtractor>? logger = null)
    {
        _pathResolver = pathResolver;
        _logger = logger;
    }

    public async Task<VideoMetadata?> ExtractMetadataAsync(
        string videoFilePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoFilePath);

        if (!File.Exists(videoFilePath))
        {
            _logger?.LogWarning("Arquivo de vídeo não encontrado para extração de metadados: '{Path}'", videoFilePath);
            return null;
        }

        var ffprobePath = _pathResolver.ResolveFFprobePath();
        if (ffprobePath == null)
        {
            _logger?.LogWarning("FFprobe não disponível no sistema. Metadados não serão extraídos.");
            return null;
        }

        try
        {
            var arguments = $"-v quiet -print_format json -show_format -show_streams \"{videoFilePath}\"";

            _logger?.LogDebug("Executando FFprobe: '{Exe}' {Args}", ffprobePath, arguments);

            var jsonOutput = await RunProcessAsync(ffprobePath, arguments, cancellationToken);

            if (string.IsNullOrWhiteSpace(jsonOutput))
            {
                _logger?.LogWarning("FFprobe retornou saída vazia para '{Path}'", videoFilePath);
                return null;
            }

            return ParseFFprobeJson(jsonOutput, videoFilePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Erro ao extrair metadados de '{Path}' via FFprobe", videoFilePath);
            return null;
        }
    }

    /// <summary>
    /// Desserializa e interpreta a saída JSON do FFprobe, extraindo os metadados relevantes.
    /// Visibilidade interna para permitir testes unitários do parser.
    /// </summary>
    internal VideoMetadata? ParseFFprobeJson(string json, string sourceFile = "")
    {
        FFprobeOutput? probeOutput;
        try
        {
            probeOutput = JsonSerializer.Deserialize<FFprobeOutput>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex, "Falha ao desserializar JSON do FFprobe para '{Path}'", sourceFile);
            return null;
        }

        if (probeOutput == null)
        {
            return null;
        }

        // Encontrar o primeiro stream de vídeo
        var videoStream = probeOutput.Streams?.FirstOrDefault(s =>
            string.Equals(s.CodecType, "video", StringComparison.OrdinalIgnoreCase));

        var metadata = new VideoMetadata();

        // Duração: preferir format.duration, fallback para stream.duration
        if (TryParseDuration(probeOutput.Format?.Duration, out var formatDuration))
        {
            metadata.DurationSeconds = formatDuration;
        }
        else if (videoStream != null && TryParseDuration(videoStream.Duration, out var streamDuration))
        {
            metadata.DurationSeconds = streamDuration;
        }

        // Resolução
        if (videoStream is { Width: > 0, Height: > 0 })
        {
            metadata.Width = videoStream.Width.Value;
            metadata.Height = videoStream.Height.Value;
        }

        // Codec de vídeo
        metadata.VideoCodec = videoStream?.CodecName;

        // Bitrate: preferir format.bit_rate, fallback para stream.bit_rate
        if (TryParseLong(probeOutput.Format?.BitRate, out var formatBitrate))
        {
            metadata.BitrateBps = formatBitrate;
        }
        else if (videoStream != null && TryParseLong(videoStream.BitRate, out var streamBitrate))
        {
            metadata.BitrateBps = streamBitrate;
        }

        // Frame rate
        if (TryParseFrameRate(videoStream?.RFrameRate, out var fps))
        {
            metadata.FrameRate = fps;
        }
        else if (TryParseFrameRate(videoStream?.AvgFrameRate, out var avgFps))
        {
            metadata.FrameRate = avgFps;
        }

        _logger?.LogDebug("Metadados extraídos de '{Path}': {Dur}s, {Res}, codec={Codec}, bitrate={Br}",
            sourceFile, metadata.DurationSeconds, metadata.Resolution, metadata.VideoCodec, metadata.BitrateBps);

        return metadata;
    }

    private static bool TryParseDuration(string? value, out double result)
    {
        result = 0;
        return !string.IsNullOrWhiteSpace(value) &&
               double.TryParse(value, CultureInfo.InvariantCulture, out result) &&
               result > 0;
    }

    private static bool TryParseLong(string? value, out long result)
    {
        result = 0;
        return !string.IsNullOrWhiteSpace(value) &&
               long.TryParse(value, CultureInfo.InvariantCulture, out result);
    }

    internal static bool TryParseFrameRate(string? value, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;

        // Formato fracionário: "30000/1001" ou "60/1"
        if (value.Contains('/'))
        {
            var parts = value.Split('/');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], CultureInfo.InvariantCulture, out var numerator) &&
                double.TryParse(parts[1], CultureInfo.InvariantCulture, out var denominator) &&
                denominator > 0)
            {
                result = numerator / denominator;
                return result > 0;
            }

            return false;
        }

        // Formato direto: "29.97"
        return double.TryParse(value, CultureInfo.InvariantCulture, out result) && result > 0;
    }

    private static async Task<string> RunProcessAsync(
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

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        // Consumir stderr para evitar deadlock, mas não usamos
        _ = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return stdout;
    }
}

