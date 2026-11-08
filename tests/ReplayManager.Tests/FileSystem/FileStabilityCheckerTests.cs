using FluentAssertions;
using ReplayManager.Infrastructure.FileSystem;

namespace ReplayManager.Tests.FileSystem;

public class FileStabilityCheckerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileStabilityChecker _checker;

    public FileStabilityCheckerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ReplayManager_StabilityTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _checker = new FileStabilityChecker();
    }

    [Fact]
    public void IsFileLocked_WhenFileIsNotLocked_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "free.mp4");
        File.WriteAllText(filePath, "video content data");

        // Act
        var isLocked = _checker.IsFileLocked(filePath);

        // Assert
        isLocked.Should().BeFalse();
    }

    [Fact]
    public void IsFileLocked_WhenFileIsHeldExclusively_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "locked.mp4");
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        stream.Write([1, 2, 3, 4]);
        stream.Flush();

        // Act
        var isLocked = _checker.IsFileLocked(filePath);

        // Assert
        isLocked.Should().BeTrue();
    }

    [Fact]
    public async Task WaitForStabilityAsync_WhenFileAlreadyStable_ReturnsTrueQuickly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "stable.mp4");
        await File.WriteAllBytesAsync(filePath, [10, 20, 30, 40]);

        // Act
        var isStable = await _checker.WaitForStabilityAsync(
            filePath,
            stabilityDuration: TimeSpan.FromMilliseconds(200),
            timeout: TimeSpan.FromSeconds(2));

        // Assert
        isStable.Should().BeTrue();
    }

    [Fact]
    public async Task WaitForStabilityAsync_WhenFileIsContinuouslyLocked_TimesOutAndReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "continuous_lock.mp4");
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        stream.Write([1, 2, 3]);
        stream.Flush();

        // Act
        var isStable = await _checker.WaitForStabilityAsync(
            filePath,
            stabilityDuration: TimeSpan.FromMilliseconds(200),
            timeout: TimeSpan.FromMilliseconds(600));

        // Assert
        isStable.Should().BeFalse();
    }

    [Fact]
    public async Task WaitForStabilityAsync_WhenRecordingCompletesDuringWait_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "recording_sim.mp4");
        var activeStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        activeStream.Write([1, 2, 3, 4, 5]);
        activeStream.Flush();

        // Simula processo gravando por 400ms e depois fechando
        _ = Task.Run(async () =>
        {
            await Task.Delay(400);
            activeStream.Write([6, 7, 8]);
            activeStream.Flush();
            await activeStream.DisposeAsync();
        });

        // Act
        var isStable = await _checker.WaitForStabilityAsync(
            filePath,
            stabilityDuration: TimeSpan.FromMilliseconds(300),
            timeout: TimeSpan.FromSeconds(3));

        // Assert
        isStable.Should().BeTrue();
    }

    public void Dispose()
    {
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

