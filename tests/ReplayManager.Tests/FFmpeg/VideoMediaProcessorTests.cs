using FluentAssertions;
using Moq;
using ReplayManager.Application.Contracts.Media;
using ReplayManager.Application.Models.Media;
using ReplayManager.Application.Services;
using ReplayManager.Domain.Entities;
using ReplayManager.Infrastructure.Repositories;
using ReplayManager.Tests.Persistence;

namespace ReplayManager.Tests.FFmpeg;

public class VideoMediaProcessorTests : IDisposable
{
    private readonly TestDatabaseFixture _fixture;
    private readonly string _tempDir;
    private readonly Mock<IMediaMetadataExtractor> _metadataExtractorMock;
    private readonly Mock<IThumbnailGenerator> _thumbnailGeneratorMock;

    public VideoMediaProcessorTests()
    {
        _fixture = new TestDatabaseFixture();
        _tempDir = Path.Combine(Path.GetTempPath(), "ReplayManager_MediaProcTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _metadataExtractorMock = new Mock<IMediaMetadataExtractor>();
        _thumbnailGeneratorMock = new Mock<IThumbnailGenerator>();
    }

    [Fact]
    public async Task ProcessVideoMediaAsync_WhenMetadataAndThumbnailSucceed_UpdatesVideoRecord()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);

        var folder = new MonitoredFolder { Id = Guid.NewGuid(), Name = "F", Path = _tempDir };
        await uow.MonitoredFolders.AddAsync(folder);

        var videoPath = Path.Combine(_tempDir, "clip.mp4");
        await File.WriteAllBytesAsync(videoPath, [1, 2, 3, 4]);

        var video = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "clip.mp4",
            FilePath = videoPath,
            ContentSha256 = "hash123",
            FileSizeBytes = 4
        };
        await uow.Videos.AddAsync(video);
        await uow.SaveChangesAsync();

        var expectedMetadata = new VideoMetadata
        {
            DurationSeconds = 120.5,
            Width = 1920,
            Height = 1080,
            VideoCodec = "h264",
            BitrateBps = 10000000
        };

        _metadataExtractorMock
            .Setup(m => m.ExtractMetadataAsync(videoPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetadata);

        _thumbnailGeneratorMock
            .Setup(t => t.CalculateThumbnailTimestamp(120.5, 0.15))
            .Returns(18.075);

        var expectedThumbPath = Path.Combine(_tempDir, "thumbs", $"{video.Id}.jpg");
        _thumbnailGeneratorMock
            .Setup(t => t.GenerateThumbnailAsync(videoPath, It.IsAny<string>(), 18.075, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ThumbnailResult.Succeeded(expectedThumbPath, 18.075));

        var processor = new VideoMediaProcessor(
            uow,
            _metadataExtractorMock.Object,
            _thumbnailGeneratorMock.Object,
            thumbnailDirectory: Path.Combine(_tempDir, "thumbs"));

        // Act
        var result = await processor.ProcessVideoMediaAsync(video.Id);

        // Assert
        result.Should().BeTrue();

        var updated = await uow.Videos.GetByIdAsync(video.Id);
        updated.Should().NotBeNull();
        updated!.DurationSeconds.Should().Be(120.5);
        updated.ResolutionWidth.Should().Be(1920);
        updated.ResolutionHeight.Should().Be(1080);
        updated.ThumbnailPath.Should().Be(expectedThumbPath);
    }

    [Fact]
    public async Task ProcessVideoMediaAsync_WhenMetadataExtractorReturnsNull_PreservesVideoWithoutUpdating()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);

        var folder = new MonitoredFolder { Id = Guid.NewGuid(), Name = "F", Path = _tempDir };
        await uow.MonitoredFolders.AddAsync(folder);

        var videoPath = Path.Combine(_tempDir, "unknown.mp4");
        await File.WriteAllBytesAsync(videoPath, [10, 20]);

        var video = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "unknown.mp4",
            FilePath = videoPath,
            ContentSha256 = "hash999",
            FileSizeBytes = 2
        };
        await uow.Videos.AddAsync(video);
        await uow.SaveChangesAsync();

        _metadataExtractorMock
            .Setup(m => m.ExtractMetadataAsync(videoPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VideoMetadata?)null);

        var processor = new VideoMediaProcessor(
            uow,
            _metadataExtractorMock.Object,
            _thumbnailGeneratorMock.Object,
            thumbnailDirectory: _tempDir);

        // Act
        var result = await processor.ProcessVideoMediaAsync(video.Id);

        // Assert
        result.Should().BeFalse();
        var reloaded = await uow.Videos.GetByIdAsync(video.Id);
        reloaded!.DurationSeconds.Should().BeNull();
        reloaded.ThumbnailPath.Should().BeNull();
    }

    [Fact]
    public async Task ProcessVideoMediaAsync_WhenVideoDoesNotExist_ReturnsFalse()
    {
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);

        var processor = new VideoMediaProcessor(
            uow,
            _metadataExtractorMock.Object,
            _thumbnailGeneratorMock.Object,
            thumbnailDirectory: _tempDir);

        var result = await processor.ProcessVideoMediaAsync(Guid.NewGuid());
        result.Should().BeFalse();
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

