using FluentAssertions;
using ReplayManager.Infrastructure.FFmpeg;

namespace ReplayManager.Tests.FFmpeg;

public class FFmpegThumbnailGeneratorTests
{
    private readonly FFmpegThumbnailGenerator _generator;

    public FFmpegThumbnailGeneratorTests()
    {
        var resolver = new FFmpegPathResolver();
        _generator = new FFmpegThumbnailGenerator(resolver);
    }

    [Theory]
    [InlineData(100.0, 0.15, 15.0)]
    [InlineData(60.0, 0.15, 9.0)]
    [InlineData(10.0, 0.15, 1.5)]
    [InlineData(200.0, 0.10, 20.0)]
    public void CalculateThumbnailTimestamp_StandardVideos_CalculatesExactPercentage(
        double duration, double percentage, double expected)
    {
        var ts = _generator.CalculateThumbnailTimestamp(duration, percentage);
        ts.Should().BeApproximately(expected, 0.001);
    }

    [Theory]
    [InlineData(2.0, 1.0)]
    [InlineData(1.5, 0.75)]
    [InlineData(2.8, 1.4)]
    public void CalculateThumbnailTimestamp_VeryShortVideos_CapturesMiddleFrame(
        double duration, double expected)
    {
        var ts = _generator.CalculateThumbnailTimestamp(duration);
        ts.Should().BeApproximately(expected, 0.001);
    }

    [Fact]
    public void CalculateThumbnailTimestamp_ZeroOrNegativeDuration_ReturnsZero()
    {
        _generator.CalculateThumbnailTimestamp(0).Should().Be(0);
        _generator.CalculateThumbnailTimestamp(-5).Should().Be(0);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_NonExistentVideo_ReturnsFailedResult()
    {
        var result = await _generator.GenerateThumbnailAsync("C:\\non_existent_folder\\video.mp4", "thumb.jpg");
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("não encontrado");
    }

    [Fact]
    public async Task ReplaceThumbnailAsync_ValidImage_CopiesToTargetDirectoryAndReturnsPath()
    {
        // Arrange
        var tempSource = Path.Combine(Path.GetTempPath(), $"source_thumb_{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(tempSource, [1, 2, 3, 4, 5]);
        var videoId = Guid.NewGuid();

        try
        {
            // Act
            var path = await _generator.ReplaceThumbnailAsync(tempSource, videoId);

            // Assert
            path.Should().NotBeNullOrWhiteSpace();
            File.Exists(path).Should().BeTrue();
            path.Should().EndWith($"{videoId}.png");
            var bytes = await File.ReadAllBytesAsync(path);
            bytes.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5 });

            // Cleanup target file
            File.Delete(path);
        }
        finally
        {
            if (File.Exists(tempSource)) File.Delete(tempSource);
        }
    }

    [Fact]
    public async Task ReplaceThumbnailAsync_NonExistentImage_ThrowsFileNotFoundException()
    {
        var act = () => _generator.ReplaceThumbnailAsync("C:\\non_existent_image.jpg", Guid.NewGuid());
        await act.Should().ThrowAsync<FileNotFoundException>();
    }
}

