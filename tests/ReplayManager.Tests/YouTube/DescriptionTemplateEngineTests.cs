using FluentAssertions;
using ReplayManager.Application.Services;
using ReplayManager.Domain.Entities;

namespace ReplayManager.Tests.YouTube;

public class DescriptionTemplateEngineTests
{
    private readonly DescriptionTemplateEngine _engine = new();

    [Fact]
    public void RenderDescription_ReplacesAllSupportedVariables()
    {
        var recordedAt = new DateTime(2026, 10, 31, 10, 30, 0, DateTimeKind.Utc);
        var expectedLocal = recordedAt.ToLocalTime();

        var video = new VideoRecord
        {
            FileName = "pubg_match.mp4",
            FilePath = @"C:\Replays\pubg_match.mp4",
            RecordedAtUtc = recordedAt,
            DurationSeconds = 125, // 02:05
            ResolutionWidth = 1920,
            ResolutionHeight = 1080,
            FileSizeBytes = 25L * 1024 * 1024 // 25 MB
        };

        const string template = """
        Vídeo: {FileName}
        Pasta: {FolderName}
        Data: {Date} às {Time}
        Duração: {Duration}
        Resolução: {Resolution}
        Tamanho: {FileSize}
        """;

        var result = _engine.RenderDescription(template, video, "Replays PUBG");

        result.Should().Contain("Vídeo: pubg_match.mp4");
        result.Should().Contain("Pasta: Replays PUBG");
        result.Should().Contain($"Data: {expectedLocal:dd/MM/yyyy} às {expectedLocal:HH:mm:ss}");
        result.Should().Contain("Duração: 02:05");
        result.Should().Contain("Resolução: 1920x1080");
        result.Should().Contain("Tamanho: 25.0 MB");
    }

    [Fact]
    public void RenderDescription_EmptyTemplate_ReturnsEmptyString()
    {
        var video = new VideoRecord { FileName = "v.mp4" };
        _engine.RenderDescription("", video).Should().BeEmpty();
    }
}

