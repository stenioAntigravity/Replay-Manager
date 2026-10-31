using FluentAssertions;
using ReplayManager.Application.Services;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.Tests.Filters;

public class FilterEvaluatorTests
{
    private readonly FilterEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_WithoutRule_AllowsStandardVideoExtensions()
    {
        var videoMp4 = new VideoRecord { FileName = "replay.mp4", FilePath = @"C:\Replays\replay.mp4" };
        var videoMkv = new VideoRecord { FileName = "replay.mkv", FilePath = @"C:\Replays\replay.mkv" };
        var videoAvi = new VideoRecord { FileName = "replay.avi", FilePath = @"C:\Replays\replay.avi" };
        var videoTxt = new VideoRecord { FileName = "notes.txt", FilePath = @"C:\Replays\notes.txt" };

        _evaluator.Evaluate(videoMp4, null).IsApproved.Should().BeTrue();
        _evaluator.Evaluate(videoMkv, null).IsApproved.Should().BeTrue();
        _evaluator.Evaluate(videoAvi, null).IsApproved.Should().BeTrue();

        var resultTxt = _evaluator.Evaluate(videoTxt, null);
        resultTxt.IsApproved.Should().BeFalse();
        resultTxt.Status.Should().Be(FilterEvaluationStatus.RejectedByExtension);
    }

    [Fact]
    public void Evaluate_WhenFileSizeBelowMinimum_RejectsWithMinSizeStatus()
    {
        var rule = new FolderFilterRule
        {
            MinSizeBytes = 10L * 1024 * 1024 // 10 MB
        };

        var video = new VideoRecord
        {
            FileName = "small.mp4",
            FilePath = @"C:\Replays\small.mp4",
            FileSizeBytes = 5L * 1024 * 1024 // 5 MB
        };

        var result = _evaluator.Evaluate(video, rule);

        result.IsApproved.Should().BeFalse();
        result.Status.Should().Be(FilterEvaluationStatus.RejectedByMinSize);
        result.Reason.Should().Contain("inferior ao mínimo");
    }

    [Fact]
    public void Evaluate_WhenFileSizeExceedsMaximumAndEnabled_RejectsWithMaxSizeStatus()
    {
        var rule = new FolderFilterRule
        {
            MinSizeBytes = 10L * 1024 * 1024,
            MaxSizeBytes = 100L * 1024 * 1024,
            IsMaxSizeEnabled = true
        };

        var video = new VideoRecord
        {
            FileName = "huge.mp4",
            FilePath = @"C:\Replays\huge.mp4",
            FileSizeBytes = 150L * 1024 * 1024 // 150 MB
        };

        var result = _evaluator.Evaluate(video, rule);

        result.IsApproved.Should().BeFalse();
        result.Status.Should().Be(FilterEvaluationStatus.RejectedByMaxSize);
        result.Reason.Should().Contain("excede o máximo");
    }

    [Fact]
    public void Evaluate_WhenMaxSizeDisabled_AllowsFilesLargerThanMaxSizeBytes()
    {
        var rule = new FolderFilterRule
        {
            MinSizeBytes = 10L * 1024 * 1024,
            MaxSizeBytes = 100L * 1024 * 1024,
            IsMaxSizeEnabled = false
        };

        var video = new VideoRecord
        {
            FileName = "huge.mp4",
            FilePath = @"C:\Replays\huge.mp4",
            FileSizeBytes = 500L * 1024 * 1024 // 500 MB
        };

        var result = _evaluator.Evaluate(video, rule);

        result.IsApproved.Should().BeTrue();
        result.Status.Should().Be(FilterEvaluationStatus.Approved);
    }

    [Fact]
    public void Evaluate_WhenDurationBelowMinimum_RejectsWithMinDurationStatus()
    {
        var rule = new FolderFilterRule
        {
            MinSizeBytes = 1024,
            MinDurationSeconds = 30 // Min 30s
        };

        var video = new VideoRecord
        {
            FileName = "short.mp4",
            FilePath = @"C:\Replays\short.mp4",
            FileSizeBytes = 10L * 1024 * 1024,
            DurationSeconds = 15.0 // 15s
        };

        var result = _evaluator.Evaluate(video, rule);

        result.IsApproved.Should().BeFalse();
        result.Status.Should().Be(FilterEvaluationStatus.RejectedByMinDuration);
        result.Reason.Should().Contain("inferior à duração mínima");
    }

