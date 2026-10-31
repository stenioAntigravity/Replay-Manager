using ReplayManager.Application.Models.YouTube;

namespace ReplayManager.Application.Contracts.YouTube;

/// <summary>
/// Contrato do serviço de autenticação com o YouTube API v3 via OAuth 2.0.
/// </summary>
public interface IYouTubeAuthService
{
    /// <summary>
    /// Verifica se existe credencial ativa e válida salva localmente.
    /// </summary>
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia o fluxo de autorização OAuth 2.0 (GoogleWebAuthorizationBroker).
    /// </summary>
    Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o perfil e status do canal da conta conectada.
    /// </summary>
    Task<YouTubeChannelProfile> GetChannelProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Desconecta a conta e remove com segurança os tokens salvos.
    /// </summary>
    Task SignOutAsync(CancellationToken cancellationToken = default);
}

