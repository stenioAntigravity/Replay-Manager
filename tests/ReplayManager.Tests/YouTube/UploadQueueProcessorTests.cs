using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Application.Contracts.Upload;
using ReplayManager.Application.Contracts.YouTube;
using ReplayManager.Application.Models.YouTube;
using ReplayManager.Application.Services.Upload;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using Xunit;

namespace ReplayManager.Tests.YouTube;

public class UploadQueueProcessorTests
{
    private readonly Mock<IUploadQueueRepository> _queueRepoMock = new();
    private readonly Mock<IYouTubeUploader> _uploaderMock = new();
    private readonly Mock<IUploadQueueService> _queueServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task ProcessQueueAsync_WhenQueueEmpty_ExitsGracefully()
    {
        // Arrange
        _queueRepoMock
            .Setup(r => r.GetNextPendingItemAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((UploadQueueItem?)null);

        var processor = new UploadQueueProcessor(
            _queueRepoMock.Object,
            _uploaderMock.Object,
            _queueServiceMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<UploadQueueProcessor>.Instance);

        // Act
        await processor.ProcessQueueAsync(CancellationToken.None);

        // Assert
        _uploaderMock.Verify(u => u.UploadVideoAsync(It.IsAny<VideoRecord>(), It.IsAny<IProgress<UploadProgressNotification>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessQueueAsync_WhenUploadSucceeds_UpdatesQueueItemToCompleted()
    {
        // Arrange
        var video = new VideoRecord
        {
            Id = Guid.NewGuid(),
            FileName = "replay.mp4",
            FilePath = @"C:\replays\replay.mp4"
        };

        var queueItem = new UploadQueueItem
        {
            Id = Guid.NewGuid(),
            VideoRecordId = video.Id,
            VideoRecord = video,
            Status = UploadStatus.Queued
        };

        _queueRepoMock
            .SetupSequence(r => r.GetNextPendingItemAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(queueItem)
            .ReturnsAsync((UploadQueueItem?)null);

        _uploaderMock
            .Setup(u => u.UploadVideoAsync(video, It.IsAny<IProgress<UploadProgressNotification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(YouTubeUploadResult.Succeeded("yt_abc123"));

        var processor = new UploadQueueProcessor(
            _queueRepoMock.Object,
            _uploaderMock.Object,
            _queueServiceMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<UploadQueueProcessor>.Instance);

        // Act
        await processor.ProcessQueueAsync(CancellationToken.None);

        // Assert
        queueItem.Status.Should().Be(UploadStatus.Completed);
        queueItem.CompletedAtUtc.Should().NotBeNull();
        video.YouTubeVideoId.Should().Be("yt_abc123");
        _queueRepoMock.Verify(r => r.UpdateAsync(queueItem, It.IsAny<CancellationToken>()), Times.AtLeast(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task ProcessQueueAsync_WhenUploadFails_UpdatesQueueItemToFailed()
    {
        // Arrange
        var video = new VideoRecord
        {
            Id = Guid.NewGuid(),
            FileName = "replay.mp4",
            FilePath = @"C:\replays\replay.mp4"
        };

        var queueItem = new UploadQueueItem
        {
            Id = Guid.NewGuid(),
            VideoRecordId = video.Id,
            VideoRecord = video,
            Status = UploadStatus.Queued
        };

        _queueRepoMock
            .SetupSequence(r => r.GetNextPendingItemAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(queueItem)
            .ReturnsAsync((UploadQueueItem?)null);

        _uploaderMock
            .Setup(u => u.UploadVideoAsync(video, It.IsAny<IProgress<UploadProgressNotification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(YouTubeUploadResult.Failed("Quota exceeded"));

        var processor = new UploadQueueProcessor(
            _queueRepoMock.Object,
            _uploaderMock.Object,
            _queueServiceMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<UploadQueueProcessor>.Instance);

        // Act
        await processor.ProcessQueueAsync(CancellationToken.None);

        // Assert
        queueItem.Status.Should().Be(UploadStatus.Failed);
        queueItem.LastError.Should().Be("Quota exceeded");
        _queueRepoMock.Verify(r => r.UpdateAsync(queueItem, It.IsAny<CancellationToken>()), Times.AtLeast(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }
}
