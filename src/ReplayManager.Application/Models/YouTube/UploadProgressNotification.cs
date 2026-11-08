namespace ReplayManager.Application.Models.YouTube;

/// <summary>
/// Notificação de progresso da transmissão de bytes de um vídeo em upload.
/// </summary>
public class UploadProgressNotification
{
    public Guid VideoId { get; set; }
    public long BytesSent { get; set; }
    public long TotalBytes { get; set; }
    public double Percentage => TotalBytes > 0 ? (double)BytesSent / TotalBytes * 100.0 : 0.0;
    public string Status { get; set; } = string.Empty;
}

