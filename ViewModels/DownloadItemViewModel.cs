using System;
using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public class DownloadItemViewModel : BaseViewModel
    {
        private readonly IDownloadService _downloadService;
        private DownloadItem _item;

        public DownloadItemViewModel(DownloadItem item, IDownloadService downloadService)
        {
            _item = item;
            _downloadService = downloadService;

            PauseCommand = new AsyncRelayCommand(async () =>
            {
                _item.Status = DownloadStatus.Paused;
                await _downloadService.PauseDownloadAsync(_item.Id);
                Refresh();
            }, () => _item.Status == DownloadStatus.Downloading);

            ResumeCommand = new AsyncRelayCommand(async () =>
            {
                _item.Status = DownloadStatus.Downloading;
                await _downloadService.ResumeDownloadAsync(_item.Id);
                Refresh();
            }, () => _item.Status == DownloadStatus.Paused);

            CancelCommand = new AsyncRelayCommand(async () =>
            {
                await _downloadService.CancelDownloadAsync(_item.Id);
                Refresh();
            }, () => _item.Status == DownloadStatus.Downloading || _item.Status == DownloadStatus.Paused);

            RetryCommand = new AsyncRelayCommand(async () =>
            {
                _item.RetryCount++;
                _item.Status = DownloadStatus.Retrying;
                _item.ErrorMessage = null;
                Refresh();
                await _downloadService.StartDownloadAsync(_item);
            }, () => _item.Status == DownloadStatus.Failed);
        }

        public DownloadItem Item => _item;

        public string Id => _item.Id;
        public string AnimeTitle => _item.AnimeTitle;
        public string EpisodeTitle => _item.EpisodeTitle;
        public int EpisodeNumber => _item.EpisodeNumber;
        public string Language => _item.Language;
        public string PosterUrl => _item.PosterUrl;
        public DownloadStatus Status => _item.Status;
        public double Progress => _item.Progress;
        public string ProgressText => $"{_item.Progress:F0}%";
        public string DownloadedText => FormatBytes(_item.DownloadedBytes);
        public string TotalText => FormatBytes(_item.TotalBytes);
        public string SpeedText => $"{_item.CurrentSpeed / 1_000_000:F1} MB/s";
        public string EtaText => FormatEta(_item.EstimatedTimeRemaining);
        public int RetryCount => _item.RetryCount;
        public string? ErrorMessage => _item.ErrorMessage;
        public bool IsDownloading => _item.Status == DownloadStatus.Downloading;
        public bool IsPaused => _item.Status == DownloadStatus.Paused;
        public bool IsFailed => _item.Status == DownloadStatus.Failed;
        public bool IsCompleted => _item.Status == DownloadStatus.Completed;
        public bool IsRetrying => _item.Status == DownloadStatus.Retrying;
        public bool IsCancelled => _item.Status == DownloadStatus.Cancelled;

        public ICommand PauseCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RetryCommand { get; }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(DownloadedText));
            OnPropertyChanged(nameof(TotalText));
            OnPropertyChanged(nameof(SpeedText));
            OnPropertyChanged(nameof(EtaText));
            OnPropertyChanged(nameof(IsDownloading));
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(IsFailed));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsRetrying));
            OnPropertyChanged(nameof(IsCancelled));
            OnPropertyChanged(nameof(RetryCount));
            OnPropertyChanged(nameof(ErrorMessage));
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1_000_000_000) return $"{bytes / 1_000_000_000.0:F2} GB";
            if (bytes >= 1_000_000) return $"{bytes / 1_000_000.0:F0} MB";
            if (bytes >= 1_000) return $"{bytes / 1_000.0:F0} KB";
            return $"{bytes} B";
        }

        private static string FormatEta(TimeSpan eta)
        {
            if (eta == TimeSpan.Zero) return "--:--";
            if (eta.TotalHours >= 1) return $"{(int)eta.TotalHours:D2}:{eta.Minutes:D2}:{eta.Seconds:D2}";
            return $"{eta.Minutes:D2}:{eta.Seconds:D2}";
        }
    }
}