    [Fact]
    public void Evaluate_WhenDurationExceedsMaximum_RejectsWithMaxDurationStatus()
    {
        var rule = new FolderFilterRule
        {
            MinSizeBytes = 1024,
            MaxDurationSeconds = 300 // Max 5 min
        };

        var video = new VideoRecord
        {
            FileName = "long.mp4",
            FilePath = @"C:\Replays\long.mp4",
            FileSizeBytes = 50L * 1024 * 1024,
            DurationSeconds = 350.0 // 350s
        };

        var result = _evaluator.Evaluate(video, rule);

        result.IsApproved.Should().BeFalse();
        result.Status.Should().Be(FilterEvaluationStatus.RejectedByMaxDuration);
        result.Reason.Should().Contain("excede a duração máxima");
    }

    [Fact]
    public void Evaluate_WithRegexPattern_ValidatesFileNameCorrectly()
    {
        var rule = new FolderFilterRule
        {
            MinSizeBytes = 1024,
            NamePatternRegex = @"^PUBG_.*\.(mp4|mkv)$"
        };

        var matchingVideo = new VideoRecord
        {
            FileName = "PUBG_ChickenDinner_2026.mp4",
            FilePath = @"C:\Replays\PUBG_ChickenDinner_2026.mp4",
            FileSizeBytes = 20L * 1024 * 1024
        };

        var nonMatchingVideo = new VideoRecord
        {
            FileName = "Valorant_Clip.mp4",
            FilePath = @"C:\Replays\Valorant_Clip.mp4",
            FileSizeBytes = 20L * 1024 * 1024
        };

        _evaluator.Evaluate(matchingVideo, rule).IsApproved.Should().BeTrue();

        var resultNonMatch = _evaluator.Evaluate(nonMatchingVideo, rule);
        resultNonMatch.IsApproved.Should().BeFalse();
        resultNonMatch.Status.Should().Be(FilterEvaluationStatus.RejectedByNamePattern);
    }

    [Fact]
    public void Evaluate_WhenSubdirectoriesNotAllowed_RejectsSubdirectoryFiles()
    {
        var rule = new FolderFilterRule
        {
            MinSizeBytes = 1024,
            IncludeSubdirectories = false
        };

        var folderPath = @"C:\Replays";

        var rootVideo = new VideoRecord
        {
            FileName = "root.mp4",
            FilePath = @"C:\Replays\root.mp4",
            FileSizeBytes = 20L * 1024 * 1024
        };

        var subfolderVideo = new VideoRecord
        {
            FileName = "sub.mp4",
            FilePath = @"C:\Replays\2026-10-31\sub.mp4",
            FileSizeBytes = 20L * 1024 * 1024
        };

        _evaluator.Evaluate(rootVideo, rule, folderPath).IsApproved.Should().BeTrue();

        var resultSub = _evaluator.Evaluate(subfolderVideo, rule, folderPath);
        resultSub.IsApproved.Should().BeFalse();
        resultSub.Status.Should().Be(FilterEvaluationStatus.RejectedByNamePattern);
        resultSub.Reason.Should().Contain("subpasta");
    }

    [Fact]
    public void Evaluate_WhenSubdirectoriesAllowed_ApprovesSubdirectoryFiles()
    {
        var rule = new FolderFilterRule
        {
            MinSizeBytes = 1024,
            IncludeSubdirectories = true
        };

        var folderPath = @"C:\Replays";

        var subfolderVideo = new VideoRecord
        {
            FileName = "sub.mp4",
            FilePath = @"C:\Replays\2026-10-31\sub.mp4",
            FileSizeBytes = 20L * 1024 * 1024
        };

        var result = _evaluator.Evaluate(subfolderVideo, rule, folderPath);
        result.IsApproved.Should().BeTrue();
    }
}

