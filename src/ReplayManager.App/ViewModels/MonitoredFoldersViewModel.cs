using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReplayManager.App.Services;
using ReplayManager.Application.Contracts.FileSystem;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Domain.Entities;

namespace ReplayManager.App.ViewModels;

public partial class FolderItemViewModel : ObservableObject
{
    private readonly MonitoredFolder _folder;

    public MonitoredFolder Model => _folder;

    public Guid Id => _folder.Id;
    public string Path => _folder.Path;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _autoUploadEnabled;

    [ObservableProperty]
    private bool _includeSubdirectories;

    [ObservableProperty]
    private string _allowedExtensions;

    [ObservableProperty]
    private DateTime? _lastScannedAtUtc;

    public string LastScannedText => LastScannedAtUtc.HasValue 
        ? LastScannedAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") 
        : "Nunca verificado";

    public FolderItemViewModel(MonitoredFolder folder)
    {
        _folder = folder;
        _name = folder.Name;
        _isActive = folder.IsActive;
        _autoUploadEnabled = folder.AutoUploadEnabled;
        _includeSubdirectories = folder.FilterRule?.IncludeSubdirectories ?? false;
        _allowedExtensions = folder.FilterRule?.AllowedExtensions ?? ".mp4;.mkv;.avi";
        _lastScannedAtUtc = folder.LastScannedAtUtc;
    }
}

