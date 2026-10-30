using ReplayManager.Domain.Common;

namespace ReplayManager.Domain.Entities;

/// <summary>
/// Representa uma conta do YouTube autenticada via OAuth 2.0.
/// </summary>
public class YouTubeAccount : BaseEntity
{
    /// <summary>
    /// ID exclusivo do canal do YouTube (ex: "UC...").
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Título ou nome exibido do canal.
    /// </summary>
    public string ChannelTitle { get; set; } = string.Empty;

    /// <summary>
    /// URL do avatar ou thumbnail do canal.
    /// </summary>
    public string? ChannelThumbnailUrl { get; set; }

    /// <summary>
    /// Token de acesso e refresh token serializados e cifrados via Windows DPAPI (ProtectedData).
    /// Nunca armazenado em texto puro.
    /// </summary>
    public string EncryptedAuthData { get; set; } = string.Empty;

    /// <summary>
    /// Indica se esta é a conta ativa selecionada para uploads.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Data/hora UTC da última autenticação ou renovação bem-sucedida de token.
    /// </summary>
    public DateTime LastAuthenticatedAtUtc { get; set; } = DateTime.UtcNow;
}

