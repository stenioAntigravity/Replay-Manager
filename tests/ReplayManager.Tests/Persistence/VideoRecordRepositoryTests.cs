using FluentAssertions;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using ReplayManager.Infrastructure.Repositories;
using Xunit;

namespace ReplayManager.Tests.Persistence;

public class VideoRecordRepositoryTests : IDisposable
{
    private readonly TestDatabaseFixture _fixture;

    public VideoRecordRepositoryTests()
    {
        _fixture = new TestDatabaseFixture();
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_ShouldFilterByFileNameOrCustomTitle()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var folder = new MonitoredFolder { Name = "Main", Path = "C:\\Videos" };
        context.MonitoredFolders.Add(folder);
        await context.SaveChangesAsync();

        var v1 = new VideoRecord
        {
            MonitoredFolderId = folder.Id,
            FileName = "PUBG_ChickenDinner_2026.mp4",
            FilePath = "C:\\Videos\\PUBG_ChickenDinner_2026.mp4",
            ContentSha256 = "hash1",
            FileSizeBytes = 1000
        };
        var v2 = new VideoRecord
        {
            MonitoredFolderId = folder.Id,
            FileName = "random_match.mp4",
            CustomTitle = "Melhor jogada do PUBG",
            FilePath = "C:\\Videos\\random_match.mp4",
            ContentSha256 = "hash2",
            FileSizeBytes = 2000
        };
        var v3 = new VideoRecord
        {
            MonitoredFolderId = folder.Id,
            FileName = "cs2_clutch.mp4",
            FilePath = "C:\\Videos\\cs2_clutch.mp4",
            ContentSha256 = "hash3",
            FileSizeBytes = 3000
        };
        context.Videos.AddRange(v1, v2, v3);
        await context.SaveChangesAsync();

        var repo = new VideoRecordRepository(context);

        // Act
        var results = await repo.GetAllAsync(searchTerm: "pubg");

        // Assert
        results.Should().HaveCount(2);
        results.Select(r => r.Id).Should().Contain(new[] { v1.Id, v2.Id });
        results.Select(r => r.Id).Should().NotContain(v3.Id);
    }

    [Fact]
    public async Task ExistsInFolderAsync_ShouldReturnTrue_OnlyForMatchingFolderAndHash()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var folder1 = new MonitoredFolder { Name = "F1", Path = "C:\\F1" };
        var folder2 = new MonitoredFolder { Name = "F2", Path = "C:\\F2" };
        context.MonitoredFolders.AddRange(folder1, folder2);
        await context.SaveChangesAsync();

        var hash = "unique_hash_12345";
        var video = new VideoRecord
        {
            MonitoredFolderId = folder1.Id,
            FileName = "v.mp4",
            FilePath = "C:\\F1\\v.mp4",
            ContentSha256 = hash,
            FileSizeBytes = 500
        };
        context.Videos.Add(video);
        await context.SaveChangesAsync();

        var repo = new VideoRecordRepository(context);

        // Act & Assert
        var existsInF1 = await repo.ExistsInFolderAsync(folder1.Id, hash);
        var existsInF2 = await repo.ExistsInFolderAsync(folder2.Id, hash);

        existsInF1.Should().BeTrue();
        existsInF2.Should().BeFalse();
    }

    public void Dispose()
    {
        _fixture.Dispose();
        GC.SuppressFinalize(this);
    }
}

