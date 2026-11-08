using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ReplayManager.Application.Contracts.FileSystem;
using ReplayManager.Application.Services;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using ReplayManager.Infrastructure.FileSystem;
using ReplayManager.Infrastructure.Repositories;
using ReplayManager.Tests.Persistence;

namespace ReplayManager.Tests.FileSystem;

public class FolderReconciliationWorkerTests : IDisposable
{
    private readonly TestDatabaseFixture _fixture;
    private readonly string _tempDir;
    private readonly IContentHasher _hasher;
    private readonly IFileStabilityChecker _stabilityChecker;

    public FolderReconciliationWorkerTests()
    {
        _fixture = new TestDatabaseFixture();
        _tempDir = Path.Combine(Path.GetTempPath(), "ReplayManager_ReconcileTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _hasher = new Sha256ContentHasher();
        _stabilityChecker = new FileStabilityChecker();
    }

    [Fact]
    public async Task ReconcileFolderAsync_WithNewFilesOnDisk_IngestsAllDiscoveredVideos()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);
        var ingestion = new VideoIngestionService(uow, _hasher, _stabilityChecker);
        var worker = new FolderReconciliationWorker(uow, ingestion);

        var folder = new MonitoredFolder
        {
            Id = Guid.NewGuid(),
            Name = "Reconcile Target",
            Path = _tempDir,
            IsActive = true
        };
        await uow.MonitoredFolders.AddAsync(folder);
        await uow.SaveChangesAsync();

        var file1 = Path.Combine(_tempDir, "rec1.mp4");
        var file2 = Path.Combine(_tempDir, "rec2.mkv");
        await File.WriteAllBytesAsync(file1, [1, 2, 3]);
        await File.WriteAllBytesAsync(file2, [4, 5, 6]);

        // Act
        var result = await worker.ReconcileFolderAsync(folder.Id);

        // Assert
        result.Errors.Should().BeEmpty();
        result.Success.Should().BeTrue();
        result.TotalScannedFiles.Should().Be(2);
        result.NewFilesIngested.Should().Be(2);

        var count = await context.Videos.CountAsync(v => v.MonitoredFolderId == folder.Id);
        count.Should().Be(2);

        var updatedFolder = await uow.MonitoredFolders.GetByIdAsync(folder.Id);
        updatedFolder!.LastScannedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ReconcileFolderAsync_WhenFileDeletedExternally_MarksAsMissingWithoutDeletingRecord()
    {
        // Arrange (ADR 007: Preservação Irrestrita de Arquivos Locais)
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);
        var ingestion = new VideoIngestionService(uow, _hasher, _stabilityChecker);
        var worker = new FolderReconciliationWorker(uow, ingestion);

        var folder = new MonitoredFolder
        {
            Id = Guid.NewGuid(),
            Name = "Missing Check",
            Path = _tempDir,
            IsActive = true
        };
        await uow.MonitoredFolders.AddAsync(folder);
        await uow.SaveChangesAsync();

        var physicalFile = Path.Combine(_tempDir, "to_delete.mp4");
        await File.WriteAllBytesAsync(physicalFile, [7, 8, 9]);

        var ingestResult = await ingestion.IngestVideoFileAsync(folder.Id, physicalFile, waitForStability: false);
        ingestResult.Status.Should().Be(Application.Models.Ingestion.IngestionStatus.Ingested);

        // Deleta arquivo externamente
        File.Delete(physicalFile);

        // Act
        var result = await worker.ReconcileFolderAsync(folder.Id);

        // Assert
        result.Errors.Should().BeEmpty();
        result.Success.Should().BeTrue();
        result.MissingFilesMarked.Should().Be(1);

        var recordInDb = await context.Videos.FirstOrDefaultAsync(v => v.Id == ingestResult.Video!.Id);
        recordInDb.Should().NotBeNull("O registro nunca deve ser deletado do banco quando o arquivo for removido do disco.");
        recordInDb!.LocalStatus.Should().Be(LocalFileStatus.Missing);
    }

    [Fact]
    public async Task ReconcileFolderAsync_WhenMissingFileReappears_RestoresAvailableStatus()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);
        var ingestion = new VideoIngestionService(uow, _hasher, _stabilityChecker);
        var worker = new FolderReconciliationWorker(uow, ingestion);

        var folder = new MonitoredFolder
        {
            Id = Guid.NewGuid(),
            Name = "Restore Check",
            Path = _tempDir,
            IsActive = true
        };
        await uow.MonitoredFolders.AddAsync(folder);
        await uow.SaveChangesAsync();

        var targetFile = Path.Combine(_tempDir, "reappearing.mp4");
        await File.WriteAllBytesAsync(targetFile, [10, 20, 30]);

        var ingest = await ingestion.IngestVideoFileAsync(folder.Id, targetFile, waitForStability: false);

        // Marca manualmente como Missing para simular estado anterior
        var videoRecord = await uow.Videos.GetByIdAsync(ingest.Video!.Id);
        videoRecord!.LocalStatus = LocalFileStatus.Missing;
        await uow.Videos.UpdateAsync(videoRecord);
        await uow.SaveChangesAsync();

        // Act
        var result = await worker.ReconcileFolderAsync(folder.Id);

        // Assert
        result.Errors.Should().BeEmpty();
        result.Success.Should().BeTrue();
        var restored = await uow.Videos.GetByIdAsync(videoRecord.Id);
        restored!.LocalStatus.Should().Be(LocalFileStatus.Available);
    }

    public void Dispose()
    {
        _fixture.Dispose();
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Limpeza
        }
    }
}
