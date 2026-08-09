using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public class DownloadsViewModel : BaseViewModel
    {
        private readonly IDownloadQueueService _queueService;
        private readonly IDownloadService _downloadService;
        private readonly INotificationService _notificationService;

        public ObservableCollection<DownloadItemViewModel> ActiveDownloads { get; } = new();
        public ObservableCollection<DownloadItemViewModel> CompletedDownloads { get; } = new();

        public bool HasActiveDownloads => ActiveDownloads.Count > 0;
        public bool HasCompletedDownloads => CompletedDownloads.Count > 0;

        public ICommand PauseAllCommand { get; }
        public ICommand ResumeAllCommand { get; }
        public ICommand ClearCompletedCommand { get; }

        public DownloadsViewModel(
            IDownloadQueueService queueService,
            IDownloadService downloadService,
            INotificationService notificationService)
        {
            _queueService = queueService;
            _downloadService = downloadService;
            _notificationService = notificationService;

            PauseAllCommand = new AsyncRelayCommand(async () =>
            {
                await _queueService.PauseAllAsync();
                _notificationService.Show("All downloads paused");
            });

            ResumeAllCommand = new AsyncRelayCommand(async () =>
            {
                await _queueService.ResumeAllAsync();
                _notificationService.Show("All downloads resumed");
            });

            ClearCompletedCommand = new AsyncRelayCommand(async () =>
            {
                await _queueService.ClearCompletedAsync();
                CompletedDownloads.Clear();
                OnPropertyChanged(nameof(HasCompletedDownloads));
            });

            _downloadService.DownloadProgressChanged += OnProgressChanged;
            _downloadService.DownloadStatusChanged += OnStatusChanged;
            _downloadService.DownloadCompleted += OnCompleted;
            _downloadService.DownloadFailed += OnFailed;
            _queueService.QueueChanged += OnQueueChanged;
        }

        private void OnProgressChanged(object? sender, DownloadItem item)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var vm = ActiveDownloads.FirstOrDefault(d => d.Id == item.Id);
                vm?.Refresh();
            });
        }

        private void OnStatusChanged(object? sender, DownloadItem item)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var vm = ActiveDownloads.FirstOrDefault(d => d.Id == item.Id);
                if (vm == null && item.Status == DownloadStatus.Downloading)
                {
                    ActiveDownloads.Add(new DownloadItemViewModel(item, _downloadService));
                    OnPropertyChanged(nameof(HasActiveDownloads));
                }
                else vm?.Refresh();
            });
        }

        private void OnCompleted(object? sender, DownloadItem item)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var vm = ActiveDownloads.FirstOrDefault(d => d.Id == item.Id);
                if (vm != null)
                {
                    vm.Refresh();
                    ActiveDownloads.Remove(vm);
                    CompletedDownloads.Insert(0, vm);
                    OnPropertyChanged(nameof(HasActiveDownloads));
                    OnPropertyChanged(nameof(HasCompletedDownloads));
                    _notificationService.ShowSuccess($"Download complete: {item.AnimeTitle} Ep {item.EpisodeNumber}");
                }
            });
        }

        private void OnFailed(object? sender, DownloadItem item)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var vm = ActiveDownloads.FirstOrDefault(d => d.Id == item.Id);
                vm?.Refresh();
                _notificationService.ShowError($"Download failed: {item.AnimeTitle} Ep {item.EpisodeNumber}");
            });
        }

        private void OnQueueChanged(object? sender, QueueItem qi)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (qi.DownloadItem?.Status == DownloadStatus.Downloading)
                {
                    if (!ActiveDownloads.Any(d => d.Id == qi.DownloadItem.Id))
                    {
                        ActiveDownloads.Add(new DownloadItemViewModel(qi.DownloadItem, _downloadService));
                        OnPropertyChanged(nameof(HasActiveDownloads));
                    }
                }
            });
        }
    }
}
