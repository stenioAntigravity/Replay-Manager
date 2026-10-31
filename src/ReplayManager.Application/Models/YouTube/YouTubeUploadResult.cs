namespace ReplayManager.Application.Models.YouTube;

/// <summary>
/// Resultado da operação de upload de vídeo para o YouTube.
/// </summary>
public class YouTubeUploadResult
{
    public bool Success { get; set; }
    public string? VideoId { get; set; }
    public string? YouTubeUrl => !string.IsNullOrWhiteSpace(VideoId) ? $"https://youtu.be/{VideoId}" : null;
    public string? ErrorMessage { get; set; }

    public static YouTubeUploadResult Succeeded(string videoId) => new() { Success = true, VideoId = videoId };
    public static YouTubeUploadResult Failed(string error) => new() { Success = false, ErrorMessage = error };
}

