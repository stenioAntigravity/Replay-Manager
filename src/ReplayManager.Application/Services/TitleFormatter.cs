using System.Globalization;
using System.Text.RegularExpressions;
using ReplayManager.Application.Contracts.YouTube;
using ReplayManager.Domain.Entities;

namespace ReplayManager.Application.Services;

/// <summary>
/// Gerador canônico de títulos para replays e destaques do PUBG.
/// Formato canônico: "PUBG Replay - DD/MM/AAAA - HH:MM:SS"
/// </summary>
public class TitleFormatter : ITitleFormatter
{
    private static readonly Regex PubgDatePattern = new(
        @"(\d{4})[-_.](\d{2})[-_.](\d{2})[-_.](\d{2})[-_.](\d{2})[-_.](\d{2})",
        RegexOptions.Compiled);

    public string FormatTitle(VideoRecord video)
    {
        ArgumentNullException.ThrowIfNull(video);

        DateTime date;

        if (TryExtractTimestampFromFileName(video.FileName, out var extracted))
        {
            date = extracted;
        }
        else
        {
            date = (video.RecordedAtUtc ?? video.CreatedAtUtc).ToLocalTime();
        }

        var formattedDate = date.ToString("dd/MM/yyyy - HH:mm:ss", CultureInfo.InvariantCulture);
        return $"PUBG Replay - {formattedDate}";
    }

    public bool TryExtractTimestampFromFileName(string fileName, out DateTime timestamp)
    {
        timestamp = default;
        if (string.IsNullOrWhiteSpace(fileName)) return false;

        var match = PubgDatePattern.Match(fileName);
        if (match.Success)
        {
            var year = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var day = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            var hour = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
            var minute = int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);
            var second = int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture);

            try
            {
                timestamp = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        return false;
    }
}

