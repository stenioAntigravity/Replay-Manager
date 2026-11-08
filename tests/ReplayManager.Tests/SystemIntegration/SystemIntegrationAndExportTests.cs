using System.IO;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using ReplayManager.Infrastructure.Data;
using ReplayManager.Infrastructure.Export;
using ReplayManager.Infrastructure.SystemIntegration;
using Xunit;

namespace ReplayManager.Tests.SystemIntegration;

public class SystemIntegrationAndExportTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _sqliteDbPath;

    public SystemIntegrationAndExportTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ReplayManager_SysTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _sqliteDbPath = Path.Combine(_tempDirectory, "test.db");
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignora locks do OS
        }
    }

    [Fact]
    public async Task ExportToCsvAsync_ShouldGenerateValidCsvWithHeadersAndRows()
    {
        // 1. Arrange: Popula banco
        var options = new DbContextOptionsBuilder<ReplayManagerDbContext>()
            .UseSqlite($"Data Source={_sqliteDbPath};")
            .Options;

        using (var context = new ReplayManagerDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            var folder = new MonitoredFolder { Name = "PUBG", Path = @"C:\PUBG" };
            context.MonitoredFolders.Add(folder);
            await context.SaveChangesAsync();

            context.Videos.Add(new VideoRecord
            {
                FileName = "match_01.mp4",
                FilePath = @"C:\PUBG\match_01.mp4",
                ContentSha256 = "abc123sha",
                FileSizeBytes = 50 * 1024 * 1024,
                DurationSeconds = 120.5,
                MonitoredFolderId = folder.Id,
                UploadStatus = UploadStatus.Completed,
                LocalStatus = LocalFileStatus.Available,
                FilterStatus = FilterEvaluationStatus.Approved,
                YouTubeVideoId = "yt_test_id",
                YouTubeUrl = "https://youtu.be/yt_test_id"
            });
            await context.SaveChangesAsync();
        }

        // 2. Act
        using (var context = new ReplayManagerDbContext(options))
        {
            var exportService = new LibraryExportService(context);
            var csvPath = Path.Combine(_tempDirectory, "export.csv");

            var resultPath = await exportService.ExportToCsvAsync(csvPath);

            // 3. Assert
            File.Exists(resultPath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(resultPath);
            content.Should().Contain("Id;FileName;RecordedAtUtc;DurationSeconds");
            content.Should().Contain("match_01.mp4");
            content.Should().Contain("yt_test_id");
            content.Should().Contain("Completed");
        }
    }

    [Fact]
    public async Task ExportToJsonAsync_ShouldGenerateValidJsonArray()
    {
        // 1. Arrange: Popula banco
        var options = new DbContextOptionsBuilder<ReplayManagerDbContext>()
            .UseSqlite($"Data Source={_sqliteDbPath};")
            .Options;

        using (var context = new ReplayManagerDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            var folder = new MonitoredFolder { Name = "Destaques", Path = @"C:\Clips" };
            context.MonitoredFolders.Add(folder);
            await context.SaveChangesAsync();

            context.Videos.Add(new VideoRecord
            {
                FileName = "highlight_01.mp4",
                FilePath = @"C:\Clips\highlight_01.mp4",
                ContentSha256 = "sha789xyz",
                FileSizeBytes = 25 * 1024 * 1024,
                DurationSeconds = 45.0,
                MonitoredFolderId = folder.Id,
                UploadStatus = UploadStatus.LocalOnly,
                LocalStatus = LocalFileStatus.Available,
                FilterStatus = FilterEvaluationStatus.Approved
            });
            await context.SaveChangesAsync();
        }

        // 2. Act
        using (var context = new ReplayManagerDbContext(options))
        {
            var exportService = new LibraryExportService(context);
            var jsonPath = Path.Combine(_tempDirectory, "export.json");

            var resultPath = await exportService.ExportToJsonAsync(jsonPath);

            // 3. Assert
            File.Exists(resultPath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(resultPath);
            content.Should().Contain("\"FileName\": \"highlight_01.mp4\"");
            content.Should().Contain("\"UploadStatus\": \"LocalOnly\"");
            content.Should().Contain("\"MonitoredFolder\": \"Destaques\"");
        }
    }

    [Fact]
    public void WindowsStartupRegistrationService_CanToggleAutoStart()
    {
        // Testa o serviço com caminho customizado seguro para não afetar o registro real do app
        var service = new WindowsStartupRegistrationService(null, @"C:\TestApp\ReplayManager.exe");

        // Act: Habilitar
        var enableResult = service.SetAutoStart(true, "--minimized");
        enableResult.Should().BeTrue();
        service.IsAutoStartEnabled().Should().BeTrue();

        // Act: Desabilitar
        var disableResult = service.SetAutoStart(false);
        disableResult.Should().BeTrue();
        service.IsAutoStartEnabled().Should().BeFalse();
    }
}

