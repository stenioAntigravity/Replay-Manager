using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReplayManager.App.Services;
using ReplayManager.Application.Contracts.Backup;
using ReplayManager.Application.Contracts.Export;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Application.Contracts.SystemIntegration;
using ReplayManager.Application.Contracts.Upload;
using ReplayManager.Application.Contracts.YouTube;
using ReplayManager.Application.Models.Backup;
using ReplayManager.Domain.Entities;
using ReplayManager.Infrastructure.Data;
using ReplayManager.Infrastructure.Security;

namespace ReplayManager.App.ViewModels;

public enum SettingsCategory
{
    GeneralAndFilters,
    UploadAndSpeed,
    Templates,
    YouTubeAccount,
    BackupAndSystem
}

public partial class FolderFilterConfigViewModel : ObservableObject
{
    private readonly MonitoredFolder _folder;
    private readonly FolderFilterRule _rule;

    public Guid FolderId => _folder.Id;
    public string FolderName => _folder.Name;
    public string FolderPath => _folder.Path;

    [ObservableProperty]
    private string _allowedExtensions;

    [ObservableProperty]
    private long _minSizeMb;

    [ObservableProperty]
    private long _maxSizeMb;

    [ObservableProperty]
    private bool _isMaxSizeEnabled;

    [ObservableProperty]
    private int? _minDurationSeconds;

    [ObservableProperty]
    private int? _maxDurationSeconds;

    [ObservableProperty]
    private string _namePatternRegex;

    [ObservableProperty]
    private bool _includeSubdirectories;

    public FolderFilterConfigViewModel(MonitoredFolder folder)
    {
        _folder = folder;
        _rule = folder.FilterRule ?? new FolderFilterRule
        {
            Id = Guid.NewGuid(),
            MonitoredFolderId = folder.Id,
            AllowedExtensions = ".mp4;.mkv;.avi",
            MinSizeBytes = 10L * 1024L * 1024L,
            MaxSizeBytes = 100L * 1024L * 1024L,
            IsMaxSizeEnabled = true
        };

        _allowedExtensions = _rule.AllowedExtensions;
        _minSizeMb = _rule.MinSizeBytes / (1024 * 1024);
        _maxSizeMb = (_rule.MaxSizeBytes ?? (100L * 1024L * 1024L)) / (1024 * 1024);
        _isMaxSizeEnabled = _rule.IsMaxSizeEnabled;
        _minDurationSeconds = _rule.MinDurationSeconds;
        _maxDurationSeconds = _rule.MaxDurationSeconds;
        _namePatternRegex = _rule.NamePatternRegex ?? string.Empty;
        _includeSubdirectories = _rule.IncludeSubdirectories;
    }

    public void ApplyToModel()
    {
        _rule.AllowedExtensions = AllowedExtensions;
        _rule.MinSizeBytes = Math.Max(0, MinSizeMb) * 1024 * 1024;
        _rule.MaxSizeBytes = Math.Max(0, MaxSizeMb) * 1024 * 1024;
        _rule.IsMaxSizeEnabled = IsMaxSizeEnabled;
        _rule.MinDurationSeconds = MinDurationSeconds;
        _rule.MaxDurationSeconds = MaxDurationSeconds;
        _rule.NamePatternRegex = string.IsNullOrWhiteSpace(NamePatternRegex) ? null : NamePatternRegex.Trim();
        _rule.IncludeSubdirectories = IncludeSubdirectories;

        _folder.FilterRule = _rule;
    }
}

