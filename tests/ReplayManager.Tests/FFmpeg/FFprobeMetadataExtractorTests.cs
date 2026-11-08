using FluentAssertions;
using ReplayManager.Infrastructure.FFmpeg;

namespace ReplayManager.Tests.FFmpeg;

public class FFprobeMetadataExtractorTests
{
    private readonly FFprobeMetadataExtractor _extractor;

    public FFprobeMetadataExtractorTests()
    {
        var resolver = new FFmpegPathResolver();
        _extractor = new FFprobeMetadataExtractor(resolver);
    }

    [Fact]
    public void ParseFFprobeJson_WithStandard1080pVideo_ExtractsAllFieldsCorrectly()
    {
        // Arrange
        const string json = """
        {
            "format": {
                "duration": "125.450000",
                "bit_rate": "15420000",
                "size": "241800000",
                "format_name": "mov,mp4,m4a,3gp,3g2,mj2"
            },
            "streams": [
                {
                    "codec_type": "video",
                    "codec_name": "h264",
                    "width": 1920,
                    "height": 1080,
                    "r_frame_rate": "60/1",
                    "avg_frame_rate": "60/1",
                    "duration": "125.450000",
                    "bit_rate": "15000000"
                },
                {
                    "codec_type": "audio",
                    "codec_name": "aac",
                    "bit_rate": "320000"
                }
            ]
        }
        """;

        // Act
        var metadata = _extractor.ParseFFprobeJson(json, "test.mp4");

        // Assert
        metadata.Should().NotBeNull();
        metadata!.DurationSeconds.Should().BeApproximately(125.45, 0.001);
        metadata.Width.Should().Be(1920);
        metadata.Height.Should().Be(1080);
        metadata.Resolution.Should().Be("1920x1080");
        metadata.VideoCodec.Should().Be("h264");
        metadata.BitrateBps.Should().Be(15420000);
        metadata.FrameRate.Should().Be(60.0);
    }

    [Fact]
    public void ParseFFprobeJson_WithFractionalFramerate_ComputesCorrectFps()
    {
        // Arrange (29.97 fps comum: 30000/1001)
        const string json = """
        {
            "format": {
                "duration": "45.000000",
                "bit_rate": "8000000"
            },
            "streams": [
                {
                    "codec_type": "video",
                    "codec_name": "hevc",
                    "width": 2560,
                    "height": 1440,
                    "r_frame_rate": "30000/1001",
                    "avg_frame_rate": "30000/1001"
                }
            ]
        }
        """;

        // Act
        var metadata = _extractor.ParseFFprobeJson(json, "2k_clip.mp4");

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Width.Should().Be(2560);
        metadata.Height.Should().Be(1440);
        metadata.VideoCodec.Should().Be("hevc");
        metadata.FrameRate.Should().BeApproximately(29.97, 0.01);
    }

    [Fact]
    public void ParseFFprobeJson_WithAudioOnlyStream_ReturnsEmptyVideoFields()
    {
        // Arrange
        const string json = """
        {
            "format": {
                "duration": "30.000000",
                "bit_rate": "128000"
            },
            "streams": [
                {
                    "codec_type": "audio",
                    "codec_name": "mp3"
                }
            ]
        }
        """;

        // Act
        var metadata = _extractor.ParseFFprobeJson(json, "audio.mp3");

        // Assert
        metadata.Should().NotBeNull();
        metadata!.DurationSeconds.Should().Be(30.0);
        metadata.Width.Should().Be(0);
        metadata.Height.Should().Be(0);
        metadata.VideoCodec.Should().BeNull();
    }

    [Fact]
    public void ParseFFprobeJson_WithInvalidJson_ReturnsNull()
    {
        // Arrange
        const string invalidJson = "{ corrupted json format ...";

        // Act
        var metadata = _extractor.ParseFFprobeJson(invalidJson, "corrupt.mp4");

        // Assert
        metadata.Should().BeNull();
    }

    [Theory]
    [InlineData("60/1", 60.0)]
    [InlineData("30/1", 30.0)]
    [InlineData("30000/1001", 29.970029)]
    [InlineData("24000/1001", 23.976023)]
    [InlineData("50/1", 50.0)]
    [InlineData("0/0", 0.0)]
    [InlineData("", 0.0)]
    [InlineData(null, 0.0)]
    public void TryParseFrameRate_HandlesVariousFormats(string? input, double expected)
    {
        var success = FFprobeMetadataExtractor.TryParseFrameRate(input, out var fps);

        if (expected > 0)
        {
            success.Should().BeTrue();
            fps.Should().BeApproximately(expected, 0.001);
        }
        else
        {
            success.Should().BeFalse();
        }
    }
}

