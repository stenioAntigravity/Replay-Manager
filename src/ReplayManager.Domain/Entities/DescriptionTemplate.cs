using ReplayManager.Domain.Common;

namespace ReplayManager.Domain.Entities;

/// <summary>
/// Modelo configurável de descrição com suporte a placeholders dinâmicos:
/// {Date}, {Time}, {Duration}, {OriginalFileName}, {FileSize}, {FolderName}.
/// </summary>
public class DescriptionTemplate : BaseEntity
{
    /// <summary>
    /// ID da pasta monitorada associada a este template, ou nulo para templates globais.
    /// </summary>
    public Guid? MonitoredFolderId { get; set; }

    /// <summary>
    /// Referência de navegação opcional para a pasta monitorada.
    /// </summary>
    public MonitoredFolder? MonitoredFolder { get; set; }

    /// <summary>
    /// Nome amigável do modelo (ex: "PUBG Padrão", "Clipes Curtos").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Conteúdo do modelo com variáveis substituíveis.
    /// </summary>
    public string TemplateContent { get; set; } = string.Empty;

    /// <summary>
    /// Indica se este modelo é o padrão para a pasta monitorada associada (ou padrão global).
    /// </summary>
    public bool IsDefault { get; set; } = false;
}
