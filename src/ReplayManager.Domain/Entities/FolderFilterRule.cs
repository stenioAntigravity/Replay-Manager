using ReplayManager.Domain.Common;

namespace ReplayManager.Domain.Entities;

/// <summary>
/// Regras de validação e critérios automáticos aplicados aos arquivos de uma pasta monitorada.
/// </summary>
public class FolderFilterRule : BaseEntity
{
    /// <summary>
    /// ID da pasta monitorada a que esta regra pertence.
    /// </summary>
    public Guid MonitoredFolderId { get; set; }

    /// <summary>
    /// Referência de navegação para a pasta monitorada.
    /// </summary>
    public MonitoredFolder? MonitoredFolder { get; set; }

    /// <summary>
    /// Lista de extensões permitidas separadas por ponto e vírgula (ex: ".mp4;.mkv;.avi").
    /// </summary>
    public string AllowedExtensions { get; set; } = ".mp4;.mkv;.avi";

    /// <summary>
    /// Tamanho mínimo do arquivo em bytes (padrão: 10 MB = 10.485.760 bytes).
    /// </summary>
    public long MinSizeBytes { get; set; } = 10L * 1024L * 1024L;

    /// <summary>
    /// Tamanho máximo do arquivo em bytes (padrão: 100 MB = 104.857.600 bytes).
    /// </summary>
    public long? MaxSizeBytes { get; set; } = 100L * 1024L * 1024L;

    /// <summary>
    /// Indica se o limite máximo de tamanho de arquivo está ativado.
    /// </summary>
    public bool IsMaxSizeEnabled { get; set; } = true;

    /// <summary>
    /// Duração mínima do vídeo em segundos (opcional).
    /// </summary>
    public int? MinDurationSeconds { get; set; }

    /// <summary>
    /// Duração máxima do vídeo em segundos (opcional).
    /// </summary>
    public int? MaxDurationSeconds { get; set; }

    /// <summary>
    /// Expressão regular opcional para validar ou filtrar nomes de arquivos.
    /// </summary>
    public string? NamePatternRegex { get; set; }

    /// <summary>
    /// Se verdadeiro, varre recursivamente as subpastas da pasta monitorada.
    /// </summary>
    public bool IncludeSubdirectories { get; set; } = false;
}
