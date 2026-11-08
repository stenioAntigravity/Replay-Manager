using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.FileSystem;

namespace ReplayManager.Infrastructure.FileSystem;

/// <summary>
/// Implementação de alta performance e baixo consumo de memória para cálculo de SHA-256
/// utilizando streaming assíncrono nativo do .NET.
/// </summary>
public class Sha256ContentHasher : IContentHasher
{
    private const int StreamBufferSize = 64 * 1024; // 64 KB
    private readonly ILogger<Sha256ContentHasher>? _logger;

    public Sha256ContentHasher(ILogger<Sha256ContentHasher>? logger = null)
    {
        _logger = logger;
    }

    public async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Arquivo não encontrado para cálculo de hash: {filePath}", filePath);
        }

        try
        {
            await using var fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: StreamBufferSize,
                useAsync: true);

            return await ComputeSha256Async(fileStream, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Falha ao calcular hash SHA-256 do arquivo '{FilePath}'", filePath);
            throw;
        }
    }

    public async Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hashBytes);
    }
}