/// <summary>
/// ViewModel da aba "Pastas Monitoradas", permitindo cadastro, edição,
/// exclusão, ativação do FileSystemWatcher e varredura/reconciliação imediata.
/// </summary>
public partial class MonitoredFoldersViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileWatcherService _fileWatcherService;
    private readonly IFolderReconciliationWorker _reconciliationWorker;
    private readonly IDialogService _dialogService;
    private readonly ILogger<MonitoredFoldersViewModel>? _logger;

    [ObservableProperty]
    private ObservableCollection<FolderItemViewModel> _folders = new();

    [ObservableProperty]
    private FolderItemViewModel? _selectedFolder;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public MonitoredFoldersViewModel(
        IUnitOfWork unitOfWork,
        IFileWatcherService fileWatcherService,
        IFolderReconciliationWorker reconciliationWorker,
        IDialogService dialogService,
        ILogger<MonitoredFoldersViewModel>? logger = null)
    {
        _unitOfWork = unitOfWork;
        _fileWatcherService = fileWatcherService;
        _reconciliationWorker = reconciliationWorker;
        _dialogService = dialogService;
        _logger = logger;
    }

    [RelayCommand]
    public async Task LoadFoldersAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        StatusMessage = "Carregando pastas cadastradas...";

        try
        {
            var list = await _unitOfWork.MonitoredFolders.GetAllAsync(cancellationToken: cancellationToken);
            Folders.Clear();

            foreach (var item in list)
            {
                var vm = new FolderItemViewModel(item);
                Folders.Add(vm);

                // Garante que o watcher esteja alinhado ao estado da entidade
                if (item.IsActive && Directory.Exists(item.Path))
                {
                    if (!_fileWatcherService.IsMonitoring(item.Id))
                    {
                        _fileWatcherService.StartMonitoring(item);
                    }
                }
                else
                {
                    _fileWatcherService.StopMonitoring(item.Id);
                }
            }

            StatusMessage = $"{Folders.Count} pasta(s) configurada(s).";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao carregar pastas monitoradas.");
            StatusMessage = "Erro ao carregar pastas.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AddFolderAsync()
    {
        var selectedPath = _dialogService.ShowFolderBrowserDialog("Selecione o diretório para monitoramento de vídeos");
        if (string.IsNullOrWhiteSpace(selectedPath))
            return;

        try
        {
            var existing = await _unitOfWork.MonitoredFolders.GetByPathAsync(selectedPath);
            if (existing != null)
            {
                _dialogService.ShowWarning($"A pasta '{selectedPath}' já está cadastrada no sistema.", "Pasta Duplicada");
                return;
            }

            var dirName = new DirectoryInfo(selectedPath).Name;
            var newFolder = new MonitoredFolder
            {
                Id = Guid.NewGuid(),
                Path = selectedPath,
                Name = string.IsNullOrWhiteSpace(dirName) ? selectedPath : dirName,
                IsActive = true,
                AutoUploadEnabled = false,
                FilterRule = new FolderFilterRule
                {
                    Id = Guid.NewGuid(),
                    IncludeSubdirectories = false,
                    AllowedExtensions = ".mp4;.mkv;.avi"
                }
            };

            await _unitOfWork.MonitoredFolders.AddAsync(newFolder);
            await _unitOfWork.SaveChangesAsync();

            _fileWatcherService.StartMonitoring(newFolder);

            var vm = new FolderItemViewModel(newFolder);
            Folders.Add(vm);
            SelectedFolder = vm;

            _dialogService.ShowInformation(
                $"Pasta '{newFolder.Name}' adicionada com sucesso!\nIniciando verificação de vídeos...",
                "Pasta Adicionada");

            // Executa varredura inicial imediatamente
            await ReconcileFolderAsync(vm);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao adicionar pasta monitorada: {Path}", selectedPath);
            _dialogService.ShowWarning($"Falha ao adicionar pasta: {ex.Message}", "Erro");
        }
    }

    [RelayCommand]
    public async Task ToggleActiveAsync(FolderItemViewModel? folderVm)
    {
        var target = folderVm ?? SelectedFolder;
        if (target == null) return;

        try
        {
            target.IsActive = !target.IsActive;
            target.Model.IsActive = target.IsActive;

            await _unitOfWork.MonitoredFolders.UpdateAsync(target.Model);
            await _unitOfWork.SaveChangesAsync();

            if (target.IsActive)
            {
                if (Directory.Exists(target.Path))
                {
                    _fileWatcherService.StartMonitoring(target.Model);
                    StatusMessage = $"Monitoramento ativado para '{target.Name}'.";
                }
                else
                {
                    _dialogService.ShowWarning($"O diretório '{target.Path}' não existe no disco.", "Aviso");
                }
            }
            else
            {
                _fileWatcherService.StopMonitoring(target.Id);
                StatusMessage = $"Monitoramento pausado para '{target.Name}'.";
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao alterar status da pasta.");
            _dialogService.ShowWarning($"Erro ao atualizar status: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ToggleSubdirectoriesAsync(FolderItemViewModel? folderVm)
    {
        var target = folderVm ?? SelectedFolder;
        if (target == null) return;

        try
        {
            if (target.Model.FilterRule != null)
            {
                target.Model.FilterRule.IncludeSubdirectories = target.IncludeSubdirectories;
            }
            else
            {
                target.Model.FilterRule = new FolderFilterRule
                {
                    Id = Guid.NewGuid(),
                    MonitoredFolderId = target.Id,
                    IncludeSubdirectories = target.IncludeSubdirectories
                };
            }

            await _unitOfWork.MonitoredFolders.UpdateAsync(target.Model);
            await _unitOfWork.SaveChangesAsync();

            // Reinicia watcher se ativo para aplicar a flag de subdiretórios
            if (target.IsActive && Directory.Exists(target.Path))
            {
                _fileWatcherService.StartMonitoring(target.Model);
            }

            StatusMessage = $"Subpastas {(target.IncludeSubdirectories ? "incluídas" : "ignoradas")} para '{target.Name}'.";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao atualizar configuração de subpastas.");
            _dialogService.ShowWarning($"Erro: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ToggleAutoUploadAsync(FolderItemViewModel? folderVm)
    {
        var target = folderVm ?? SelectedFolder;
        if (target == null) return;

        try
        {
            target.Model.AutoUploadEnabled = target.AutoUploadEnabled;

            await _unitOfWork.MonitoredFolders.UpdateAsync(target.Model);
            await _unitOfWork.SaveChangesAsync();

            StatusMessage = $"Auto-upload {(target.AutoUploadEnabled ? "ativado" : "desativado")} para '{target.Name}'.";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao alterar auto-upload.");
            _dialogService.ShowWarning($"Erro: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DeleteFolderAsync(FolderItemViewModel? folderVm)
    {
        var target = folderVm ?? SelectedFolder;
        if (target == null) return;

        var confirmed = _dialogService.ShowConfirmation(
            $"Deseja remover a pasta '{target.Name}' do monitoramento?\n(Os arquivos físicos no disco e vídeos catalogados não serão apagados).",
            "Remover Pasta");

        if (!confirmed) return;

        try
        {
            _fileWatcherService.StopMonitoring(target.Id);

            // Nota: o repositório mantém integridade
            var folderEntity = await _unitOfWork.MonitoredFolders.GetByIdAsync(target.Id);
            if (folderEntity != null)
            {
                folderEntity.IsActive = false;
                await _unitOfWork.MonitoredFolders.UpdateAsync(folderEntity);
                await _unitOfWork.SaveChangesAsync();
            }

            Folders.Remove(target);
            SelectedFolder = Folders.FirstOrDefault();

            StatusMessage = $"Pasta '{target.Name}' removida do monitoramento ativo.";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao remover pasta.");
            _dialogService.ShowWarning($"Erro ao remover pasta: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ReconcileFolderAsync(FolderItemViewModel? folderVm)
    {
        var target = folderVm ?? SelectedFolder;
        if (target == null) return;

        IsLoading = true;
        StatusMessage = $"Varrendo '{target.Name}'...";

        try
        {
            var result = await _reconciliationWorker.ReconcileFolderAsync(target.Id);

            target.LastScannedAtUtc = DateTime.UtcNow;
            target.Model.LastScannedAtUtc = target.LastScannedAtUtc;
            await _unitOfWork.MonitoredFolders.UpdateAsync(target.Model);
            await _unitOfWork.SaveChangesAsync();

            var resumo = $"Varredura concluída para '{target.Name}':\n" +
                         $"• Arquivos no disco: {result.TotalScannedFiles}\n" +
                         $"• Novos importados: {result.NewFilesIngested}\n" +
                         $"• Preservados/Reconciliados: {result.ExistingFilesPreserved}\n" +
                         $"• Marcados como ausentes: {result.MissingFilesMarked}\n" +
                         $"• Erros: {result.Errors.Count}";

            _dialogService.ShowInformation(resumo, "Resultado da Varredura");
            StatusMessage = $"Varredura de '{target.Name}' finalizada. +{result.NewFilesIngested} novos vídeos.";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao reconciliar pasta '{Path}'", target.Path);
            _dialogService.ShowWarning($"Erro durante a varredura: {ex.Message}", "Falha na Varredura");
            StatusMessage = "Erro na varredura da pasta.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
