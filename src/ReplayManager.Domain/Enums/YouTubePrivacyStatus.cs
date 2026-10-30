namespace ReplayManager.Domain.Enums;

/// <summary>
/// Níveis de visibilidade de um vídeo publicado no YouTube.
/// </summary>
public enum YouTubePrivacyStatus
{
    /// <summary>
    /// Padrão da aplicação: acessível apenas via link direto, não listado nas buscas públicas.
    /// </summary>
    Unlisted = 0,

    /// <summary>
    /// Visível exclusivamente para a conta do proprietário.
    /// </summary>
    Private = 1,

    /// <summary>
    /// Visível publicamente para qualquer usuário e indexado nas buscas do YouTube.
    /// </summary>
    Public = 2
}
