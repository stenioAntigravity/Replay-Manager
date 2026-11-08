using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.YouTube;
using ReplayManager.Application.Models.YouTube;
using ReplayManager.Infrastructure.Security;

namespace ReplayManager.Infrastructure.YouTube;

/// <summary>
/// Implementação do serviço de autenticação OAuth 2.0 com o YouTube.
/// </summary>
public class YouTubeAuthService : IYouTubeAuthService
{
    private static readonly string[] Scopes =
    [
        YouTubeService.Scope.YoutubeUpload,
        YouTubeService.Scope.YoutubeReadonly
    ];

    private readonly DpapiCredentialStorage _dpapiStorage;
    private readonly ILogger<YouTubeAuthService>? _logger;
    private readonly string _tokenDirectory;

    public YouTubeAuthService(
        DpapiCredentialStorage dpapiStorage,
        ILogger<YouTubeAuthService>? logger = null,
        string? tokenDirectory = null)
    {
        _dpapiStorage = dpapiStorage;
        _logger = logger;
        _tokenDirectory = tokenDirectory ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ReplayManager",
                "GoogleTokens");

        if (!Directory.Exists(_tokenDirectory))
        {
            Directory.CreateDirectory(_tokenDirectory);
        }
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var credential = await GetUserCredentialAsync(cancellationToken);
        if (credential == null) return false;

        // Se o token estiver expirado, tentar renovar
        if (credential.Token.IsStale)
        {
            try
            {
                var refreshed = await credential.RefreshTokenAsync(cancellationToken);
                return refreshed;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Falha ao renovar token OAuth do YouTube.");
                return false;
            }
        }

        return true;
    }

    public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var secrets = await GetClientSecretsAsync(cancellationToken);
            if (secrets == null)
            {
                _logger?.LogError("ClientSecrets do Google OAuth não configurados.");
                return false;
            }

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                Scopes,
                "user",
                cancellationToken,
                new FileDataStore(_tokenDirectory, true));

            return credential != null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Erro no fluxo de autenticação do YouTube.");
            return false;
        }
    }

    public async Task<YouTubeChannelProfile> GetChannelProfileAsync(CancellationToken cancellationToken = default)
    {
        var credential = await GetUserCredentialAsync(cancellationToken);
        if (credential == null)
        {
            return YouTubeChannelProfile.Unauthenticated();
        }

        try
        {
            using var youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "ReplayManager"
            });

            var request = youtubeService.Channels.List("snippet");
            request.Mine = true;

            var response = await request.ExecuteAsync(cancellationToken);
            var channel = response.Items?.FirstOrDefault();

            if (channel != null)
            {
                return new YouTubeChannelProfile
                {
                    IsAuthenticated = true,
                    ChannelId = channel.Id,
                    ChannelTitle = channel.Snippet.Title,
                    ThumbnailUrl = channel.Snippet.Thumbnails?.Default__?.Url
                };
            }

            return new YouTubeChannelProfile { IsAuthenticated = true };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Erro ao obter perfil do canal do YouTube.");
            return YouTubeChannelProfile.Unauthenticated();
        }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (Directory.Exists(_tokenDirectory))
            {
                Directory.Delete(_tokenDirectory, true);
            }
            await _dpapiStorage.DeleteSecretAsync("GoogleClientSecrets", cancellationToken);
            _logger?.LogInformation("Sessão do YouTube encerrada e tokens excluídos.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao encerrar sessão do YouTube.");
        }
    }

    public async Task<UserCredential?> GetUserCredentialAsync(CancellationToken cancellationToken = default)
    {
        // Se a pasta de tokens não contém token persistido, não dispara AuthorizeAsync para evitar abrir o navegador automaticamente
        var tokenFile = Path.Combine(_tokenDirectory, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user");
        if (!File.Exists(tokenFile))
        {
            return null;
        }

        var secrets = await GetClientSecretsAsync(cancellationToken);
        if (secrets == null) return null;

        try
        {
            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                Scopes,
                "user",
                cancellationToken,
                new FileDataStore(_tokenDirectory, true));
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Não foi possível carregar credencial salva.");
            return null;
        }
    }

    private async Task<ClientSecrets?> GetClientSecretsAsync(CancellationToken cancellationToken)
    {
        // 1. Tentar ler do armazenamento DPAPI seguro
        var json = await _dpapiStorage.ReadSecretAsync("GoogleClientSecrets", cancellationToken);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                return GoogleClientSecrets.FromStream(stream).Secrets;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Erro ao ler GoogleClientSecrets protegido via DPAPI.");
            }
        }

        // 2. Fallback: procurar arquivo credentials.json ou client_secret.json local
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(appDir, "client_secrets.json"),
            Path.Combine(appDir, "credentials.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReplayManager", "credentials.json")
        };

        foreach (var file in candidates)
        {
            if (File.Exists(file))
            {
                using var stream = File.OpenRead(file);
                return GoogleClientSecrets.FromStream(stream).Secrets;
            }
        }

        return null;
    }
}

