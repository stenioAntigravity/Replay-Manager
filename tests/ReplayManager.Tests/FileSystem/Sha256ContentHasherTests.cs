using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using ReplayManager.Infrastructure.FileSystem;

namespace ReplayManager.Tests.FileSystem;

public class Sha256ContentHasherTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Sha256ContentHasher _hasher;

    public Sha256ContentHasherTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ReplayManager_HasherTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _hasher = new Sha256ContentHasher();
    }

    [Fact]
    public async Task ComputeSha256Async_WithKnownContent_ReturnsCorrectHex()
    {
        // Arrange
        const string content = "ReplayManager_Test_Payload_12345";
        var rawBytes = Encoding.UTF8.GetBytes(content);
        var expectedBytes = SHA256.HashData(rawBytes);
        var expectedHex = Convert.ToHexStringLower(expectedBytes);

        var filePath = Path.Combine(_tempDir, "sample.txt");
        await File.WriteAllBytesAsync(filePath, rawBytes);

        // Act
        var resultHex = await _hasher.ComputeSha256Async(filePath);

        // Assert
        resultHex.Should().Be(expectedHex);
        resultHex.Length.Should().Be(64);
    }

    [Fact]
    public async Task ComputeSha256Async_StreamAndFilePath_ProduceIdenticalHash()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "binary.bin");
        var randomBytes = new byte[1024 * 64]; // 64 KB
        Random.Shared.NextBytes(randomBytes);
        await File.WriteAllBytesAsync(filePath, randomBytes);

        // Act
        var hashFromFile = await _hasher.ComputeSha256Async(filePath);
        using var stream = File.OpenRead(filePath);
        var hashFromStream = await _hasher.ComputeSha256Async(stream);

        // Assert
        hashFromFile.Should().Be(hashFromStream);
    }

    [Fact]
    public async Task ComputeSha256Async_FileNotFound_ThrowsFileNotFoundException()
    {
        var nonExistent = Path.Combine(_tempDir, "missing.bin");

        var act = async () => await _hasher.ComputeSha256Async(nonExistent);

        await act.Should().ThrowAsync<FileNotFoundException>();
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
        catch
        {
            // Limpeza de testes
        }
    }
}
