using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.Backup;
using ReplayManager.Application.Models.Backup;
using ReplayManager.Infrastructure.Security;

namespace ReplayManager.Infrastructure.Backup;

/// <summary>
/// Implementação do serviço de backup e retenção de arquivos no Google Drive via Google Drive API v3.
/// Suporta autenticação OAuth com escopo DriveFile (arquivos criados pelo app) e verificação de integridade pré-upload.
/// </summary>
public class GoogleDriveBackupService : IGoogleDriveBackupService
{
    private static readonly string[] DriveScopes =
    [
        DriveService.Scope.DriveFile
    ];

    private readonly ISqliteBackupService _sqliteBackupService;
    private readonly DpapiCredentialStorage _dpapiStorage;
    private readonly ILogger<GoogleDriveBackupService>? _logger;
    private readonly string _tokenDirectory;

    public GoogleDriveBackupService(
        ISqliteBackupService sqliteBackupService,
        DpapiCredentialStorage dpapiStorage,
        ILogger<GoogleDriveBackupService>? logger = null,
        string? tokenDirectory = null)
    {
        _sqliteBackupService = sqliteBackupService;
        _dpapiStorage = dpapiStorage;
        _logger = logger;
        _tokenDirectory = tokenDirectory ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ReplayManager",
                "GoogleTokensDrive");

        if (!Directory.Exists(_tokenDirectory))
        {
            Directory.CreateDirectory(_tokenDirectory);
        }
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var credential = await GetUserCredentialAsync(cancellationToken);
        if (credential == null) return false;

        if (credential.Token.IsStale)
        {
            try
            {
                return await credential.RefreshTokenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Falha ao renovar token OAuth do Google Drive.");
                return false;
            }
        }

