using CommunityToolkit.Mvvm.ComponentModel;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.App.ViewModels;

/// <summary>
/// ViewModel representativo de um cartão individual de vídeo na grade da biblioteca.
/// </summary>
public partial class VideoCardViewModel : ObservableObject
{
    public VideoRecord Model { get; }

    public Guid Id => Model.Id;
    public string FileName => Model.FileName;
    public string FilePath => Model.FilePath;
    public long FileSizeBytes => Model.FileSizeBytes;
    public double? DurationSeconds => Model.DurationSeconds;
    public int? ResolutionWidth => Model.ResolutionWidth;
    public int? ResolutionHeight => Model.ResolutionHeight;
    public DateTime? RecordedAtUtc => Model.RecordedAtUtc;

    [ObservableProperty]
    private string? _customTitle;

    [ObservableProperty]
    private string? _customDescription;

    [ObservableProperty]
    private string? _thumbnailPath;

    [ObservableProperty]
    private LocalFileStatus _localStatus;

    [ObservableProperty]
    private UploadStatus _uploadStatus;

    [ObservableProperty]
    private string? _youTubeVideoId;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _isSelected;

    public VideoCardViewModel(VideoRecord model)
    {
        Model = model;
        _customTitle = model.CustomTitle;
        _customDescription = model.CustomDescription;
        _thumbnailPath = model.ThumbnailPath;
        _localStatus = model.LocalStatus;
        _uploadStatus = model.UploadStatus;
        _youTubeVideoId = model.YouTubeVideoId;
        _isFavorite = model.IsFavorite;
    }

    /// <summary>
    /// Retorna a duração formatada em MM:SS ou HH:MM:SS.
    /// </summary>
    public string FormattedDuration
    {
        get
        {
            if (!DurationSeconds.HasValue || DurationSeconds.Value <= 0)
                return "--:--";

            var time = TimeSpan.FromSeconds(DurationSeconds.Value);
            return time.TotalHours >= 1
                ? $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}"
                : $"{time.Minutes:D2}:{time.Seconds:D2}";
        }
    }

    /// <summary>
    /// Retorna o tamanho em formato legível (MB / GB).
    /// </summary>
    public string FormattedFileSize
    {
        get
        {
            if (FileSizeBytes < 1024 * 1024)
                return $"{FileSizeBytes / 1024.0:F1} KB";
            if (FileSizeBytes < 1024L * 1024 * 1024)
                return $"{FileSizeBytes / (1024.0 * 1024):F1} MB";
            return $"{FileSizeBytes / (1024.0 * 1024 * 1024):F2} GB";
        }
    }

    /// <summary>
    /// Resolução legível (ex: "1920x1080").
    /// </summary>
    public string FormattedResolution =>
        ResolutionWidth.HasValue && ResolutionHeight.HasValue
            ? $"{ResolutionWidth}x{ResolutionHeight}"
            : "Desconhecida";

    /// <summary>
    /// Data local formatada da gravação ou cadastro.
    /// </summary>
    public string FormattedRecordedAt =>
        (RecordedAtUtc ?? Model.CreatedAtUtc).ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    /// <summary>
    /// Título em exibição (customizado se houver, caso contrário nome do arquivo).
    /// </summary>
    public string DisplayTitle =>
        !string.IsNullOrWhiteSpace(CustomTitle) ? CustomTitle : FileName;

    /// <summary>
    /// Texto legível para o badge de status do vídeo.
    /// </summary>
    public string StatusBadgeText
    {
        get
        {
            if (LocalStatus == LocalFileStatus.Missing) return "Arquivo Ausente";
            return UploadStatus switch
            {
                UploadStatus.Completed => "Enviado",
                UploadStatus.Uploading => "Enviando...",
                UploadStatus.Queued => "Na Fila",
                UploadStatus.Preparing => "Preparando...",
                UploadStatus.Paused => "Pausado",
                UploadStatus.Failed => "Falha no Envio",
                UploadStatus.Interrupted => "Interrompido",
                _ => "Local"
            };
        }
    }

    /// <summary>
    /// Cor hexadecimal de destaque do badge.
    /// </summary>
    public string StatusBadgeColor
    {
        get
        {
            if (LocalStatus == LocalFileStatus.Missing) return "#E53935"; // Vermelho
            return UploadStatus switch
            {
                UploadStatus.Completed => "#43A047", // Verde
                UploadStatus.Uploading => "#FB8C00", // Laranja
                UploadStatus.Queued => "#FDD835",    // Amarelo
                UploadStatus.Failed => "#E53935",    // Vermelho
                UploadStatus.Interrupted => "#8E24AA", // Roxo
                _ => "#1E88E5"                      // Azul (Local)
            };
        }
    }

    public bool IsMissingOnDisk => LocalStatus == LocalFileStatus.Missing;
    public bool HasLocalFile => LocalStatus == LocalFileStatus.Available;
    public bool HasYouTubeVideo => !string.IsNullOrWhiteSpace(YouTubeVideoId);
    public bool HasBoth => HasLocalFile && HasYouTubeVideo;
    public string? YouTubeUrl => HasYouTubeVideo ? (Model.YouTubeUrl ?? $"https://youtu.be/{YouTubeVideoId}") : null;

    public string RecordedTimeFormatted =>
        (RecordedAtUtc ?? Model.CreatedAtUtc).ToLocalTime().ToString("HH:mm");

    public string RecordedDateFormatted =>
        (RecordedAtUtc ?? Model.CreatedAtUtc).ToLocalTime().ToString("dd-MM-yyyy");
}
