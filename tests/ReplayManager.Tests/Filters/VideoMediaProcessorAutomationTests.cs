using FluentAssertions;
using Moq;
using ReplayManager.Application.Contracts.Filters;
using ReplayManager.Application.Contracts.Media;
using ReplayManager.Application.Models.Filters;
using ReplayManager.Application.Models.Media;
using ReplayManager.Application.Services;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using ReplayManager.Infrastructure.Repositories;
using ReplayManager.Tests.Persistence;

namespace ReplayManager.Tests.Filters;

public class VideoMediaProcessorAutomationTests : IDisposable
{
    private readonly TestDatabaseFixture _fixture;
    private readonly string _tempDir;
    private readonly Mock<IMediaMetadataExtractor> _metadataExtractorMock;
    private readonly Mock<IThumbnailGenerator> _thumbnailGeneratorMock;
    private readonly Mock<IFilterEvaluator> _filterEvaluatorMock;

    public VideoMediaProcessorAutomationTests()
    {
        _fixture = new TestDatabaseFixture();
        _tempDir = Path.Combine(Path.GetTempPath(), "ReplayManager_AutomationTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _metadataExtractorMock = new Mock<IMediaMetadataExtractor>();
        _thumbnailGeneratorMock = new Mock<IThumbnailGenerator>();
        _filterEvaluatorMock = new Mock<IFilterEvaluator>();
    }

    [Fact]
    public async Task ProcessVideoMediaAsync_WhenAutoUploadEnabledAndFiltersApproved_EnqueuesVideoAutomatically()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);

        var folder = new MonitoredFolder
        {
            Id = Guid.NewGuid(),
            Name = "PUBG Highlights",
            Path = _tempDir,
            AutoUploadEnabled = true
        };
        await uow.MonitoredFolders.AddAsync(folder);

        var videoPath = Path.Combine(_tempDir, "pubg_highlight.mp4");
        await File.WriteAllBytesAsync(videoPath, [1, 2, 3, 4]);

        var video = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "pubg_highlight.mp4",
            FilePath = videoPath,
            ContentSha256 = "hash123",
            FileSizeBytes = 4,
            UploadStatus = UploadStatus.None
        };
        await uow.Videos.AddAsync(video);
        await uow.SaveChangesAsync();

        _metadataExtractorMock
            .Setup(m => m.ExtractMetadataAsync(videoPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoMetadata { DurationSeconds = 60, Width = 1920, Height = 1080 });

        _thumbnailGeneratorMock
            .Setup(t => t.GenerateThumbnailAsync(videoPath, It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ThumbnailResult.Succeeded("thumb.jpg", 9.0));

        _filterEvaluatorMock
            .Setup(f => f.Evaluate(It.IsAny<VideoRecord>(), It.IsAny<FolderFilterRule?>(), It.IsAny<string?>()))
            .Returns(FilterEvaluationResult.Approved());

        var processor = new VideoMediaProcessor(
            uow,
            _metadataExtractorMock.Object,
            _thumbnailGeneratorMock.Object,
            _filterEvaluatorMock.Object,
            thumbnailDirectory: _tempDir);

        // Act
        var result = await processor.ProcessVideoMediaAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        var updatedVideo = await uow.Videos.GetByIdAsync(video.Id);
        updatedVideo.Should().NotBeNull();
        updatedVideo!.FilterStatus.Should().Be(FilterEvaluationStatus.Approved);
        updatedVideo.UploadStatus.Should().Be(UploadStatus.Queued);

        var queuedItem = await uow.UploadQueue.GetNextPendingItemAsync();
        queuedItem.Should().NotBeNull();
        queuedItem!.VideoRecordId.Should().Be(video.Id);
        queuedItem.Status.Should().Be(UploadStatus.Queued);
    }

    [Fact]
    public async Task ProcessVideoMediaAsync_WhenFiltersRejected_SetsRejectedStatusAndDoesNotEnqueue()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);

        var folder = new MonitoredFolder
        {
            Id = Guid.NewGuid(),
            Name = "PUBG Highlights",
            Path = _tempDir,
            AutoUploadEnabled = true
        };
        await uow.MonitoredFolders.AddAsync(folder);

        var videoPath = Path.Combine(_tempDir, "small.mp4");
        await File.WriteAllBytesAsync(videoPath, [1, 2]);

        var video = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "small.mp4",
            FilePath = videoPath,
            ContentSha256 = "hash456",
            FileSizeBytes = 2,
            UploadStatus = UploadStatus.None
        };
        await uow.Videos.AddAsync(video);
        await uow.SaveChangesAsync();

        _metadataExtractorMock
            .Setup(m => m.ExtractMetadataAsync(videoPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoMetadata { DurationSeconds = 10, Width = 1280, Height = 720 });

        _thumbnailGeneratorMock
            .Setup(t => t.GenerateThumbnailAsync(videoPath, It.IsAny<string>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ThumbnailResult.Succeeded("thumb.jpg", 1.5));

        _filterEvaluatorMock
            .Setup(f => f.Evaluate(It.IsAny<VideoRecord>(), It.IsAny<FolderFilterRule?>(), It.IsAny<string?>()))
            .Returns(FilterEvaluationResult.Rejected(FilterEvaluationStatus.RejectedByMinSize, "Tamanho menor que 10MB."));

        var processor = new VideoMediaProcessor(
            uow,
            _metadataExtractorMock.Object,
            _thumbnailGeneratorMock.Object,
            _filterEvaluatorMock.Object,
            thumbnailDirectory: _tempDir);

        // Act
        var result = await processor.ProcessVideoMediaAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        var updatedVideo = await uow.Videos.GetByIdAsync(video.Id);
        updatedVideo.Should().NotBeNull();
        updatedVideo!.FilterStatus.Should().Be(FilterEvaluationStatus.RejectedByMinSize);
        updatedVideo.RejectionReason.Should().Be("Tamanho menor que 10MB.");
        updatedVideo.UploadStatus.Should().Be(UploadStatus.None);

        var queuedItem = await uow.UploadQueue.GetNextPendingItemAsync();
        queuedItem.Should().BeNull();
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
        catch { }
    }
}