        return true;
    }

    public async Task<BackupMetadata> UploadBackupAsync(string backupFilePath, string folderName = "ReplayManager_Backups", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupFilePath);

        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException($"Arquivo de backup não encontrado em '{backupFilePath}'.");
        }

        // 1. Verificação prévia de integridade SQLite
        var (isValid, message) = await _sqliteBackupService.VerifyDatabaseIntegrityAsync(backupFilePath, cancellationToken);
        if (!isValid)
        {
            throw new InvalidOperationException($"O arquivo de backup '{backupFilePath}' não passou na verificação de integridade SQLite: {message}");
        }

        var credential = await GetUserCredentialAsync(cancellationToken)
            ?? throw new InvalidOperationException("Usuário não autenticado no Google Drive. Autentique primeiro.");

        using var driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ReplayManager"
        });

        // 2. Localizar ou criar a pasta de backup de destino no Google Drive
        var folderId = await GetOrCreateFolderAsync(driveService, folderName, cancellationToken);

        var fileInfo = new FileInfo(backupFilePath);
        var driveFile = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileInfo.Name,
            Description = $"ReplayManager SQLite Database Backup gerado em {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            Parents = new List<string> { folderId }
        };

        _logger?.LogInformation("Iniciando upload de backup para o Google Drive: {File} ({Bytes} bytes)", fileInfo.Name, fileInfo.Length);

        using var fileStream = File.OpenRead(backupFilePath);
        var createRequest = driveService.Files.Create(driveFile, fileStream, "application/x-sqlite3");
        createRequest.Fields = "id, name, size, createdTime";

        var uploadProgress = await createRequest.UploadAsync(cancellationToken);
        if (uploadProgress.Status != Google.Apis.Upload.UploadStatus.Completed)
        {
            throw new IOException($"Upload do backup para o Google Drive falhou: {uploadProgress.Exception?.Message ?? uploadProgress.Status.ToString()}", uploadProgress.Exception);
        }

        var uploadedFile = createRequest.ResponseBody;
        _logger?.LogInformation("Backup enviado para o Google Drive com sucesso. DriveFileId: {Id}", uploadedFile?.Id);

        return new BackupMetadata
        {
            FileName = fileInfo.Name,
            LocalFilePath = backupFilePath,
            FileSizeBytes = fileInfo.Length,
            CreatedUtc = uploadedFile?.CreatedTimeDateTimeOffset?.UtcDateTime ?? DateTime.UtcNow,
            DriveFileId = uploadedFile?.Id,
            IsConsistent = true,
            Details = $"Backup enviado com sucesso para a pasta '{folderName}' no Google Drive."
        };
    }

    public async Task<int> EnforceRetentionAsync(int keepCount = 5, string folderName = "ReplayManager_Backups", CancellationToken cancellationToken = default)
    {
        if (keepCount <= 0) keepCount = 5;

        var credential = await GetUserCredentialAsync(cancellationToken);
        if (credential == null) return 0;

        using var driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ReplayManager"
        });

        var folderId = await FindFolderIdAsync(driveService, folderName, cancellationToken);
        if (folderId == null) return 0;

        // Listar todos os arquivos na pasta ordenados por createdTime desc
        var listRequest = driveService.Files.List();
        listRequest.Q = $"'{folderId}' in parents and trashed = false";
        listRequest.OrderBy = "createdTime desc";
        listRequest.Fields = "files(id, name, createdTime)";

        var response = await listRequest.ExecuteAsync(cancellationToken);
        var files = response.Files;
        if (files == null || files.Count <= keepCount)
        {
            return 0;
        }

        var filesToDelete = files.Skip(keepCount).ToList();
        var deletedCount = 0;

        foreach (var file in filesToDelete)
        {
            try
            {
                await driveService.Files.Delete(file.Id).ExecuteAsync(cancellationToken);
                deletedCount++;
                _logger?.LogInformation("Backup antigo excluído por retenção no Google Drive: {Name} (Id: {Id})", file.Name, file.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Erro ao excluir backup antigo no Google Drive: {Id}", file.Id);
            }
        }

        return deletedCount;
    }

    public async Task<IReadOnlyList<BackupMetadata>> ListRemoteBackupsAsync(string folderName = "ReplayManager_Backups", CancellationToken cancellationToken = default)
    {
        var credential = await GetUserCredentialAsync(cancellationToken);
        if (credential == null) return Array.Empty<BackupMetadata>();

        using var driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ReplayManager"
        });

        var folderId = await FindFolderIdAsync(driveService, folderName, cancellationToken);
        if (folderId == null) return Array.Empty<BackupMetadata>();

        var listRequest = driveService.Files.List();
        listRequest.Q = $"'{folderId}' in parents and trashed = false";
        listRequest.OrderBy = "createdTime desc";
        listRequest.Fields = "files(id, name, size, createdTime)";

        var response = await listRequest.ExecuteAsync(cancellationToken);
        var list = new List<BackupMetadata>();

        if (response.Files != null)
        {
            foreach (var f in response.Files)
            {
                list.Add(new BackupMetadata
                {
                    FileName = f.Name,
                    DriveFileId = f.Id,
                    FileSizeBytes = f.Size ?? 0,
                    CreatedUtc = f.CreatedTimeDateTimeOffset?.UtcDateTime ?? DateTime.UtcNow,
                    IsConsistent = true,
                    Details = "Armazenado no Google Drive."
                });
            }
        }

        return list;
    }

    private async Task<string> GetOrCreateFolderAsync(DriveService service, string folderName, CancellationToken cancellationToken)
    {
        var existingFolderId = await FindFolderIdAsync(service, folderName, cancellationToken);
        if (existingFolderId != null) return existingFolderId;

        var folderMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Description = "Pasta dedicada para backups automáticos do Replay Manager"
        };

        var createReq = service.Files.Create(folderMetadata);
        createReq.Fields = "id";
        var created = await createReq.ExecuteAsync(cancellationToken);

        _logger?.LogInformation("Pasta '{Folder}' criada no Google Drive com ID: {Id}", folderName, created.Id);
        return created.Id;
    }

    private async Task<string?> FindFolderIdAsync(DriveService service, string folderName, CancellationToken cancellationToken)
    {
        var listReq = service.Files.List();
        listReq.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}' and trashed = false";
        listReq.Fields = "files(id, name)";

        var res = await listReq.ExecuteAsync(cancellationToken);
        return res.Files?.FirstOrDefault()?.Id;
    }

    private async Task<UserCredential?> GetUserCredentialAsync(CancellationToken cancellationToken)
    {
        // Se a pasta de tokens não contém token persistido, não dispara AuthorizeAsync para evitar abrir o navegador automaticamente
        var tokenFile = Path.Combine(_tokenDirectory, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user");
        if (!File.Exists(tokenFile))
        {
            return null;
        }

        var secrets = await GetClientSecretsAsync(cancellationToken);
        if (secrets == null) return null;

        try
        {
            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                DriveScopes,
                "user",
                cancellationToken,
                new FileDataStore(_tokenDirectory, true));
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Não foi possível carregar credencial salva do Google Drive.");
            return null;
        }
    }

    private async Task<ClientSecrets?> GetClientSecretsAsync(CancellationToken cancellationToken)
    {
        var json = await _dpapiStorage.ReadSecretAsync("GoogleClientSecrets", cancellationToken);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                return GoogleClientSecrets.FromStream(stream).Secrets;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Erro ao ler GoogleClientSecrets protegido via DPAPI.");
            }
        }

        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(appDir, "client_secrets.json"),
            Path.Combine(appDir, "credentials.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReplayManager", "credentials.json")
        };

        foreach (var file in candidates)
        {
            if (File.Exists(file))
            {
                using var stream = File.OpenRead(file);
                return GoogleClientSecrets.FromStream(stream).Secrets;
            }
        }

        return null;
    }
}

