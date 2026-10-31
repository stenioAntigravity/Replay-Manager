using ReplayManager.Domain.Entities;

namespace ReplayManager.Application.Contracts.YouTube;

/// <summary>
/// Contrato para o motor de substituição de variáveis dinâmicas em modelos de descrição.
/// </summary>
public interface IDescriptionTemplateEngine
{
    /// <summary>
    /// Renderiza o texto final da descrição aplicando as variáveis dinâmicas do vídeo.
    /// Variáveis suportadas: {FileName}, {Date}, {Time}, {Duration}, {Resolution}, {FileSize}, {FolderName}.
    /// </summary>
    /// <param name="template">Template com tags.</param>
    /// <param name="video">Vídeo de origem.</param>
    /// <param name="folderName">Nome opcional da pasta monitorada.</param>
    /// <returns>Texto renderizado.</returns>
    string RenderDescription(string template, VideoRecord video, string? folderName = null);
}

