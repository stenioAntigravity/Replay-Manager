namespace ReplayManager.Application.Models.Media;

/// <summary>
/// Metadados técnicos extraídos de um arquivo de vídeo via FFprobe.
/// </summary>
public class VideoMetadata
{
    /// <summary>
    /// Duração total do vídeo em segundos.
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Largura do vídeo em pixels (ex: 1920).
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Altura do vídeo em pixels (ex: 1080).
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Codec de vídeo utilizado (ex: "h264", "hevc", "vp9").
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Bitrate total em bits por segundo.
    /// </summary>
    public long? BitrateBps { get; set; }

    /// <summary>
    /// Taxa de quadros por segundo (ex: 30.0, 59.94, 60.0).
    /// </summary>
    public double? FrameRate { get; set; }

    /// <summary>
    /// Representação legível da resolução (ex: "1920x1080").
    /// </summary>
    public string Resolution => $"{Width}x{Height}";
}

