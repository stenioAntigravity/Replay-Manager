using ReplayManager.Domain.Common;

namespace ReplayManager.Domain.Entities;

/// <summary>
/// Par de chave/valor persistente para preferências do usuário e configurações gerais do sistema.
/// </summary>
public class AppSetting : BaseEntity
{
    /// <summary>
    /// Chave identificadora única da configuração (ex: "CloseAction", "UploadSpeedLimitMbps", "StartWithWindows").
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Valor serializado em texto (string, JSON, int, bool).
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da utilidade desta configuração.
    /// </summary>
    public string? Description { get; set; }
}

