using ReplayManager.Domain.Common;

namespace ReplayManager.Domain.Entities;

/// <summary>
/// Representa uma pasta local configurada pelo usuário para monitoramento de vídeos.
/// </summary>
public class MonitoredFolder : BaseEntity
{
    /// <summary>
    /// Caminho absoluto da pasta monitorada no sistema operacional.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Nome amigável de exibição da pasta.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Indica se o monitoramento desta pasta está ativo no momento.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Se verdadeiro, novos vídeos aprovados pelos filtros são enfileirados automaticamente para upload (ex: pasta PUBG).
    /// </summary>
    public bool AutoUploadEnabled { get; set; } = false;

    /// <summary>
    /// Data/hora UTC da última varredura ou reconciliação de diretório realizada.
    /// </summary>
    public DateTime? LastScannedAtUtc { get; set; }

    /// <summary>
    /// Regras de filtro aplicáveis exclusivamente a esta pasta.
    /// </summary>
    public FolderFilterRule? FilterRule { get; set; }

    /// <summary>
    /// Coleção de modelos de descrição associados a esta pasta.
    /// </summary>
    public ICollection<DescriptionTemplate> DescriptionTemplates { get; set; } = new List<DescriptionTemplate>();

    /// <summary>
    /// Coleção de vídeos indexados nesta pasta monitorada.
    /// </summary>
    public ICollection<VideoRecord> Videos { get; set; } = new List<VideoRecord>();
}

