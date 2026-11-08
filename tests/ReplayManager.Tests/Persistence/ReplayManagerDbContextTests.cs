using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using Xunit;

namespace ReplayManager.Tests.Persistence;

public class ReplayManagerDbContextTests : IDisposable
{
    private readonly TestDatabaseFixture _fixture;

    public ReplayManagerDbContextTests()
    {
        _fixture = new TestDatabaseFixture();
    }

    [Fact]
    public async Task SameHash_InSameFolder_ShouldThrowException_WhenDuplicateInserted()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var folder = new MonitoredFolder { Name = "Pasta PUBG 1", Path = "C:\\Videos\\PUBG1" };
        context.MonitoredFolders.Add(folder);
        await context.SaveChangesAsync();

        var hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        var video1 = new VideoRecord
        {
            MonitoredFolderId = folder.Id,
            FileName = "video1.mp4",
            FilePath = "C:\\Videos\\PUBG1\\video1.mp4",
            ContentSha256 = hash,
            FileSizeBytes = 50 * 1024 * 1024
        };
        context.Videos.Add(video1);
        await context.SaveChangesAsync();

        var video2DuplicateInSameFolder = new VideoRecord
        {
            MonitoredFolderId = folder.Id,
            FileName = "video1_renamed.mp4",
            FilePath = "C:\\Videos\\PUBG1\\video1_renamed.mp4",
            ContentSha256 = hash,
            FileSizeBytes = 50 * 1024 * 1024
        };
        context.Videos.Add(video2DuplicateInSameFolder);

        // Act
        var act = async () => await context.SaveChangesAsync();

        // Assert: Deve falhar devido ao índice único IX_Videos_MonitoredFolderId_ContentSha256
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SameHash_InDifferentFolders_ShouldBeAllowed_AndTreatedAsDistinctRecords()
    {
        // Arrange (Seção 5.1 e ADR 003: Arquivos em pastas diferentes são registros diferentes)
        using var context = _fixture.CreateContext();
        var folder1 = new MonitoredFolder { Name = "Pasta Replays", Path = "C:\\Videos\\Replays" };
        var folder2 = new MonitoredFolder { Name = "Pasta Clipes", Path = "C:\\Videos\\Clipes" };
        context.MonitoredFolders.AddRange(folder1, folder2);
        await context.SaveChangesAsync();

        var sameHash = "a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e";
        var videoInFolder1 = new VideoRecord
        {
            MonitoredFolderId = folder1.Id,
            FileName = "kill_clip.mp4",
            FilePath = "C:\\Videos\\Replays\\kill_clip.mp4",
            ContentSha256 = sameHash,
            FileSizeBytes = 25 * 1024 * 1024
        };
        var videoInFolder2 = new VideoRecord
        {
            MonitoredFolderId = folder2.Id,
            FileName = "kill_clip_copy.mp4",
            FilePath = "C:\\Videos\\Clipes\\kill_clip_copy.mp4",
            ContentSha256 = sameHash,
            FileSizeBytes = 25 * 1024 * 1024
        };

        // Act
        context.Videos.AddRange(videoInFolder1, videoInFolder2);
        var saveResult = await context.SaveChangesAsync();

        // Assert
        saveResult.Should().Be(2);

        var count = await context.Videos.CountAsync(v => v.ContentSha256 == sameHash);
        count.Should().Be(2);
    }

    [Fact]
    public async Task DeletingMonitoredFolder_ShouldCascadeDelete_AssociatedVideosAndFilterRules()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var folder = new MonitoredFolder
        {
            Name = "Pasta Temporaria",
            Path = "C:\\TempVideos",
            FilterRule = new FolderFilterRule { MinSizeBytes = 20 * 1024 * 1024 }
        };
        context.MonitoredFolders.Add(folder);
        await context.SaveChangesAsync();

        var video = new VideoRecord
        {
            MonitoredFolderId = folder.Id,
            FileName = "gameplay.mp4",
            FilePath = "C:\\TempVideos\\gameplay.mp4",
            ContentSha256 = "1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
            FileSizeBytes = 40 * 1024 * 1024
        };
        context.Videos.Add(video);
        await context.SaveChangesAsync();

        // Act
        context.MonitoredFolders.Remove(folder);
        await context.SaveChangesAsync();

        // Assert
        var videosCount = await context.Videos.CountAsync(v => v.MonitoredFolderId == folder.Id);
        videosCount.Should().Be(0);

        var rulesCount = await context.FolderFilterRules.CountAsync(r => r.MonitoredFolderId == folder.Id);
        rulesCount.Should().Be(0);
    }

    [Fact]
    public async Task VideoLocalStatus_CanBeChangedToMissing_PreservingYouTubeInfoAndMetadata()
    {
        // Arrange (Seção 7.1: Preservar registro mesmo se o arquivo local for apagado)
        using var context = _fixture.CreateContext();
        var folder = new MonitoredFolder { Name = "Pasta Teste", Path = "C:\\Videos" };
        context.MonitoredFolders.Add(folder);
        await context.SaveChangesAsync();

        var video = new VideoRecord
        {
            MonitoredFolderId = folder.Id,
            FileName = "winner.mp4",
            FilePath = "C:\\Videos\\winner.mp4",
            ContentSha256 = "fedcba0987654321fedcba0987654321fedcba0987654321fedcba0987654321",
            FileSizeBytes = 80 * 1024 * 1024,
            LocalStatus = LocalFileStatus.Available,
            UploadStatus = UploadStatus.Completed,
            YouTubeVideoId = "abc123xyz",
            YouTubeUrl = "https://youtu.be/abc123xyz",
            ThumbnailPath = "C:\\AppData\\Thumbnails\\thumb1.jpg"
        };
        context.Videos.Add(video);
        await context.SaveChangesAsync();

        // Act: Arquivo local foi deletado externamente
        video.LocalStatus = LocalFileStatus.Missing;
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.Videos.FindAsync(video.Id);
        retrieved.Should().NotBeNull();
        retrieved!.LocalStatus.Should().Be(LocalFileStatus.Missing);
        retrieved.UploadStatus.Should().Be(UploadStatus.Completed);
        retrieved.YouTubeVideoId.Should().Be("abc123xyz");
        retrieved.YouTubeUrl.Should().Be("https://youtu.be/abc123xyz");
        retrieved.ThumbnailPath.Should().Be("C:\\AppData\\Thumbnails\\thumb1.jpg");
    }

    public void Dispose()
    {
        _fixture.Dispose();
        GC.SuppressFinalize(this);
    }
}

