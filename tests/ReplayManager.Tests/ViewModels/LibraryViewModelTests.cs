using FluentAssertions;
using Moq;
using ReplayManager.App.Services;
using ReplayManager.App.ViewModels;
using ReplayManager.Application.Contracts.Media;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using ReplayManager.Infrastructure.Repositories;
using ReplayManager.Tests.Persistence;

namespace ReplayManager.Tests.ViewModels;

public class LibraryViewModelTests : IDisposable
{
    private readonly TestDatabaseFixture _fixture;
    private readonly Mock<IMediaPlayerLauncher> _mediaPlayerMock;
    private readonly Mock<IThumbnailGenerator> _thumbnailGeneratorMock;
    private readonly Mock<IDialogService> _dialogServiceMock;

    public LibraryViewModelTests()
    {
        _fixture = new TestDatabaseFixture();
        _mediaPlayerMock = new Mock<IMediaPlayerLauncher>();
        _thumbnailGeneratorMock = new Mock<IThumbnailGenerator>();
        _dialogServiceMock = new Mock<IDialogService>();
    }

    [Fact]
    public async Task LoadVideosAsync_PopulatesFilteredVideos_AndSetsFirstAsSelected()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);

        var folder = new MonitoredFolder { Id = Guid.NewGuid(), Name = "Pastas", Path = @"C:\Replays" };
        await uow.MonitoredFolders.AddAsync(folder);

        var v1 = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "replay_01.mp4",
            FilePath = @"C:\Replays\replay_01.mp4",
            ContentSha256 = "hash1",
            FileSizeBytes = 1000,
            DurationSeconds = 60,
            LocalStatus = LocalFileStatus.Available,
            UploadStatus = UploadStatus.None
        };

        var v2 = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "replay_02.mp4",
            FilePath = @"C:\Replays\replay_02.mp4",
            ContentSha256 = "hash2",
            FileSizeBytes = 2000,
            DurationSeconds = 120,
            LocalStatus = LocalFileStatus.Available,
            UploadStatus = UploadStatus.Completed
        };

        await uow.Videos.AddAsync(v1);
        await uow.Videos.AddAsync(v2);
        await uow.SaveChangesAsync();

        var vm = new LibraryViewModel(uow, _mediaPlayerMock.Object, _thumbnailGeneratorMock.Object, _dialogServiceMock.Object);

        // Act
        await vm.LoadVideosAsync();

        // Assert
        vm.FilteredVideos.Should().HaveCount(2);
        vm.SelectedVideo.Should().NotBeNull();
        vm.SelectedVideo!.Id.Should().Be(v2.Id); // v2 mais recente por padrão
        vm.StatusMessage.Should().Contain("2 vídeo(s)");
    }

    [Fact]
    public async Task SearchText_FiltersVideosByFileNameOrCustomTitle()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);

        var folder = new MonitoredFolder { Id = Guid.NewGuid(), Name = "Pastas", Path = @"C:\Replays" };
        await uow.MonitoredFolders.AddAsync(folder);

        var v1 = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "pubg_match_chicken.mp4",
            FilePath = @"C:\Replays\pubg_match_chicken.mp4",
            ContentSha256 = "hash1",
            CustomTitle = "Vitória Épica",
            FileSizeBytes = 1000
        };

        var v2 = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "pubg_fail.mp4",
            FilePath = @"C:\Replays\pubg_fail.mp4",
            ContentSha256 = "hash2",
            FileSizeBytes = 1000
        };

        await uow.Videos.AddAsync(v1);
        await uow.Videos.AddAsync(v2);
        await uow.SaveChangesAsync();

        var vm = new LibraryViewModel(uow, _mediaPlayerMock.Object, _thumbnailGeneratorMock.Object, _dialogServiceMock.Object);
        await vm.LoadVideosAsync();

        // Act - Buscar por termo presente no CustomTitle
        vm.SearchText = "épica";

        // Assert
        vm.FilteredVideos.Should().HaveCount(1);
        vm.FilteredVideos.First().FileName.Should().Be("pubg_match_chicken.mp4");

        // Act 2 - Buscar por termo presente no FileName
        vm.SearchText = "fail";
        vm.FilteredVideos.Should().HaveCount(1);
        vm.FilteredVideos.First().FileName.Should().Be("pubg_fail.mp4");
    }

    [Fact]
    public async Task CurrentFilter_FiltersByStatusCorrectly()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);

        var folder = new MonitoredFolder { Id = Guid.NewGuid(), Name = "Pastas", Path = @"C:\Replays" };
        await uow.MonitoredFolders.AddAsync(folder);

        var vLocal = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "local.mp4",
            FilePath = @"C:\Replays\local.mp4",
            ContentSha256 = "h1",
            LocalStatus = LocalFileStatus.Available,
            UploadStatus = UploadStatus.None
        };

        var vQueued = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "queued.mp4",
            FilePath = @"C:\Replays\queued.mp4",
            ContentSha256 = "h2",
            LocalStatus = LocalFileStatus.Available,
            UploadStatus = UploadStatus.Queued
        };

        var vUploaded = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "uploaded.mp4",
            FilePath = @"C:\Replays\uploaded.mp4",
            ContentSha256 = "h3",
            LocalStatus = LocalFileStatus.Available,
            UploadStatus = UploadStatus.Completed
        };

        var vMissing = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "missing.mp4",
            FilePath = @"C:\Replays\missing.mp4",
            ContentSha256 = "h4",
            LocalStatus = LocalFileStatus.Missing,
            UploadStatus = UploadStatus.None
        };

        await uow.Videos.AddAsync(vLocal);
        await uow.Videos.AddAsync(vQueued);
        await uow.Videos.AddAsync(vUploaded);
        await uow.Videos.AddAsync(vMissing);
        await uow.SaveChangesAsync();

        var vm = new LibraryViewModel(uow, _mediaPlayerMock.Object, _thumbnailGeneratorMock.Object, _dialogServiceMock.Object);
        await vm.LoadVideosAsync();

        // 1. Apenas local
        vm.CurrentFilter = LibraryFilterOption.OnlyLocal;
        vm.FilteredVideos.Should().ContainSingle(v => v.FileName == "local.mp4");

        // 2. Na Fila
        vm.CurrentFilter = LibraryFilterOption.Queued;
        vm.FilteredVideos.Should().ContainSingle(v => v.FileName == "queued.mp4");

        // 3. Enviados
        vm.CurrentFilter = LibraryFilterOption.Uploaded;
        vm.FilteredVideos.Should().ContainSingle(v => v.FileName == "uploaded.mp4");

        // 4. Arquivo Ausente
        vm.CurrentFilter = LibraryFilterOption.MissingFile;
        vm.FilteredVideos.Should().ContainSingle(v => v.FileName == "missing.mp4");
    }

    [Fact]
    public async Task PlayVideo_WhenVideoIsMissing_ShowsWarningAndDoesNotLaunchPlayer()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);

        var video = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = Guid.NewGuid(),
            FileName = "missing.mp4",
            FilePath = @"C:\Replays\missing.mp4",
            ContentSha256 = "hash",
            LocalStatus = LocalFileStatus.Missing
        };

        var cardVm = new VideoCardViewModel(video);
        var vm = new LibraryViewModel(uow, _mediaPlayerMock.Object, _thumbnailGeneratorMock.Object, _dialogServiceMock.Object)
        {
            SelectedVideo = cardVm
        };

        // Act
        vm.PlayVideo(cardVm);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mediaPlayerMock.Verify(m => m.PlayVideo(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveMetadataChangesAsync_UpdatesDatabaseSuccessfully()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var uow = new UnitOfWork(context);

        var folder = new MonitoredFolder { Id = Guid.NewGuid(), Name = "P", Path = @"C:\Replays" };
        await uow.MonitoredFolders.AddAsync(folder);

        var video = new VideoRecord
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            FileName = "raw.mp4",
            FilePath = @"C:\Replays\raw.mp4",
            ContentSha256 = "h"
        };
        await uow.Videos.AddAsync(video);
        await uow.SaveChangesAsync();

        var vm = new LibraryViewModel(uow, _mediaPlayerMock.Object, _thumbnailGeneratorMock.Object, _dialogServiceMock.Object);
        await vm.LoadVideosAsync();

        vm.SelectedVideo = vm.FilteredVideos.First();
        vm.EditTitle = "Título Alterado Manualmente";
        vm.EditDescription = "Descrição rica com #tags";

        // Act
        await vm.SaveMetadataChangesAsync();

        // Assert
        var updated = await uow.Videos.GetByIdAsync(video.Id);
        updated!.CustomTitle.Should().Be("Título Alterado Manualmente");
        updated.CustomDescription.Should().Be("Descrição rica com #tags");
        _dialogServiceMock.Verify(d => d.ShowInformation(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}

