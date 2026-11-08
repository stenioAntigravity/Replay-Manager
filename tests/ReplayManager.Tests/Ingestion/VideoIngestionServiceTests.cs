using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ReplayManager.Application.Contracts.FileSystem;
using ReplayManager.Application.Models.Ingestion;
using ReplayManager.Application.Services;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using ReplayManager.Infrastructure.FileSystem;
using ReplayManager.Infrastructure.Repositories;
using ReplayManager.Tests.Persistence;

namespace ReplayManager.Tests.Ingestion;

public class VideoIngestionServiceTests : IDisposable
{
    private readonly TestDatabaseFixture _fixture;
    private readonly string _tempDir;
    private readonly IContentHasher _hasher;
    private readonly IFileStabilityChecker _stabilityChecker;

    public VideoIngestionServiceTests()
    {
        _fixture = new TestDatabaseFixture();
        _tempDir = Path.Combine(Path.GetTempPath(), "ReplayManager_IngestTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _hasher = new Sha256ContentHasher();
        _stabilityChecker = new FileStabilityChecker();
    }

    [Fact]
    public async Task IngestVideoFileAsync_NewFile_CreatesVideoRecordSuccessfully()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);
        var service = new VideoIngestionService(uow, _hasher, _stabilityChecker);

        var folder = new MonitoredFolder
        {
            Id = Guid.NewGuid(),
            Name = "Folder A",
            Path = _tempDir,
            IsActive = true
        };
        await uow.MonitoredFolders.AddAsync(folder);
        await uow.SaveChangesAsync();

        var videoFile = Path.Combine(_tempDir, "gameplay1.mp4");
        await File.WriteAllBytesAsync(videoFile, [1, 2, 3, 4, 5, 6]);

        // Act
        var result = await service.IngestVideoFileAsync(folder.Id, videoFile, waitForStability: false);

        // Assert
        result.Status.Should().Be(IngestionStatus.Ingested);
        result.Video.Should().NotBeNull();
        result.Video!.FileName.Should().Be("gameplay1.mp4");
        result.Video.FilePath.Should().Be(videoFile);
        result.Video.FileSizeBytes.Should().Be(6);
        result.Video.LocalStatus.Should().Be(LocalFileStatus.Available);
        result.Video.ContentSha256.Should().NotBeNullOrWhiteSpace();

        var persisted = await context.Videos.FirstOrDefaultAsync(v => v.Id == result.Video.Id);
        persisted.Should().NotBeNull();
        persisted!.MonitoredFolderId.Should().Be(folder.Id);
    }

    [Fact]
    public async Task IngestVideoFileAsync_DuplicateContentInSameFolder_ReturnsDuplicateWithoutInsertingSecondRow()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);
        var service = new VideoIngestionService(uow, _hasher, _stabilityChecker);

        var folder = new MonitoredFolder
        {
            Id = Guid.NewGuid(),
            Name = "Folder Duplicates",
            Path = _tempDir,
            IsActive = true
        };
        await uow.MonitoredFolders.AddAsync(folder);
        await uow.SaveChangesAsync();

        var videoFile1 = Path.Combine(_tempDir, "original.mp4");
        var dummyBytes = new byte[] { 10, 20, 30, 40, 50 };
        await File.WriteAllBytesAsync(videoFile1, dummyBytes);

        var firstResult = await service.IngestVideoFileAsync(folder.Id, videoFile1, waitForStability: false);
        firstResult.Status.Should().Be(IngestionStatus.Ingested);

        // Act - Ingerir exatamente o mesmo arquivo novamente
        var secondResult = await service.IngestVideoFileAsync(folder.Id, videoFile1, waitForStability: false);

        // Assert
        secondResult.Status.Should().Be(IngestionStatus.DuplicateSameFolder);
        var count = await context.Videos.CountAsync(v => v.MonitoredFolderId == folder.Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task IngestVideoFileAsync_IdenticalContentInDifferentFolders_CreatesTwoDistinctRecords()
    {
        // Arrange (ADR 003: Deduplicação restrita à mesma pasta)
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);
        var service = new VideoIngestionService(uow, _hasher, _stabilityChecker);

        var folderA = new MonitoredFolder { Id = Guid.NewGuid(), Name = "Folder A", Path = Path.Combine(_tempDir, "A"), IsActive = true };
        var folderB = new MonitoredFolder { Id = Guid.NewGuid(), Name = "Folder B", Path = Path.Combine(_tempDir, "B"), IsActive = true };
        Directory.CreateDirectory(folderA.Path);
        Directory.CreateDirectory(folderB.Path);

        await uow.MonitoredFolders.AddAsync(folderA);
        await uow.MonitoredFolders.AddAsync(folderB);
        await uow.SaveChangesAsync();

        var sharedBytes = new byte[] { 99, 88, 77, 66 };
        var fileA = Path.Combine(folderA.Path, "clip.mp4");
        var fileB = Path.Combine(folderB.Path, "clip.mp4");
        await File.WriteAllBytesAsync(fileA, sharedBytes);
        await File.WriteAllBytesAsync(fileB, sharedBytes);

        // Act
        var resultA = await service.IngestVideoFileAsync(folderA.Id, fileA, waitForStability: false);
        var resultB = await service.IngestVideoFileAsync(folderB.Id, fileB, waitForStability: false);

        // Assert
        resultA.Status.Should().Be(IngestionStatus.Ingested);
        resultB.Status.Should().Be(IngestionStatus.Ingested);
        resultA.Video!.Id.Should().NotBe(resultB.Video!.Id);
        resultA.Video.ContentSha256.Should().Be(resultB.Video.ContentSha256);

        var allVideos = await context.Videos.ToListAsync();
        allVideos.Count.Should().Be(2);
    }

    [Fact]
    public async Task IngestVideoFileAsync_RenamedFileInSameFolder_UpdatesExistingRecord()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);
        var service = new VideoIngestionService(uow, _hasher, _stabilityChecker);

        var folder = new MonitoredFolder { Id = Guid.NewGuid(), Name = "Rename Folder", Path = _tempDir, IsActive = true };
        await uow.MonitoredFolders.AddAsync(folder);
        await uow.SaveChangesAsync();

        var initialFile = Path.Combine(_tempDir, "initial_name.mp4");
        var fileBytes = new byte[] { 1, 3, 5, 7, 9 };
        await File.WriteAllBytesAsync(initialFile, fileBytes);

        var firstResult = await service.IngestVideoFileAsync(folder.Id, initialFile, waitForStability: false);
        firstResult.Status.Should().Be(IngestionStatus.Ingested);

        // Renomear arquivo no disco
        var renamedFile = Path.Combine(_tempDir, "renamed_video.mp4");
        File.Move(initialFile, renamedFile);

        // Act
        var renameResult = await service.IngestVideoFileAsync(folder.Id, renamedFile, waitForStability: false);

        // Assert
        renameResult.Status.Should().Be(IngestionStatus.RenamedUpdated);
        renameResult.Video!.Id.Should().Be(firstResult.Video!.Id);
        renameResult.Video.FileName.Should().Be("renamed_video.mp4");
        renameResult.Video.FilePath.Should().Be(renamedFile);

        var count = await context.Videos.CountAsync(v => v.MonitoredFolderId == folder.Id);
        count.Should().Be(1);
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
