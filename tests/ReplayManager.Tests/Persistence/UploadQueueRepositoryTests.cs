using FluentAssertions;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using ReplayManager.Infrastructure.Repositories;
using Xunit;

namespace ReplayManager.Tests.Persistence;

public class UploadQueueRepositoryTests : IDisposable
{
    private readonly TestDatabaseFixture _fixture;

    public UploadQueueRepositoryTests()
    {
        _fixture = new TestDatabaseFixture();
    }

    [Fact]
    public async Task GetNextPendingItemAsync_ShouldReturnItemWithLowestPriorityAndOldestQueuedAt()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var folder = new MonitoredFolder { Name = "Queue Folder", Path = "C:\\Queue" };
        context.MonitoredFolders.Add(folder);
        await context.SaveChangesAsync();

        var v1 = new VideoRecord { MonitoredFolderId = folder.Id, FileName = "v1.mp4", FilePath = "C:\\v1.mp4", ContentSha256 = "h1", FileSizeBytes = 100 };
        var v2 = new VideoRecord { MonitoredFolderId = folder.Id, FileName = "v2.mp4", FilePath = "C:\\v2.mp4", ContentSha256 = "h2", FileSizeBytes = 100 };
        var v3 = new VideoRecord { MonitoredFolderId = folder.Id, FileName = "v3.mp4", FilePath = "C:\\v3.mp4", ContentSha256 = "h3", FileSizeBytes = 100 };
        context.Videos.AddRange(v1, v2, v3);
        await context.SaveChangesAsync();

        var itemNormalPriority = new UploadQueueItem
        {
            VideoRecordId = v1.Id,
            Priority = 2,
            Status = UploadStatus.Queued,
            QueuedAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };
        var itemHighPriority = new UploadQueueItem
        {
            VideoRecordId = v2.Id,
            Priority = 1, // Menor valor = Maior prioridade
            Status = UploadStatus.Queued,
            QueuedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        };
        var itemCompleted = new UploadQueueItem
        {
            VideoRecordId = v3.Id,
            Priority = 0,
            Status = UploadStatus.Completed,
            QueuedAtUtc = DateTime.UtcNow.AddMinutes(-20)
        };

        context.UploadQueue.AddRange(itemNormalPriority, itemHighPriority, itemCompleted);
        await context.SaveChangesAsync();

        var repo = new UploadQueueRepository(context);

        // Act
        var nextItem = await repo.GetNextPendingItemAsync();

        // Assert
        nextItem.Should().NotBeNull();
        nextItem!.Id.Should().Be(itemHighPriority.Id);
    }

    [Fact]
    public async Task ResetActiveUploadsToInterruptedAsync_ShouldMarkUploadingAndPreparingItemsAsInterrupted()
    {
        // Arrange (Seção 4.1 e 11: Recuperação de estado após queda de energia ou fechamento forçado)
        using var context = _fixture.CreateContext();
        var folder = new MonitoredFolder { Name = "F", Path = "C:\\F" };
        context.MonitoredFolders.Add(folder);
        await context.SaveChangesAsync();

        var v1 = new VideoRecord { MonitoredFolderId = folder.Id, FileName = "v1.mp4", FilePath = "C:\\v1.mp4", ContentSha256 = "h1", FileSizeBytes = 100 };
        var v2 = new VideoRecord { MonitoredFolderId = folder.Id, FileName = "v2.mp4", FilePath = "C:\\v2.mp4", ContentSha256 = "h2", FileSizeBytes = 100 };
        var v3 = new VideoRecord { MonitoredFolderId = folder.Id, FileName = "v3.mp4", FilePath = "C:\\v3.mp4", ContentSha256 = "h3", FileSizeBytes = 100 };
        context.Videos.AddRange(v1, v2, v3);
        await context.SaveChangesAsync();

        var itemUploading = new UploadQueueItem { VideoRecordId = v1.Id, Status = UploadStatus.Uploading };
        var itemPreparing = new UploadQueueItem { VideoRecordId = v2.Id, Status = UploadStatus.Preparing };
        var itemQueued = new UploadQueueItem { VideoRecordId = v3.Id, Status = UploadStatus.Queued };

        context.UploadQueue.AddRange(itemUploading, itemPreparing, itemQueued);
        await context.SaveChangesAsync();

        var repo = new UploadQueueRepository(context);

        // Act
        await repo.ResetActiveUploadsToInterruptedAsync();
        await context.SaveChangesAsync();

        // Assert
        var updatedUploading = await context.UploadQueue.FindAsync(itemUploading.Id);
        var updatedPreparing = await context.UploadQueue.FindAsync(itemPreparing.Id);
        var updatedQueued = await context.UploadQueue.FindAsync(itemQueued.Id);

        updatedUploading!.Status.Should().Be(UploadStatus.Interrupted);
        updatedUploading.LastError.Should().NotBeNullOrEmpty();

        updatedPreparing!.Status.Should().Be(UploadStatus.Interrupted);
        updatedPreparing.LastError.Should().NotBeNullOrEmpty();

        updatedQueued!.Status.Should().Be(UploadStatus.Queued);
    }

    public void Dispose()
    {
        _fixture.Dispose();
        GC.SuppressFinalize(this);
    }
}
