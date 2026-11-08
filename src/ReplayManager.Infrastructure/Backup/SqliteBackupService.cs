using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.Backup;
using ReplayManager.Application.Models.Backup;
using ReplayManager.Infrastructure.Data;

namespace ReplayManager.Infrastructure.Backup;

/// <summary>
/// Implementação do serviço de backup SQLite utilizando a SQLite Online Backup API nativa.
/// Permite clonagem consistente de bancos em modo WAL sem locks prolongados e sem corrupção.
/// </summary>
public class SqliteBackupService : ISqliteBackupService
{
    private readonly ReplayManagerDbContext _dbContext;
    private readonly ILogger<SqliteBackupService>? _logger;

    public SqliteBackupService(
        ReplayManagerDbContext dbContext,
        ILogger<SqliteBackupService>? logger = null)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<BackupMetadata> CreateBackupAsync(string destinationFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFilePath);

        var destinationDir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrWhiteSpace(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        // Se o arquivo de destino já existir, remove para substituir pela nova cópia
        if (File.Exists(destinationFilePath))
        {
            File.Delete(destinationFilePath);
        }

        var sourceConnection = _dbContext.Database.GetDbConnection() as SqliteConnection;
        if (sourceConnection == null)
        {
            throw new InvalidOperationException("A conexão atual do DbContext não é uma conexão SQLite válida.");
        }

        var shouldCloseSource = false;
        if (sourceConnection.State != System.Data.ConnectionState.Open)
        {
            await sourceConnection.OpenAsync(cancellationToken);
            shouldCloseSource = true;
        }

        try
        {
            var destConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = destinationFilePath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            using (var destConnection = new SqliteConnection(destConnectionString))
            {
                await destConnection.OpenAsync(cancellationToken);

                // Utiliza a SQLite Backup API nativa (SqliteConnection.BackupDatabase)
                // Isto faz a cópia página a página de forma atômica e consistente com WAL ativo.
                sourceConnection.BackupDatabase(destConnection);
            }

            // Realiza verificação de integridade pós-cópia
            var (isValid, message) = await VerifyDatabaseIntegrityAsync(destinationFilePath, cancellationToken);
            var fileInfo = new FileInfo(destinationFilePath);

            _logger?.LogInformation("Backup SQLite gerado em '{Path}'. Tamanho: {Bytes} bytes. Integridade: {Valid}",
                destinationFilePath, fileInfo.Length, isValid);

            return new BackupMetadata
            {
                FileName = fileInfo.Name,
                LocalFilePath = destinationFilePath,
                FileSizeBytes = fileInfo.Length,
                CreatedUtc = DateTime.UtcNow,
                IsConsistent = isValid,
                Details = isValid ? "Backup consistente criado com sucesso via SQLite Backup API." : $"Falha de integridade: {message}"
            };
        }
        finally
        {
            if (shouldCloseSource)
            {
                await sourceConnection.CloseAsync();
            }
        }
    }

    public async Task<(bool IsValid, string Message)> VerifyDatabaseIntegrityAsync(string sqliteFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqliteFilePath);

        if (!File.Exists(sqliteFilePath))
        {
            return (false, "Arquivo de banco de dados não encontrado.");
        }

        var connString = new SqliteConnectionStringBuilder
        {
            DataSource = sqliteFilePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        try
        {
            using var connection = new SqliteConnection(connString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var resultString = result?.ToString() ?? string.Empty;

            if (string.Equals(resultString, "ok", StringComparison.OrdinalIgnoreCase))
            {
                return (true, "ok");
            }

            return (false, $"Resultado da verificação de integridade: {resultString}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao verificar integridade do banco SQLite '{Path}'.", sqliteFilePath);
            return (false, ex.Message);
        }
    }
}

