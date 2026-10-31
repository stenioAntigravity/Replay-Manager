using ReplayManager.Domain.Entities;

namespace ReplayManager.Application.Contracts.YouTube;

/// <summary>
/// Contrato para geração e padronização automática de títulos de vídeos (PUBG Replays).
/// </summary>
public interface ITitleFormatter
{
    /// <summary>
    /// Gera um título no formato canônico "PUBG Replay - DD/MM/AAAA - HH:MM:SS"
    /// extraindo data/hora do nome do arquivo ou da data de gravação do registro.
    /// </summary>
    /// <param name="video">Registro do vídeo.</param>
    /// <returns>Título formatado legível.</returns>
    string FormatTitle(VideoRecord video);

    /// <summary>
    /// Tenta extrair a data/hora a partir de padrões comuns de nomenclatura de replays do PUBG.
    /// </summary>
    bool TryExtractTimestampFromFileName(string fileName, out DateTime timestamp);
}

