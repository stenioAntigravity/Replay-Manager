using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReplayManager.Application.Contracts.Upload;
using ReplayManager.Domain.Enums;

namespace ReplayManager.App.ViewModels;

public partial class UploadQueueViewModel : ObservableObject
{
    private readonly IUploadQueueService _queueService;

    [ObservableProperty]
    private ObservableCollection<UploadQueueItemViewModel> _items = new();

    [ObservableProperty]
    private UploadQueueItemViewModel? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _bandwidthLimitDisplay = "Ilimitado";

    public UploadQueueViewModel(IUploadQueueService queueService)
    {
        _queueService = queueService;
        _queueService.ProgressUpdated += OnProgressUpdated;
        _queueService.StatusChanged += OnStatusChanged;
    }

    [RelayCommand]
    public async Task LoadQueueAsync()
    {
        IsLoading = true;
        try
        {
            var rawItems = await _queueService.GetQueueItemsAsync();
            Items.Clear();
            foreach (var item in rawItems)
            {
                Items.Add(new UploadQueueItemViewModel(item));
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task PauseItemAsync(UploadQueueItemViewModel? item)
    {
        if (item == null) return;
        await _queueService.PauseItemAsync(item.Id);
        item.UpdateStatus(UploadStatus.Paused);
    }

    [RelayCommand]
    public async Task ResumeItemAsync(UploadQueueItemViewModel? item)
    {
        if (item == null) return;
        await _queueService.ResumeItemAsync(item.Id);
        item.UpdateStatus(UploadStatus.Queued);
    }

    [RelayCommand]
    public async Task CancelItemAsync(UploadQueueItemViewModel? item)
    {
        if (item == null) return;
        var success = await _queueService.CancelItemAsync(item.Id);
        if (success)
        {
            Items.Remove(item);
        }
    }

    [RelayCommand]
    public async Task RetryItemAsync(UploadQueueItemViewModel? item)
    {
        if (item == null) return;
        await _queueService.RetryItemAsync(item.Id);
        item.UpdateStatus(UploadStatus.Queued);
    }

    [RelayCommand]
    public async Task MoveUpAsync(UploadQueueItemViewModel? item)
    {
        if (item == null) return;
        var success = await _queueService.MoveUpAsync(item.Id);
        if (success)
        {
            await LoadQueueAsync();
        }
    }

    [RelayCommand]
    public async Task MoveDownAsync(UploadQueueItemViewModel? item)
    {
        if (item == null) return;
        var success = await _queueService.MoveDownAsync(item.Id);
        if (success)
        {
            await LoadQueueAsync();
        }
    }

    private void OnProgressUpdated(object? sender, UploadProgressUpdatedEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            var item = Items.FirstOrDefault(i => i.Id == e.QueueItemId);
            item?.UpdateProgress(e.BytesUploaded, e.TotalBytes, e.Percentage, e.SpeedBytesPerSecond);
        });
    }

    private void OnStatusChanged(object? sender, QueueItemStatusChangedEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            var item = Items.FirstOrDefault(i => i.Id == e.QueueItemId);
            item?.UpdateStatus(e.NewStatus, e.ErrorMessage);
        });
    }
}