/// <summary>
/// ViewModel da tela de Configurações organizada em Categorias (Fase 13):
/// 1. Filtros Globais e por Pasta (tamanho, duração, extensão, Regex)
/// 2. Upload, Velocidade e Rede (limite de banda, auto-upload)
/// 3. Templates de Descrição e Tags ({FileName}, {Date}, {Duration}, etc.)
/// 4. YouTube e Google OAuth (Canal conectado, status, importação de client_secrets.json)
/// 5. Backup, Nuvem e Integração com Windows (SQLite Backup, Retenção, Inicialização)
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISqliteBackupService _sqliteBackupService;
    private readonly IGoogleDriveBackupService _driveBackupService;
    private readonly IStartupRegistrationService _startupService;
    private readonly ILibraryExportService _exportService;
    private readonly IUploadQueueService _uploadQueueService;
    private readonly IYouTubeAuthService _youtubeAuthService;
    private readonly DpapiCredentialStorage _dpapiStorage;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SettingsViewModel>? _logger;

    [ObservableProperty]
    private SettingsCategory _selectedCategory = SettingsCategory.GeneralAndFilters;

    // --- Categoria 1: Filtros de Pastas ---
    [ObservableProperty]
    private ObservableCollection<FolderFilterConfigViewModel> _folderFilters = new();

    [ObservableProperty]
    private FolderFilterConfigViewModel? _selectedFolderFilter;

    // --- Categoria 2: Upload e Velocidade ---
    [ObservableProperty]
    private int _uploadSpeedLimitKbps;

    [ObservableProperty]
    private bool _isSpeedLimitEnabled;

    [ObservableProperty]
    private bool _globalAutoUploadPUBG;

    // --- Categoria 3: Templates de Descrição ---
    [ObservableProperty]
    private string _templateName = "Padrão PUBG Replay";

    [ObservableProperty]
    private string _templateContent = "PUBG Replay\nData: {Date} {Time}\nDuração: {Duration}\nArquivo: {OriginalFileName}\nTamanho: {FileSize}\nGravado em: {FolderName}";

    [ObservableProperty]
    private string _templatePreview = string.Empty;

    // --- Categoria 4: Conta YouTube ---
    [ObservableProperty]
    private bool _isYouTubeAuthenticated;

    [ObservableProperty]
    private string _youtubeChannelName = "Não conectado";

    [ObservableProperty]
    private string _youtubeChannelId = string.Empty;

    [ObservableProperty]
    private string? _youtubeThumbnailUrl;

    [ObservableProperty]
    private bool _hasClientSecretsConfigured;

    // --- Categoria 5: Backup e Sistema ---
    [ObservableProperty]
    private bool _isDriveAuthenticated;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private string _statusMessage = "Pronto";

    [ObservableProperty]
    private string _lastBackupInfo = "Nenhum backup realizado nesta sessão";

    [ObservableProperty]
    private int _retentionCount = 5;

    public ObservableCollection<BackupMetadata> RemoteBackups { get; } = new();

    public SettingsViewModel(
        IServiceScopeFactory scopeFactory,
        ISqliteBackupService sqliteBackupService,
        IGoogleDriveBackupService driveBackupService,
        IStartupRegistrationService startupService,
        ILibraryExportService exportService,
        IUploadQueueService uploadQueueService,
        IYouTubeAuthService youtubeAuthService,
        DpapiCredentialStorage dpapiStorage,
        IDialogService dialogService,
        ILogger<SettingsViewModel>? logger = null)
    {
        _scopeFactory = scopeFactory;
        _sqliteBackupService = sqliteBackupService;
        _driveBackupService = driveBackupService;
        _startupService = startupService;
        _exportService = exportService;
        _uploadQueueService = uploadQueueService;
        _youtubeAuthService = youtubeAuthService;
        _dpapiStorage = dpapiStorage;
        _dialogService = dialogService;
        _logger = logger;

        UpdateTemplatePreview();
    }

    [RelayCommand]
    public void SelectCategory(SettingsCategory category)
    {
        SelectedCategory = category;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsBusy = true;
        StatusMessage = "Carregando configurações...";

        try
        {
            // 1. Carrega inicialização do Windows
            StartWithWindows = _startupService.IsAutoStartEnabled();

            // 2. Carrega velocidade de upload
            var currentBytes = _uploadQueueService.BandwidthLimitBytesPerSecond;
            if (currentBytes > 0)
            {
                IsSpeedLimitEnabled = true;
                UploadSpeedLimitKbps = (int)(currentBytes / 1024);
            }
            else
            {
                IsSpeedLimitEnabled = false;
                UploadSpeedLimitKbps = 2048; // 2 MB/s padrão sugerido se ativado
            }

            // 3. Carrega filtros de pastas e template do banco
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ReplayManagerDbContext>();
                
                var folders = await dbContext.MonitoredFolders
                    .Include(f => f.FilterRule)
                    .ToListAsync();

                FolderFilters.Clear();
                foreach (var folder in folders)
                {
                    FolderFilters.Add(new FolderFilterConfigViewModel(folder));
                }
                SelectedFolderFilter = FolderFilters.FirstOrDefault();

                var defaultTpl = await dbContext.DescriptionTemplates.FirstOrDefaultAsync(t => t.IsDefault);
                if (defaultTpl != null)
                {
                    TemplateName = defaultTpl.Name;
                    TemplateContent = defaultTpl.TemplateContent;
                }
            }

            // 4. Carrega status YouTube & Google Secrets
            var secretJson = await _dpapiStorage.ReadSecretAsync("GoogleClientSecrets");
            HasClientSecretsConfigured = !string.IsNullOrWhiteSpace(secretJson);

            IsYouTubeAuthenticated = await _youtubeAuthService.IsAuthenticatedAsync();
            if (IsYouTubeAuthenticated)
            {
                var profile = await _youtubeAuthService.GetChannelProfileAsync();
                YoutubeChannelName = string.IsNullOrWhiteSpace(profile.ChannelTitle) ? "Conta Google Conectada" : profile.ChannelTitle;
                YoutubeChannelId = profile.ChannelId ?? string.Empty;
                YoutubeThumbnailUrl = profile.ThumbnailUrl;
            }
            else
            {
                YoutubeChannelName = "Não conectado";
                YoutubeChannelId = string.Empty;
                YoutubeThumbnailUrl = null;
            }

            // 5. Carrega status Google Drive Backup
            IsDriveAuthenticated = await _driveBackupService.IsAuthenticatedAsync();
            if (IsDriveAuthenticated)
            {
                await RefreshRemoteBackupsAsync();
            }

            UpdateTemplatePreview();
            StatusMessage = "Configurações carregadas.";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao carregar configurações.");
            StatusMessage = $"Aviso ao verificar configurações: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        _startupService.SetAutoStart(value);
    }

    partial void OnTemplateContentChanged(string value)
    {
        UpdateTemplatePreview();
    }

    private void UpdateTemplatePreview()
    {
        TemplatePreview = (TemplateContent ?? string.Empty)
            .Replace("{Date}", DateTime.Now.ToString("dd/MM/yyyy"))
            .Replace("{Time}", DateTime.Now.ToString("HH:mm:ss"))
            .Replace("{Duration}", "00:03:45")
            .Replace("{OriginalFileName}", "PUBG_ChickenDinner_20261108.mp4")
            .Replace("{FileName}", "PUBG_ChickenDinner_20261108.mp4")
            .Replace("{Resolution}", "1920x1080 (60 fps)")
            .Replace("{FileSize}", "142.5 MB")
            .Replace("{FolderName}", "PUBG Highlights");
    }

    [RelayCommand]
    public async Task SaveFilterSettingsAsync()
    {
        if (SelectedFolderFilter == null) return;

        try
        {
            SelectedFolderFilter.ApplyToModel();

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var folder = await unitOfWork.MonitoredFolders.GetByIdAsync(SelectedFolderFilter.FolderId);
            if (folder != null)
            {
                if (folder.FilterRule == null)
                {
                    folder.FilterRule = new FolderFilterRule
                    {
                        Id = Guid.NewGuid(),
                        MonitoredFolderId = folder.Id
                    };
                }

                folder.FilterRule.AllowedExtensions = SelectedFolderFilter.AllowedExtensions;
                folder.FilterRule.MinSizeBytes = Math.Max(0, SelectedFolderFilter.MinSizeMb) * 1024 * 1024;
                folder.FilterRule.MaxSizeBytes = Math.Max(0, SelectedFolderFilter.MaxSizeMb) * 1024 * 1024;
                folder.FilterRule.IsMaxSizeEnabled = SelectedFolderFilter.IsMaxSizeEnabled;
                folder.FilterRule.MinDurationSeconds = SelectedFolderFilter.MinDurationSeconds;
                folder.FilterRule.MaxDurationSeconds = SelectedFolderFilter.MaxDurationSeconds;
                folder.FilterRule.NamePatternRegex = string.IsNullOrWhiteSpace(SelectedFolderFilter.NamePatternRegex) ? null : SelectedFolderFilter.NamePatternRegex.Trim();
                folder.FilterRule.IncludeSubdirectories = SelectedFolderFilter.IncludeSubdirectories;

                await unitOfWork.MonitoredFolders.UpdateAsync(folder);
                await unitOfWork.SaveChangesAsync();

                _dialogService.ShowInformation($"Filtros da pasta '{folder.Name}' salvos com sucesso!", "Configurações Salvas");
                StatusMessage = $"Filtros de '{folder.Name}' atualizados.";
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao salvar filtros.");
            _dialogService.ShowWarning($"Erro ao salvar filtros: {ex.Message}");
        }
    }

    [RelayCommand]
    public void ApplyUploadSpeedLimit()
    {
        try
        {
            if (IsSpeedLimitEnabled && UploadSpeedLimitKbps > 0)
            {
                _uploadQueueService.BandwidthLimitBytesPerSecond = (long)UploadSpeedLimitKbps * 1024;
                StatusMessage = $"Velocidade de upload limitada a {UploadSpeedLimitKbps} KB/s.";
            }
            else
            {
                _uploadQueueService.BandwidthLimitBytesPerSecond = 0;
                StatusMessage = "Velocidade de upload configurada para Ilimitado.";
            }

            _dialogService.ShowInformation(
                $"Limite de velocidade atualizado: {(_uploadQueueService.BandwidthLimitBytesPerSecond == 0 ? "Ilimitado" : $"{UploadSpeedLimitKbps} KB/s")}",
                "Rede & Upload");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao aplicar limite de velocidade.");
            _dialogService.ShowWarning($"Erro ao aplicar limite: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SaveTemplateAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ReplayManagerDbContext>();

            var defaultTpl = await dbContext.DescriptionTemplates.FirstOrDefaultAsync(t => t.IsDefault);
            if (defaultTpl != null)
            {
                defaultTpl.Name = TemplateName;
                defaultTpl.TemplateContent = TemplateContent;
            }
            else
            {
                defaultTpl = new DescriptionTemplate
                {
                    Name = TemplateName,
                    TemplateContent = TemplateContent,
                    IsDefault = true
                };
                dbContext.DescriptionTemplates.Add(defaultTpl);
            }

            await dbContext.SaveChangesAsync();
            _dialogService.ShowInformation("Modelo de descrição salvo com sucesso!", "Template Salvo");
            StatusMessage = "Modelo de descrição atualizado.";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao salvar template de descrição.");
            _dialogService.ShowWarning($"Erro ao salvar template: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ImportClientSecretsAsync()
    {
        var filePath = _dialogService.ShowOpenJsonFileDialog("Selecione o arquivo client_secrets.json do Google Cloud");
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            if (!content.Contains("client_id") && !content.Contains("installed"))
            {
                _dialogService.ShowWarning("O arquivo selecionado não parece ser um arquivo válido de credenciais OAuth do Google.", "Formato Inválido");
                return;
            }

            await _dpapiStorage.SaveSecretAsync("GoogleClientSecrets", content);
            HasClientSecretsConfigured = true;

            _dialogService.ShowInformation("Credenciais do Google Cloud importadas e protegidas via Windows DPAPI com sucesso!", "Credenciais Configuradas");
            StatusMessage = "Credenciais do Google Cloud configuradas.";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao importar client_secrets.");
            _dialogService.ShowWarning($"Falha ao importar credenciais: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ConnectYouTubeAsync()
    {
        if (!HasClientSecretsConfigured)
        {
            _dialogService.ShowWarning("Você precisa primeiro importar o arquivo client_secrets.json do Google Cloud Console.\nConsulte o guia em docs/GOOGLE_API_SETUP.md.", "Credenciais Ausentes");
            return;
        }

        IsBusy = true;
        StatusMessage = "Abrindo autenticação Google no navegador...";

        try
        {
            var success = await _youtubeAuthService.AuthenticateAsync();
            if (success)
            {
                IsYouTubeAuthenticated = true;
                var profile = await _youtubeAuthService.GetChannelProfileAsync();
                YoutubeChannelName = string.IsNullOrWhiteSpace(profile.ChannelTitle) ? "Conta Conectada" : profile.ChannelTitle;
                YoutubeChannelId = profile.ChannelId ?? string.Empty;
                YoutubeThumbnailUrl = profile.ThumbnailUrl;

                _dialogService.ShowInformation($"Canal '{YoutubeChannelName}' conectado com sucesso ao YouTube!", "Autenticação Realizada");
                StatusMessage = $"Canal '{YoutubeChannelName}' conectado.";
            }
            else
            {
                _dialogService.ShowWarning("Autenticação não concluída ou cancelada no navegador.", "Atenção");
                StatusMessage = "Autenticação do YouTube cancelada.";
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao conectar conta YouTube.");
            _dialogService.ShowWarning($"Erro ao conectar: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task DisconnectYouTubeAsync()
    {
        var confirmed = _dialogService.ShowConfirmation("Deseja desconectar sua conta do YouTube e revogar os tokens salvos?", "Desconectar YouTube");
        if (!confirmed) return;

        try
        {
            await _youtubeAuthService.SignOutAsync();
            IsYouTubeAuthenticated = false;
            YoutubeChannelName = "Não conectado";
            YoutubeChannelId = string.Empty;
            YoutubeThumbnailUrl = null;

            _dialogService.ShowInformation("Conta desconectada com sucesso.", "YouTube");
            StatusMessage = "Conta do YouTube desconectada.";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao desconectar YouTube.");
            _dialogService.ShowWarning($"Erro: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ExportCsvAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Exportando catálogo para CSV...";

        try
        {
            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var exportFile = Path.Combine(desktopDir, $"ReplayManager_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            await _exportService.ExportToCsvAsync(exportFile);
            StatusMessage = "Catálogo exportado para CSV com sucesso.";
            _dialogService.ShowInformation($"Arquivo CSV gerado na Área de Trabalho:\n{exportFile}", "Exportação Concluída");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro na exportação CSV: {ex.Message}";
            _dialogService.ShowWarning($"Falha ao exportar catálogo:\n{ex.Message}", "Erro de Exportação");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ExportJsonAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Exportando catálogo para JSON...";

        try
        {
            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var exportFile = Path.Combine(desktopDir, $"ReplayManager_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json");

            await _exportService.ExportToJsonAsync(exportFile);
            StatusMessage = "Catálogo exportado para JSON com sucesso.";
            _dialogService.ShowInformation($"Arquivo JSON gerado na Área de Trabalho:\n{exportFile}", "Exportação Concluída");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro na exportação JSON: {ex.Message}";
            _dialogService.ShowWarning($"Falha ao exportar catálogo:\n{ex.Message}", "Erro de Exportação");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CreateLocalBackupAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Criando cópia consistente do SQLite...";

        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var backupDir = Path.Combine(localAppData, "ReplayManager", "Backups");
            var backupFileName = $"replaymanager_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db";
            var destPath = Path.Combine(backupDir, backupFileName);

            var metadata = await _sqliteBackupService.CreateBackupAsync(destPath);
            LastBackupInfo = $"Local: {metadata.FileName} ({metadata.FileSizeBytes / 1024.0:F1} KB) - Íntegro: {metadata.IsConsistent}";
            StatusMessage = "Backup local gerado com sucesso.";

            _dialogService.ShowInformation($"Backup criado com sucesso em:\n{destPath}\n\nIntegridade: Válida", "Backup Local");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao criar backup: {ex.Message}";
            _dialogService.ShowWarning($"Falha ao gerar cópia atômica do SQLite:\n{ex.Message}", "Erro de Backup");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task BackupToGoogleDriveAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Gerando cópia consistente e enviando ao Google Drive...";

        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var backupDir = Path.Combine(localAppData, "ReplayManager", "Backups");
            var backupFileName = $"replaymanager_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db";
            var destPath = Path.Combine(backupDir, backupFileName);

            var localMeta = await _sqliteBackupService.CreateBackupAsync(destPath);
            if (!localMeta.IsConsistent)
            {
                throw new InvalidOperationException("A verificação de integridade do backup local falhou.");
            }

            StatusMessage = "Transmitindo backup para o Google Drive...";
            var driveMeta = await _driveBackupService.UploadBackupAsync(destPath);

            StatusMessage = "Aplicando política de retenção...";
            var purged = await _driveBackupService.EnforceRetentionAsync(RetentionCount);

            LastBackupInfo = $"Nuvem: {driveMeta.FileName} (Id: {driveMeta.DriveFileId})";
            StatusMessage = $"Backup enviado ao Google Drive com sucesso! ({purged} antigos excluídos).";

            await RefreshRemoteBackupsAsync();
            _dialogService.ShowInformation($"Backup concluído com sucesso no Google Drive!\nID: {driveMeta.DriveFileId}\nRetenção aplicada: {purged} itens antigos removidos.", "Backup Google Drive");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao realizar backup no Google Drive: {ex.Message}";
            _dialogService.ShowWarning($"Falha no backup em nuvem:\n{ex.Message}", "Erro de Backup Google Drive");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RefreshRemoteBackupsAsync()
    {
        try
        {
            var list = await _driveBackupService.ListRemoteBackupsAsync();
            RemoteBackups.Clear();
            foreach (var item in list)
            {
                RemoteBackups.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao listar backups do Google Drive.");
        }
    }
}
