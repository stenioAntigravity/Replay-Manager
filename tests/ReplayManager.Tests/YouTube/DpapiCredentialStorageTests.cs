using FluentAssertions;
using ReplayManager.Infrastructure.Security;

namespace ReplayManager.Tests.YouTube;

public class DpapiCredentialStorageTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DpapiCredentialStorage _storage;

    public DpapiCredentialStorageTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ReplayManager_DpapiTests_" + Guid.NewGuid().ToString("N"));
        _storage = new DpapiCredentialStorage(_tempDir);
    }

    [Fact]
    public async Task SaveSecretAsync_AndReadSecretAsync_EncryptsAndDecryptsSuccessfully()
    {
        const string secretKey = "GoogleOAuthToken";
        const string secretValue = "{\"access_token\":\"ya29.a0AfH6SM...\",\"refresh_token\":\"1//0g...\"}";

        // Act
        await _storage.SaveSecretAsync(secretKey, secretValue);
        var retrieved = await _storage.ReadSecretAsync(secretKey);

        // Assert
        retrieved.Should().Be(secretValue);

        // Verificar que o arquivo no disco não está em texto plano
        var rawBytes = await File.ReadAllBytesAsync(Path.Combine(_tempDir, $"{secretKey}.dat"));
        System.Text.Encoding.UTF8.GetString(rawBytes).Should().NotContain("ya29.a0AfH6SM");
    }

    [Fact]
    public async Task DeleteSecretAsync_RemovesFileFromDisk()
    {
        const string key = "TemporarySecret";
        await _storage.SaveSecretAsync(key, "data");

        await _storage.DeleteSecretAsync(key);

        var retrieved = await _storage.ReadSecretAsync(key);
        retrieved.Should().BeNull();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch { }
    }
}

