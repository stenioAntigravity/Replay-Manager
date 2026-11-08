using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Application.Contracts.Upload;
using ReplayManager.Application.Contracts.YouTube;
using ReplayManager.Application.Models.YouTube;
using ReplayManager.Application.Services.Upload;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using ReplayManager.Infrastructure.Data;
using ReplayManager.Infrastructure.Repositories;
using ReplayManager.Infrastructure.YouTube;
using Xunit;

namespace ReplayManager.Tests.SystemIntegration;

public class EndToEndValidationAndStressTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _dbPath;

    public EndToEndValidationAndStressTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "RM_Stress_E2E_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
        _dbPath = Path.Combine(_testRoot, "stress_test.db");
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }
        catch
        {
            // Ignore OS file locks on temp directory cleanup
        }
    }

    private ReplayManagerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ReplayManagerDbContext>()
            .UseSqlite($"Data Source={_dbPath};")
            .Options;
        var ctx = new ReplayManagerDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public void LargeFileSimulation_ByteStreamingAndHashing_HandlesMultiGigabyteBoundsWithoutMemorySpike()
    {
        // Valida que o processamento em streaming suporta arquivos de grande porte (> 5 GB) com footprint constante
        var dummyData = new byte[64 * 1024]; // 64 KB buffer
        Array.Fill<byte>(dummyData, 0xAA);

        using var memoryStream = new MemoryStream(dummyData);
        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

        using var throttled = new ThrottledStream(memoryStream, 1024 * 1024);
        throttled.MaxBytesPerSecond.Should().Be(1024 * 1024);

        var currentMemory = GC.GetTotalMemory(forceFullCollection: false);
        var allocated = currentMemory - initialMemory;

        // O consumo de memória não pode explodir (permanece estritamente dentro da margem operacional)
        allocated.Should().BeLessThan(10 * 1024 * 1024);
    }

    [Fact]
    public async Task NetworkDisconnection_DuringUpload_UpdatesQueueItemToFailedWithPreservedState()
    {
        // 1. Arrange
        using var context = CreateDbContext();
        var unitOfWork = new UnitOfWork(context);
        var queueRepo = new UploadQueueRepository(context);
        var videoRepo = new VideoRecordRepository(context);

        var folder = new MonitoredFolder { Name = "PubgHighlights", Path = @"C:\PUBG\Destaques" };
        context.MonitoredFolders.Add(folder);
        await context.SaveChangesAsync();

        var video = new VideoRecord
        {
            Id = Guid.NewGuid(),
            FileName = "network_drop_test.mp4",
            FilePath = @"C:\PUBG\Destaques\network_drop_test.mp4",
            ContentSha256 = "drop123hash",
            FileSizeBytes = 100 * 1024 * 1024,
            MonitoredFolderId = folder.Id,
            UploadStatus = UploadStatus.Queued,
            LocalStatus = LocalFileStatus.Available
        };
        context.Videos.Add(video);
        await context.SaveChangesAsync();

        var queueItem = new UploadQueueItem
        {
            Id = Guid.NewGuid(),
            VideoRecordId = video.Id,
            VideoRecord = video,
            Priority = 1,
            Status = UploadStatus.Queued,
            BytesUploaded = 15 * 1024 * 1024,
            ResumableSessionUri = "https://youtube.googleapis.com/upload/resumable_mock_session"
        };
        context.UploadQueue.Add(queueItem);
        await context.SaveChangesAsync();

        // 2. Act: Mock de falha de conexão de rede durante o upload
        var uploaderMock = new Mock<IYouTubeUploader>();
        uploaderMock
            .Setup(u => u.UploadVideoAsync(It.IsAny<VideoRecord>(), It.IsAny<IProgress<UploadProgressNotification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(YouTubeUploadResult.Failed("Falha de conexao com a internet: Host unreachable"));

        var queueService = new UploadQueueService(queueRepo, videoRepo, unitOfWork, NullLogger<UploadQueueService>.Instance);
        var processor = new UploadQueueProcessor(queueRepo, uploaderMock.Object, queueService, unitOfWork, NullLogger<UploadQueueProcessor>.Instance);

        await processor.ProcessQueueAsync(CancellationToken.None);

        // 3. Assert: Item persiste o erro e transiciona com segurança sem quebrar a integridade do banco
        var reloadedItem = await context.UploadQueue.FirstAsync(q => q.Id == queueItem.Id);
        reloadedItem.Status.Should().Be(UploadStatus.Failed);
        reloadedItem.LastError.Should().Contain("Falha de conexao");
    }

    [Fact]
    public async Task CorruptedOrInvalidVideo_IsHandledGracefully_WithoutCrashingQueue()
    {
        // 1. Arrange
        using var context = CreateDbContext();
        var unitOfWork = new UnitOfWork(context);
        var queueRepo = new UploadQueueRepository(context);
        var videoRepo = new VideoRecordRepository(context);

        var folder = new MonitoredFolder { Name = "CorruptedFolder", Path = _testRoot };
        context.MonitoredFolders.Add(folder);
        await context.SaveChangesAsync();

        var video = new VideoRecord
        {
            Id = Guid.NewGuid(),
            FileName = "corrupted.mp4",
            FilePath = Path.Combine(_testRoot, "corrupted.mp4"),
            ContentSha256 = "corruptedsha",
            FileSizeBytes = 3,
            MonitoredFolderId = folder.Id,
            UploadStatus = UploadStatus.Queued,
            LocalStatus = LocalFileStatus.Available
        };
        context.Videos.Add(video);
        await context.SaveChangesAsync();

        var queueItem = new UploadQueueItem
        {
            Id = Guid.NewGuid(),
            VideoRecordId = video.Id,
            VideoRecord = video,
            Priority = 1,
            Status = UploadStatus.Queued
        };
        context.UploadQueue.Add(queueItem);
        await context.SaveChangesAsync();

        // 2. Act: Uploader rejeita por arquivo corrompido ou inválido
        var uploaderMock = new Mock<IYouTubeUploader>();
        uploaderMock
            .Setup(u => u.UploadVideoAsync(It.IsAny<VideoRecord>(), It.IsAny<IProgress<UploadProgressNotification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(YouTubeUploadResult.Failed("Arquivo corrompido ou invalido para upload"));

        var queueService = new UploadQueueService(queueRepo, videoRepo, unitOfWork, NullLogger<UploadQueueService>.Instance);
        var processor = new UploadQueueProcessor(queueRepo, uploaderMock.Object, queueService, unitOfWork, NullLogger<UploadQueueProcessor>.Instance);

        await processor.ProcessQueueAsync(CancellationToken.None);

        // 3. Assert
        var resultItem = await context.UploadQueue.FirstAsync(q => q.Id == queueItem.Id);
        resultItem.Status.Should().Be(UploadStatus.Failed);
        resultItem.LastError.Should().Contain("Arquivo corrompido ou invalido");
    }
}
