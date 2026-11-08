using FluentAssertions;
using ReplayManager.Application.Services;
using ReplayManager.Domain.Entities;

namespace ReplayManager.Tests.YouTube;

public class TitleFormatterTests
{
    private readonly TitleFormatter _formatter = new();

    [Fact]
    public void FormatTitle_WhenFileNameContainsPubgTimestamp_ExtractsDateCorrectly()
    {
        // Padrão PUBG: ano-mes-dia-hora-min-seg
        var video = new VideoRecord
        {
            FileName = "PUBG_2026.10.31-15.45.30.mp4",
            FilePath = @"C:\Replays\PUBG_2026.10.31-15.45.30.mp4"
        };

        var title = _formatter.FormatTitle(video);

        title.Should().Be("PUBG Replay - 31/10/2026 - 15:45:30");
    }

    [Fact]
    public void FormatTitle_WhenFileNameHasDashSeparators_ExtractsDateCorrectly()
    {
        var video = new VideoRecord
        {
            FileName = "Replay_2026-05-12-20-10-05.mkv",
            FilePath = @"C:\Replays\Replay_2026-05-12-20-10-05.mkv"
        };

        var title = _formatter.FormatTitle(video);

        title.Should().Be("PUBG Replay - 12/05/2026 - 20:10:05");
    }

    [Fact]
    public void FormatTitle_WhenNoTimestampInFileName_UsesRecordedAtUtc()
    {
        var recordedAt = new DateTime(2026, 8, 20, 14, 30, 0, DateTimeKind.Utc);
        var expectedLocal = recordedAt.ToLocalTime();

        var video = new VideoRecord
        {
            FileName = "highlight_clip.mp4",
            FilePath = @"C:\Replays\highlight_clip.mp4",
            RecordedAtUtc = recordedAt
        };

        var title = _formatter.FormatTitle(video);

        title.Should().Be($"PUBG Replay - {expectedLocal:dd/MM/yyyy - HH:mm:ss}");
    }
}

