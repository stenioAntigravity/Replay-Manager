namespace ReplayManager.Application.Models.YouTube;

/// <summary>
/// Informações do canal associado à conta autenticada do YouTube.
/// </summary>
public class YouTubeChannelProfile
{
    public bool IsAuthenticated { get; set; }
    public string? ChannelId { get; set; }
    public string? ChannelTitle { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AccountEmail { get; set; }

    public static YouTubeChannelProfile Unauthenticated() => new() { IsAuthenticated = false };
}

