using System.Text.Json.Serialization;

namespace ReplayManager.Infrastructure.FFmpeg;

/// <summary>
/// Estruturas de dados para desserialização da saída JSON do FFprobe.
/// Usadas internamente pelo FFprobeMetadataExtractor.
/// </summary>
internal class FFprobeOutput
{
    [JsonPropertyName("format")]
    public FFprobeFormat? Format { get; set; }

    [JsonPropertyName("streams")]
    public List<FFprobeStream>? Streams { get; set; }
}

internal class FFprobeFormat
{
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("bit_rate")]
    public string? BitRate { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }

    [JsonPropertyName("format_name")]
    public string? FormatName { get; set; }
}

internal class FFprobeStream
{
    [JsonPropertyName("codec_type")]
    public string? CodecType { get; set; }

    [JsonPropertyName("codec_name")]
    public string? CodecName { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("r_frame_rate")]
    public string? RFrameRate { get; set; }

    [JsonPropertyName("avg_frame_rate")]
    public string? AvgFrameRate { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("bit_rate")]
    public string? BitRate { get; set; }
}

