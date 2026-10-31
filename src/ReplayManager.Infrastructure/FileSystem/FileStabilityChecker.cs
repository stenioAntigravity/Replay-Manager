using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.FileSystem;

namespace ReplayManager.Infrastructure.FileSystem;

/// <summary>
/// Componente de inspeção de estabilidade de arquivos.
/// Garante que softwares de gravação concluíram a escrita de dados e liberaram o lock de arquivo
/// antes de iniciar a extração de hash ou processamento por FFmpeg.
/// </summary>
public class FileStabilityChecker : IFileStabilityChecker
{
    private static readonly TimeSpan DefaultStabilityDuration = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMilliseconds(500);

    private readonly ILogger<FileStabilityChecker>? _logger;

    public FileStabilityChecker(ILogger<FileStabilityChecker>? logger = null)
    {
        _logger = logger;
    }

    public async Task<bool> WaitForStabilityAsync(
        string filePath,
        TimeSpan? stabilityDuration = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var requiredStability = stabilityDuration ?? DefaultStabilityDuration;
        var maxWaitTimeout = timeout ?? DefaultTimeout;

        var startTime = DateTime.UtcNow;
        var lastChangeTime = DateTime.UtcNow;
        long lastKnownSize = -1;

        _logger?.LogDebug("Iniciando verificação de estabilidade para '{FilePath}'. Duração requerida: {Req}s, Timeout: {Timeout}s",
            filePath, requiredStability.TotalSeconds, maxWaitTimeout.TotalSeconds);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!File.Exists(filePath))
            {
                _logger?.LogWarning("Arquivo '{FilePath}' não encontrado durante verificação de estabilidade.", filePath);
                return false;
            }

            long currentSize;
            try
            {
                var fileInfo = new FileInfo(filePath);
                currentSize = fileInfo.Length;
            }
            catch (IOException ex)
            {
                _logger?.LogTrace(ex, "Erro de I/O temporário ao ler tamanho de '{FilePath}'. Continuando polling...", filePath);
                currentSize = -1;
            }

            if (currentSize != lastKnownSize)
            {
                lastKnownSize = currentSize;
                lastChangeTime = DateTime.UtcNow;
                _logger?.LogTrace("Tamanho de '{FilePath}' alterado para {Size} bytes. Resetando contador de estabilidade.", filePath, currentSize);
            }
            else if (currentSize > 0 && (DateTime.UtcNow - lastChangeTime) >= requiredStability)
            {
                // Tamanho estável pelo tempo requerido. Verificar se o lock exclusivo de gravação foi liberado.
                if (!IsFileLocked(filePath))
                {
                    _logger?.LogInformation("Arquivo '{FilePath}' estabilizado com sucesso ({Size} bytes).", filePath, currentSize);
                    return true;
                }

                _logger?.LogTrace("Tamanho de '{FilePath}' estável, porém o lock de gravação ainda não foi liberado.", filePath);
            }

            if ((DateTime.UtcNow - startTime) >= maxWaitTimeout)
            {
                _logger?.LogWarning("Timeout atingido ({Timeout}s) aguardando estabilização do arquivo '{FilePath}'.",
                    maxWaitTimeout.TotalSeconds, filePath);
                return false;
            }

            try
            {
                await Task.Delay(CheckInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return false;
    }

    public bool IsFileLocked(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None);

            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }
}

