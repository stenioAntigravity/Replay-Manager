using ReplayManager.Domain.Entities;

namespace ReplayManager.Application.Models.Ingestion;

/// <summary>
/// Status da tentativa de ingestão de um arquivo de vídeo.
/// </summary>
public enum IngestionStatus
{
    /// <summary>
    /// Novo vídeo indexado com sucesso na biblioteca.
    /// </summary>
    Ingested,

    /// <summary>
    /// Arquivo idêntico (mesmo SHA-256) já registrado nesta mesma pasta. Ignorado.
    /// </summary>
    DuplicateSameFolder,

    /// <summary>
    /// Arquivo renomeado ou movido dentro da pasta. Registro existente atualizado com novo caminho.
    /// </summary>
    RenamedUpdated,

    /// <summary>
    /// O arquivo não se estabilizou no tempo limite (ainda sendo gravado ou inacessível).
    /// </summary>
    StabilityTimeout,

    /// <summary>
    /// Arquivo não encontrado no sistema de arquivos local.
    /// </summary>
    FileNotFound,

    /// <summary>
    /// Erro inesperado durante o processamento.
    /// </summary>
    Error
}

/// <summary>
/// Representa o resultado consolidado do processo de ingestão de um arquivo de vídeo.
/// </summary>
public class IngestionResult
{
    public IngestionStatus Status { get; set; }
    public VideoRecord? Video { get; set; }
    public string? Message { get; set; }

    public static IngestionResult Success(VideoRecord video) =>
        new() { Status = IngestionStatus.Ingested, Video = video, Message = "Vídeo indexado com sucesso." };

    public static IngestionResult Duplicate(VideoRecord video) =>
        new() { Status = IngestionStatus.DuplicateSameFolder, Video = video, Message = "Arquivo duplicado detectado na mesma pasta." };

    public static IngestionResult Renamed(VideoRecord video) =>
        new() { Status = IngestionStatus.RenamedUpdated, Video = video, Message = "Arquivo renomeado atualizado com sucesso." };

    public static IngestionResult StabilityFailed(string path) =>
        new() { Status = IngestionStatus.StabilityTimeout, Message = $"Arquivo em gravação ou inacessível: {path}" };

    public static IngestionResult NotFound(string path) =>
        new() { Status = IngestionStatus.FileNotFound, Message = $"Arquivo não encontrado no disco: {path}" };

    public static IngestionResult Failed(string error) =>
        new() { Status = IngestionStatus.Error, Message = error };
}

