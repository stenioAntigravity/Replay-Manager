using CommunityToolkit.Mvvm.ComponentModel;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.App.ViewModels;

/// <summary>
/// Representa um item da fila de upload na interface WPF com suporte a atualização reativa.
/// </summary>
public partial class UploadQueueItemViewModel : ObservableObject
{
    public UploadQueueItem Model { get; }

    public Guid Id => Model.Id;
    public Guid VideoRecordId => Model.VideoRecordId;
    public string FileName => Model.VideoRecord?.FileName ?? "Vídeo";
    public string FilePath => Model.VideoRecord?.FilePath ?? string.Empty;
    public string? ThumbnailPath => Model.VideoRecord?.ThumbnailPath;

    [ObservableProperty]
    private int _priority;

    [ObservableProperty]
    private UploadStatus _status;

    [ObservableProperty]
    private long _bytesUploaded;

    [ObservableProperty]
    private long _totalBytes;

    [ObservableProperty]
    private double _percentage;

    [ObservableProperty]
    private long _uploadSpeedBytesPerSec;

    [ObservableProperty]
    private string? _lastError;

    [ObservableProperty]
    private string? _youTubeUrl;

    public string FormattedTotalSize => FormatBytes(TotalBytes);
    public string FormattedUploadedSize => FormatBytes(BytesUploaded);
    public string FormattedSpeed => UploadSpeedBytesPerSec > 0 ? $"{FormatBytes(UploadSpeedBytesPerSec)}/s" : "-- KB/s";

    public bool CanPause => Status == UploadStatus.Queued || Status == UploadStatus.Uploading || Status == UploadStatus.Preparing;
    public bool CanResume => Status == UploadStatus.Paused || Status == UploadStatus.Interrupted || Status == UploadStatus.Failed;
    public bool CanCancel => Status != UploadStatus.Completed;

    public UploadQueueItemViewModel(UploadQueueItem model)
    {
        Model = model;
        _priority = model.Priority;
        _status = model.Status;
        _bytesUploaded = model.BytesUploaded;
        _totalBytes = model.TotalBytes;
        _percentage = model.TotalBytes > 0 ? (double)model.BytesUploaded / model.TotalBytes * 100.0 : 0.0;
        _uploadSpeedBytesPerSec = model.UploadSpeedBytesPerSec;
        _lastError = model.LastError;
        _youTubeUrl = model.VideoRecord?.YouTubeUrl;
    }

    public void UpdateProgress(long bytesUploaded, long totalBytes, double percentage, long speed)
    {
        BytesUploaded = bytesUploaded;
        TotalBytes = totalBytes > 0 ? totalBytes : TotalBytes;
        Percentage = percentage;
        UploadSpeedBytesPerSec = speed;
        OnPropertyChanged(nameof(FormattedUploadedSize));
        OnPropertyChanged(nameof(FormattedSpeed));
    }

    public void UpdateStatus(UploadStatus newStatus, string? error = null)
    {
        Status = newStatus;
        LastError = error;
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanResume));
        OnPropertyChanged(nameof(CanCancel));
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
