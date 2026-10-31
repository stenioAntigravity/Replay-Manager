namespace ReplayManager.Application.Models.Media;

/// <summary>
/// Resultado da geração de miniatura (thumbnail) a partir de um vídeo.
/// </summary>
public class ThumbnailResult
{
    /// <summary>
    /// Indica se a miniatura foi gerada com sucesso.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Caminho absoluto do arquivo de miniatura gerado.
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Timestamp em segundos no qual o frame foi capturado.
    /// </summary>
    public double TimestampSeconds { get; set; }

    /// <summary>
    /// Mensagem de erro em caso de falha.
    /// </summary>
    public string? Error { get; set; }

    public static ThumbnailResult Succeeded(string outputPath, double timestamp) =>
        new() { Success = true, OutputPath = outputPath, TimestampSeconds = timestamp };

    public static ThumbnailResult Failed(string error) =>
        new() { Success = false, Error = error };
}

