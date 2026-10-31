using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReplayManager.App.Services;
using ReplayManager.Application.Contracts.Media;
using ReplayManager.Application.Contracts.Persistence;
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

    // Campos editáveis do painel de detalhes para o vídeo selecionado
    [ObservableProperty]
    private string _editTitle = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    public LibraryViewModel(
        IUnitOfWork unitOfWork,
        IMediaPlayerLauncher mediaPlayer,
        IThumbnailGenerator thumbnailGenerator,
        IDialogService dialogService,
        ILogger<LibraryViewModel>? logger = null)
    {
        _unitOfWork = unitOfWork;
        _mediaPlayer = mediaPlayer;
        _thumbnailGenerator = thumbnailGenerator;
        _dialogService = dialogService;
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

    [RelayCommand]
    public void PlayVideo(VideoCardViewModel? target)
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
                $"Não foi possível reproduzir o arquivo: '{video.FilePath}'. Verifique se o caminho existe ou instale o VLC Player.",
                "Falha na Reprodução");
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

        // Filtro por status
        query = CurrentFilter switch
        {
            LibraryFilterOption.OnlyLocal => query.Where(v => v.UploadStatus == UploadStatus.None && v.LocalStatus == LocalFileStatus.Available),
            LibraryFilterOption.Queued => query.Where(v => v.UploadStatus is UploadStatus.Queued or UploadStatus.Uploading),
            LibraryFilterOption.Uploaded => query.Where(v => v.UploadStatus == UploadStatus.Completed),
            LibraryFilterOption.MissingFile => query.Where(v => v.LocalStatus == LocalFileStatus.Missing),
            _ => query
        };

        // Ordenação padrão: data mais recente primeiro
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

