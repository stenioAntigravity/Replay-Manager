using FluentAssertions;
using ReplayManager.Application.Models.FileSystem;
using ReplayManager.Domain.Entities;
using ReplayManager.Infrastructure.FileSystem;

namespace ReplayManager.Tests.FileSystem;

public class FileSystemWatcherServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileSystemWatcherService _watcherService;

    public FileSystemWatcherServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ReplayManager_WatcherTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _watcherService = new FileSystemWatcherService();
    }

    [Fact]
    public async Task StartMonitoring_WhenVideoFileCreated_DispatchesFileDiscoveredEvent()
    {
        // Arrange
        var folder = new MonitoredFolder
        {
            Id = Guid.NewGuid(),
            Name = "Replays Test",
            Path = _tempDir,
            IsActive = true
        };

        var tcs = new TaskCompletionSource<FileDetectedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        _watcherService.FileDiscovered += (_, e) =>
        {
            if (e.FilePath.EndsWith("clip.mp4", StringComparison.OrdinalIgnoreCase))
            {
                tcs.TrySetResult(e);
            }
        };

        _watcherService.StartMonitoring(folder);
        _watcherService.IsMonitoring(folder.Id).Should().BeTrue();

        // Act
        var targetFile = Path.Combine(_tempDir, "clip.mp4");
        await File.WriteAllTextAsync(targetFile, "dummy video content");

        // Wait with timeout
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(4000));

        // Assert
        completedTask.Should().Be(tcs.Task, "O evento FileDiscovered deveria ter sido disparado após debounce.");
        var eventArgs = await tcs.Task;
        eventArgs.MonitoredFolderId.Should().Be(folder.Id);
        eventArgs.FilePath.Should().Be(targetFile);
    }

    [Fact]
    public async Task StartMonitoring_WhenNonVideoFileCreated_DoesNotDispatchEvent()
    {
        // Arrange
        var folder = new MonitoredFolder
        {
            Id = Guid.NewGuid(),
            Name = "Replays Test",
            Path = _tempDir,
            IsActive = true
        };

        var discovered = false;
        _watcherService.FileDiscovered += (_, _) => discovered = true;

        _watcherService.StartMonitoring(folder);

        // Act
        var textFile = Path.Combine(_tempDir, "notes.txt");
        await File.WriteAllTextAsync(textFile, "some notes");
        await Task.Delay(2000);

        // Assert
        discovered.Should().BeFalse("Arquivos .txt não são vídeos válidos e devem ser ignorados.");
    }

    [Fact]
    public void StopMonitoring_StopsTrackingFolder()
    {
        // Arrange
        var folder = new MonitoredFolder
        {
            Id = Guid.NewGuid(),
            Name = "Replays Test",
            Path = _tempDir,
            IsActive = true
        };

        _watcherService.StartMonitoring(folder);
        _watcherService.IsMonitoring(folder.Id).Should().BeTrue();

        // Act
        _watcherService.StopMonitoring(folder.Id);

        // Assert
        _watcherService.IsMonitoring(folder.Id).Should().BeFalse();
    }

    public void Dispose()
    {
        _watcherService.Dispose();
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

