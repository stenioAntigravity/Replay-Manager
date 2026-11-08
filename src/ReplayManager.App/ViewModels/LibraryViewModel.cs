using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReplayManager.App.Services;
using ReplayManager.Application.Contracts.Media;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Application.Contracts.YouTube;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;

namespace ReplayManager.App.ViewModels;

/// <summary>
/// ViewModel da tela de Biblioteca de Vídeos, gerenciando listagem, busca, filtros e detalhes.
/// </summary>
public partial class LibraryViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaPlayerLauncher _mediaPlayer;
    private readonly IThumbnailGenerator _thumbnailGenerator;
    private readonly IDialogService _dialogService;
    private readonly IYouTubeAuthService? _youTubeAuthService;
    private readonly ILogger<LibraryViewModel>? _logger;

    private readonly List<VideoCardViewModel> _allVideos = new();

    [ObservableProperty]
    private ObservableCollection<VideoCardViewModel> _filteredVideos = new();

    [ObservableProperty]
    private VideoCardViewModel? _selectedVideo;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private LibraryFilterOption _currentFilter = LibraryFilterOption.All;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // Métricas do cabeçalho
    [ObservableProperty]
    private int _totalVideosCount;

    [ObservableProperty]
    private int _uploadedVideosCount;

    [ObservableProperty]
    private int _queuedVideosCount;

    [ObservableProperty]
    private bool _isYouTubeConnected;

    // Campos editáveis do painel de detalhes para o vídeo selecionado
    [ObservableProperty]
    private string _editTitle = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    public event Action? NavigateToMonitoredFoldersRequested;

    [RelayCommand]
    public void GoToMonitoredFolders()
    {
        NavigateToMonitoredFoldersRequested?.Invoke();
    }

    public LibraryViewModel(
        IUnitOfWork unitOfWork,
        IMediaPlayerLauncher mediaPlayer,
        IThumbnailGenerator thumbnailGenerator,
        IDialogService dialogService,
        IYouTubeAuthService? youTubeAuthService = null,
        ILogger<LibraryViewModel>? logger = null)
    {
        _unitOfWork = unitOfWork;
        _mediaPlayer = mediaPlayer;
        _thumbnailGenerator = thumbnailGenerator;
        _dialogService = dialogService;
        _youTubeAuthService = youTubeAuthService;
        _logger = logger;
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnCurrentFilterChanged(LibraryFilterOption value)
    {
        ApplyFilters();
    }

    partial void OnSelectedVideoChanged(VideoCardViewModel? value)
    {
        if (value != null)
        {
            EditTitle = value.CustomTitle ?? value.FileName;
            EditDescription = value.CustomDescription ?? string.Empty;
        }
        else
        {
            EditTitle = string.Empty;
            EditDescription = string.Empty;
        }
    }

    [RelayCommand]
    public async Task LoadVideosAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        StatusMessage = "Carregando vídeos da biblioteca...";

        try
        {
            var videos = await _unitOfWork.Videos.GetAllAsync(cancellationToken: cancellationToken);
            _allVideos.Clear();

            foreach (var video in videos)
            {
                _allVideos.Add(new VideoCardViewModel(video));
            }

            // Atualiza métricas
            UpdateSummaryCounters();

            // Verifica conexão com YouTube em segundo plano sem bloquear
            try
            {
                IsYouTubeConnected = _youTubeAuthService != null
                    && await _youTubeAuthService.IsAuthenticatedAsync(cancellationToken);
            }
            catch (Exception authEx)
            {
                _logger?.LogDebug(authEx, "Não foi possível verificar status do YouTube no carregamento.");
                IsYouTubeConnected = false;
            }

            ApplyFilters();

            StatusMessage = $"{_allVideos.Count} vídeo(s) na biblioteca.";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Erro ao carregar vídeos da biblioteca.");
            StatusMessage = "Erro ao carregar biblioteca de vídeos.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateSummaryCounters()
    {
        TotalVideosCount = _allVideos.Count;
        UploadedVideosCount = _allVideos.Count(v => v.UploadStatus == UploadStatus.Completed || v.HasYouTubeVideo);
        QueuedVideosCount = _allVideos.Count(v => v.UploadStatus is UploadStatus.Queued or UploadStatus.Uploading);
    }

    [RelayCommand]
    public async Task ToggleFavoriteAsync(VideoCardViewModel? target)
    {
        var video = target ?? SelectedVideo;
        if (video == null) return;

        video.IsFavorite = !video.IsFavorite;
        video.Model.IsFavorite = video.IsFavorite;

        try
        {
            await _unitOfWork.Videos.UpdateAsync(video.Model);
            await _unitOfWork.SaveChangesAsync();

            // Se estiver filtrado por favoritos e o usuário desmarcou, reaplicar filtro
            if (CurrentFilter == LibraryFilterOption.Favorites && !video.IsFavorite)
            {
                ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao alternar favorito do vídeo {VideoId}", video.Id);
            video.IsFavorite = !video.IsFavorite; // reverte em caso de erro
            video.Model.IsFavorite = video.IsFavorite;
        }
    }

    [RelayCommand]
    public void PlayVideo(VideoCardViewModel? target)
    {
        var video = target ?? SelectedVideo;
        if (video == null) return;

        // Comportamento inteligente:
        // 1. Se existir localmente, abre o arquivo local via player configurado (VLC / padrão)
        if (video.HasLocalFile && !video.IsMissingOnDisk)
        {
            var started = _mediaPlayer.PlayVideo(video.FilePath);
            if (!started)
            {
                // Se falhou abrir localmente mas tem link no YouTube, tenta abrir o link
                if (video.HasYouTubeVideo && !string.IsNullOrWhiteSpace(video.YouTubeUrl))
                {
                    OpenYouTubeLink(video);
                }
                else
                {
                    _dialogService.ShowWarning(
                        $"Não foi possível reproduzir o arquivo: '{video.FilePath}'. Verifique se o caminho existe ou instale o VLC Player.",
                        "Falha na Reprodução");
                }
            }
            return;
        }

        // 2. Se o arquivo local não existir, abre o vídeo no YouTube
        if (video.HasYouTubeVideo && !string.IsNullOrWhiteSpace(video.YouTubeUrl))
        {
            OpenYouTubeLink(video);
            return;
        }

        _dialogService.ShowWarning(
            "O arquivo físico deste vídeo não está acessível no disco e ele ainda não foi enviado ao YouTube.",
            "Arquivo Indisponível");
    }

    [RelayCommand]
    public void OpenLocalVideo(VideoCardViewModel? target)
    {
        var video = target ?? SelectedVideo;
        if (video == null) return;

        if (video.LocalStatus == LocalFileStatus.Missing)
        {
            _dialogService.ShowWarning(
                "O arquivo físico deste vídeo não está acessível no disco.",
                "Arquivo Ausente");
            return;
        }

        var started = _mediaPlayer.PlayVideo(video.FilePath);
        if (!started)
        {
            _dialogService.ShowWarning(
                $"Não foi possível reproduzir o arquivo: '{video.FilePath}'.",
                "Falha na Reprodução");
        }
    }

    [RelayCommand]
    public void OpenYouTubeLink(VideoCardViewModel? target)
    {
        var video = target ?? SelectedVideo;
        if (video == null || string.IsNullOrWhiteSpace(video.YouTubeUrl))
        {
            _dialogService.ShowWarning("Este vídeo ainda não possui link no YouTube.", "YouTube");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(video.YouTubeUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao abrir link do YouTube: {Url}", video.YouTubeUrl);
            _dialogService.ShowWarning($"Não foi possível abrir o navegador: {ex.Message}");
        }
    }

    [RelayCommand]
    public void CopyYouTubeLink(VideoCardViewModel? target)
    {
        var video = target ?? SelectedVideo;
        if (video == null || string.IsNullOrWhiteSpace(video.YouTubeUrl))
        {
            _dialogService.ShowInformation("Este vídeo ainda não possui link no YouTube.");
            return;
        }

        try
        {
            System.Windows.Clipboard.SetText(video.YouTubeUrl);
            StatusMessage = $"Link do YouTube copiado para a área de transferência: {video.YouTubeUrl}";
            _dialogService.ShowInformation("Link do YouTube copiado para a área de transferência!");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao copiar URL do YouTube para área de transferência.");
        }
    }

    [RelayCommand]
    public async Task ChangeThumbnailAsync(VideoCardViewModel? target)
    {
        var video = target ?? SelectedVideo;
        if (video == null) return;

        var selectedImagePath = _dialogService.ShowOpenImageFileDialog();
        if (string.IsNullOrWhiteSpace(selectedImagePath))
            return;

        try
        {
            var newThumbPath = await _thumbnailGenerator.ReplaceThumbnailAsync(selectedImagePath, video.Id);
            
            video.ThumbnailPath = newThumbPath;
            video.Model.ThumbnailPath = newThumbPath;

            await _unitOfWork.Videos.UpdateAsync(video.Model);
            await _unitOfWork.SaveChangesAsync();

            _dialogService.ShowInformation("Miniatura atualizada com sucesso!");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao atualizar miniatura do vídeo {VideoId}", video.Id);
            _dialogService.ShowWarning($"Erro ao atualizar miniatura: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SaveMetadataChangesAsync()
    {
        if (SelectedVideo == null) return;

        try
        {
            SelectedVideo.CustomTitle = EditTitle;
            SelectedVideo.CustomDescription = EditDescription;

            SelectedVideo.Model.CustomTitle = EditTitle;
            SelectedVideo.Model.CustomDescription = EditDescription;

            await _unitOfWork.Videos.UpdateAsync(SelectedVideo.Model);
            await _unitOfWork.SaveChangesAsync();

            _dialogService.ShowInformation("Metadados salvos com sucesso!");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao salvar metadados do vídeo {VideoId}", SelectedVideo.Id);
            _dialogService.ShowWarning($"Erro ao salvar metadados: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task EnqueueForUploadAsync(VideoCardViewModel? target)
    {
        var video = target ?? SelectedVideo;
        if (video == null) return;

        if (video.LocalStatus == LocalFileStatus.Missing)
        {
            _dialogService.ShowWarning("Não é possível enviar um vídeo cujo arquivo físico não está acessível no disco.");
            return;
        }

        if (video.UploadStatus is UploadStatus.Queued or UploadStatus.Uploading or UploadStatus.Completed)
        {
            _dialogService.ShowInformation($"Este vídeo já está com status '{video.StatusBadgeText}'.");
            return;
        }

        try
        {
            var queueItem = new UploadQueueItem
            {
                Id = Guid.NewGuid(),
                VideoRecordId = video.Id,
                Status = UploadStatus.Queued,
                Priority = 10,
                QueuedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.UploadQueue.AddAsync(queueItem);

            video.UploadStatus = UploadStatus.Queued;
            video.Model.UploadStatus = UploadStatus.Queued;
            await _unitOfWork.Videos.UpdateAsync(video.Model);

            await _unitOfWork.SaveChangesAsync();

            UpdateSummaryCounters();
            _dialogService.ShowInformation("Vídeo adicionado à fila de upload com sucesso!");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao enfileirar vídeo {VideoId}", video.Id);
            _dialogService.ShowWarning($"Erro ao enfileirar vídeo: {ex.Message}");
        }
    }

    public void ApplyFilters()
    {
        IEnumerable<VideoCardViewModel> query = _allVideos;

        // Filtro por texto (nome do arquivo ou título customizado)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim().ToLowerInvariant();
            query = query.Where(v =>
                v.FileName.ToLowerInvariant().Contains(search) ||
                (!string.IsNullOrEmpty(v.CustomTitle) && v.CustomTitle.ToLowerInvariant().Contains(search)));
        }

        // Filtro por status / favoritos
        query = CurrentFilter switch
        {
            LibraryFilterOption.Favorites => query.Where(v => v.IsFavorite),
            LibraryFilterOption.OnlyLocal => query.Where(v => v.UploadStatus == UploadStatus.None && v.LocalStatus == LocalFileStatus.Available),
            LibraryFilterOption.Queued => query.Where(v => v.UploadStatus is UploadStatus.Queued or UploadStatus.Uploading),
            LibraryFilterOption.Uploaded => query.Where(v => v.UploadStatus == UploadStatus.Completed || v.HasYouTubeVideo),
            LibraryFilterOption.MissingFile => query.Where(v => v.LocalStatus == LocalFileStatus.Missing),
            _ => query
        };

        // Ordenação padrão: favoritos no topo se for geral ou data mais recente primeiro
        var list = query
            .OrderByDescending(v => v.RecordedAtUtc ?? v.Model.CreatedAtUtc)
            .ToList();

        FilteredVideos = new ObservableCollection<VideoCardViewModel>(list);

        // Se o selecionado foi filtrado para fora, desselecionar ou selecionar o primeiro
        if (SelectedVideo != null && !FilteredVideos.Contains(SelectedVideo))
        {
            SelectedVideo = FilteredVideos.FirstOrDefault();
        }
        else if (SelectedVideo == null && FilteredVideos.Count > 0)
        {
            SelectedVideo = FilteredVideos.First();
        }
    }
}
