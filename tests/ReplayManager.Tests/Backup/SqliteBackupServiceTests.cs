using System.IO;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using ReplayManager.Application.Contracts.Backup;
using ReplayManager.Domain.Entities;
using ReplayManager.Infrastructure.Backup;
using ReplayManager.Infrastructure.Data;
using ReplayManager.Infrastructure.Security;
using Xunit;

namespace ReplayManager.Tests.Backup;

public class SqliteBackupServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _sourceDbPath;

    public SqliteBackupServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ReplayManager_BackupTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _sourceDbPath = Path.Combine(_tempDirectory, "source.db");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignora locks temporários do OS em limpeza de teste
        }
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldCreateConsistentBackup_AndPassIntegrityCheck()
    {
        // 1. Arrange: Cria banco fonte com tabelas e registros
        var options = new DbContextOptionsBuilder<ReplayManagerDbContext>()
            .UseSqlite($"Data Source={_sourceDbPath};")
            .Options;

        using (var context = new ReplayManagerDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.MonitoredFolders.Add(new MonitoredFolder
            {
                Name = "PUBG Clips",
                Path = @"C:\Recordings\PUBG",
                AutoUploadEnabled = true
            });
            await context.SaveChangesAsync();
        }

        // 2. Act: Executa o serviço de backup atômico
        using (var context = new ReplayManagerDbContext(options))
        {
            var backupService = new SqliteBackupService(context);
            var backupDestPath = Path.Combine(_tempDirectory, "backup_test.db");

            var metadata = await backupService.CreateBackupAsync(backupDestPath);

            // 3. Assert
            metadata.Should().NotBeNull();
            metadata.IsConsistent.Should().BeTrue();
            metadata.FileSizeBytes.Should().BeGreaterThan(0);
            File.Exists(backupDestPath).Should().BeTrue();

            // Verifica se os dados estão preservados no arquivo de backup independente
            var backupOptions = new DbContextOptionsBuilder<ReplayManagerDbContext>()
                .UseSqlite($"Data Source={backupDestPath};")
                .Options;

            using var backupContext = new ReplayManagerDbContext(backupOptions);
            var folder = await backupContext.MonitoredFolders.FirstOrDefaultAsync(f => f.Name == "PUBG Clips");
            folder.Should().NotBeNull();
            folder!.Path.Should().Be(@"C:\Recordings\PUBG");
        }
    }

    [Fact]
    public async Task VerifyDatabaseIntegrityAsync_WithCorruptedFile_ShouldReturnFalse()
    {
        // 1. Arrange: Cria arquivo com bytes arbitrários corrompidos (não sendo SQLite válido)
        var corruptedFilePath = Path.Combine(_tempDirectory, "corrupted.db");
        await File.WriteAllBytesAsync(corruptedFilePath, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });

        var options = new DbContextOptionsBuilder<ReplayManagerDbContext>()
            .UseSqlite($"Data Source={_sourceDbPath};")
            .Options;

        using var context = new ReplayManagerDbContext(options);
        var backupService = new SqliteBackupService(context);

        // 2. Act
        var (isValid, message) = await backupService.VerifyDatabaseIntegrityAsync(corruptedFilePath);

        // 3. Assert
        isValid.Should().BeFalse();
        message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task VerifyDatabaseIntegrityAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        var options = new DbContextOptionsBuilder<ReplayManagerDbContext>()
            .UseSqlite($"Data Source={_sourceDbPath};")
            .Options;

        using var context = new ReplayManagerDbContext(options);
        var backupService = new SqliteBackupService(context);

        var (isValid, message) = await backupService.VerifyDatabaseIntegrityAsync(Path.Combine(_tempDirectory, "missing.db"));

        isValid.Should().BeFalse();
        message.Should().Contain("não encontrado");
    }

    [Fact]
    public async Task GoogleDriveBackupService_WhenIntegrityCheckFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mockSqliteBackup = new Mock<ISqliteBackupService>();
        var dummyFilePath = Path.Combine(_tempDirectory, "dummy.db");
        await File.WriteAllTextAsync(dummyFilePath, "dummy");

        mockSqliteBackup.Setup(s => s.VerifyDatabaseIntegrityAsync(dummyFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "PRAGMA integrity_check failed: file is corrupted"));

        var dpapiStorage = new DpapiCredentialStorage();
        var driveService = new GoogleDriveBackupService(mockSqliteBackup.Object, dpapiStorage);

        // Act & Assert
        var act = async () => await driveService.UploadBackupAsync(dummyFilePath);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não passou na verificação de integridade*");
    }
}
