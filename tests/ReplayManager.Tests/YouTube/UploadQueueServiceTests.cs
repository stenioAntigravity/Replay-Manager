using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Application.Contracts.Upload;
using ReplayManager.Application.Services.Upload;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using ReplayManager.Infrastructure.YouTube;
using Xunit;

namespace ReplayManager.Tests.YouTube;

public class UploadQueueServiceTests
{
    private readonly Mock<IUploadQueueRepository> _queueRepoMock = new();
    private readonly Mock<IVideoRecordRepository> _videoRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task EnqueueVideoAsync_WhenVideoExists_CreatesAndPersistsQueueItem()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var video = new VideoRecord
        {
            Id = videoId,
            FileName = "highlight.mp4",
            FileSizeBytes = 50 * 1024 * 1024,
            UploadStatus = UploadStatus.None
        };

        _queueRepoMock.Setup(r => r.GetByVideoRecordIdAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UploadQueueItem?)null);

        _videoRepoMock.Setup(r => r.GetByIdAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(video);

        _queueRepoMock.Setup(r => r.GetMaxPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var service = new UploadQueueService(
            _queueRepoMock.Object,
            _videoRepoMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<UploadQueueService>.Instance);

        // Act
        var result = await service.EnqueueVideoAsync(videoId);

        // Assert
        result.Should().NotBeNull();
        result.VideoRecordId.Should().Be(videoId);
        result.Priority.Should().Be(3);
        result.Status.Should().Be(UploadStatus.Queued);
        result.TotalBytes.Should().Be(video.FileSizeBytes);

        _queueRepoMock.Verify(r => r.AddAsync(It.IsAny<UploadQueueItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _videoRepoMock.Verify(r => r.UpdateAsync(video, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PauseItemAsync_WhenItemExists_UpdatesStatusToPaused()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = new UploadQueueItem
        {
            Id = itemId,
            Status = UploadStatus.Queued
        };

        _queueRepoMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var service = new UploadQueueService(
            _queueRepoMock.Object,
            _videoRepoMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<UploadQueueService>.Instance);

        // Act
        var result = await service.PauseItemAsync(itemId);

        // Assert
        result.Should().BeTrue();
        item.Status.Should().Be(UploadStatus.Paused);
        _queueRepoMock.Verify(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResumeItemAsync_WhenPaused_UpdatesStatusToQueued()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = new UploadQueueItem
        {
            Id = itemId,
            Status = UploadStatus.Paused
        };

        _queueRepoMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var service = new UploadQueueService(
            _queueRepoMock.Object,
            _videoRepoMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<UploadQueueService>.Instance);

        // Act
        var result = await service.ResumeItemAsync(itemId);

        // Assert
        result.Should().BeTrue();
        item.Status.Should().Be(UploadStatus.Queued);
        _queueRepoMock.Verify(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelItemAsync_RemovesItemFromQueue_AndResetsVideoStatus()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var video = new VideoRecord { Id = Guid.NewGuid(), UploadStatus = UploadStatus.Queued };
        var item = new UploadQueueItem
        {
            Id = itemId,
            VideoRecord = video,
            Status = UploadStatus.Queued
        };

        _queueRepoMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var service = new UploadQueueService(
            _queueRepoMock.Object,
            _videoRepoMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<UploadQueueService>.Instance);

        // Act
        var result = await service.CancelItemAsync(itemId);

        // Assert
        result.Should().BeTrue();
        video.UploadStatus.Should().Be(UploadStatus.None);
        _queueRepoMock.Verify(r => r.RemoveAsync(itemId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ThrottledStream_EnforcesMaxBytesPerSecond()
    {
        // Arrange: 100 KB buffer with 50 KB/s limit -> deve demorar pelo menos ~800ms
        var payload = new byte[100 * 1024];
        new Random(42).NextBytes(payload);
        using var memStream = new MemoryStream(payload);

        const long limit = 50 * 1024; // 50 KB/s
        using var throttled = new ThrottledStream(memStream, limit);

        var buffer = new byte[4096];
        var sw = System.Diagnostics.Stopwatch.StartNew();

        int totalRead = 0;
        int read;
        while ((read = await throttled.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            totalRead += read;
        }

        sw.Stop();

        // Assert
        totalRead.Should().Be(payload.Length);
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(700);
    }
}
