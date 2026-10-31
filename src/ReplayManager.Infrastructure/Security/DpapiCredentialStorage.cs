using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ReplayManager.Infrastructure.Security;

/// <summary>
/// Armazenamento e proteção de segredos e credenciais locais utilizando Windows DPAPI (Data Protection API).
/// Garante que somente a conta do usuário atual do Windows consiga descriptografar os tokens.
/// </summary>
public class DpapiCredentialStorage
{
    private readonly string _storageDirectory;
    private readonly ILogger<DpapiCredentialStorage>? _logger;

    public DpapiCredentialStorage(
        string? storageDirectory = null,
        ILogger<DpapiCredentialStorage>? logger = null)
    {
        _logger = logger;
        _storageDirectory = storageDirectory ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ReplayManager",
                "Credentials");

        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public async Task SaveSecretAsync(string key, string secret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(secret);

        var filePath = GetFilePath(key);
        var plainBytes = Encoding.UTF8.GetBytes(secret);

        // Criptografia atrelada ao usuário atual do Windows
        var encryptedBytes = ProtectedData.Protect(
            plainBytes,
            optionalEntropy: null,
            DataProtectionScope.CurrentUser);

        await File.WriteAllBytesAsync(filePath, encryptedBytes, cancellationToken);
        _logger?.LogDebug("Segredo '{Key}' salvo e protegido com DPAPI.", key);
    }

    public async Task<string?> ReadSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
            return null;

        try
        {
            var encryptedBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var plainBytes = ProtectedData.Unprotect(
                encryptedBytes,
                optionalEntropy: null,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException ex)
        {
            _logger?.LogWarning(ex, "Falha ao descriptografar segredo '{Key}' via DPAPI.", key);
            return null;
        }
    }

    public Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger?.LogDebug("Segredo '{Key}' removido com sucesso.", key);
        }

        return Task.CompletedTask;
    }

    private string GetFilePath(string key)
    {
        var safeKey = string.Concat(key.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_storageDirectory, $"{safeKey}.dat");
    }
}

