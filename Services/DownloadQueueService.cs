using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services
{
    public class DownloadQueueService : IDownloadQueueService
    {
        private readonly IDownloadService _downloadService;
        private readonly ObservableCollection<QueueItem> _queue = new();
        private readonly ObservableCollection<DownloadItem> _activeDownloads = new();
        private readonly ObservableCollection<DownloadItem> _completedDownloads = new();
        private readonly AppSettings _settings;
        private int _activeCount = 0;

        public event EventHandler<QueueItem>? QueueChanged;

        public IReadOnlyList<QueueItem> Queue => _queue;
        public IReadOnlyList<DownloadItem> ActiveDownloads => _activeDownloads;
        public IReadOnlyList<DownloadItem> CompletedDownloads => _completedDownloads;

        public DownloadQueueService(IDownloadService downloadService, ISettingsService settingsService)
        {
            _downloadService = downloadService;
            _settings = settingsService.Settings;

            _downloadService.DownloadCompleted += OnDownloadCompleted;
            _downloadService.DownloadFailed += OnDownloadFailed;
            _downloadService.DownloadStatusChanged += OnDownloadStatusChanged;
        }

        public async Task EnqueueAsync(DownloadItem item)
        {
            var queueItem = new QueueItem
            {
                DownloadItemId = item.Id,
                DownloadItem = item,
                Position = _queue.Count + 1,
                Status = QueueStatus.Waiting
            };

            _queue.Add(queueItem);
            QueueChanged?.Invoke(this, queueItem);

            if (_settings.AutoStartQueuedDownloads)
                await TryStartNextAsync();
        }

        public Task DequeueAsync(string queueItemId)
        {
            var qi = _queue.FirstOrDefault(q => q.Id == queueItemId);
            if (qi != null)
            {
                if (qi.Status == QueueStatus.Downloading && qi.DownloadItem != null)
                    _ = _downloadService.CancelDownloadAsync(qi.DownloadItem.Id);

                _queue.Remove(qi);
                ReorderPositions();
                QueueChanged?.Invoke(this, qi);
            }
            return Task.CompletedTask;
        }

        public Task MoveUpAsync(string queueItemId)
        {
            var qi = _queue.FirstOrDefault(q => q.Id == queueItemId);
            if (qi == null || qi.Position <= 1) return Task.CompletedTask;

            var above = _queue.FirstOrDefault(q => q.Position == qi.Position - 1);
            if (above != null)
            {
                above.Position++;
                qi.Position--;
                QueueChanged?.Invoke(this, qi);
            }
            return Task.CompletedTask;
        }

        public Task MoveDownAsync(string queueItemId)
        {
            var qi = _queue.FirstOrDefault(q => q.Id == queueItemId);
            if (qi == null || qi.Position >= _queue.Count) return Task.CompletedTask;

            var below = _queue.FirstOrDefault(q => q.Position == qi.Position + 1);
            if (below != null)
            {
                below.Position--;
                qi.Position++;
                QueueChanged?.Invoke(this, qi);
            }
            return Task.CompletedTask;
        }

        public async Task PauseAllAsync()
        {
            foreach (var qi in _queue.Where(q => q.Status == QueueStatus.Downloading))
            {
                if (qi.DownloadItem != null)
                {
                    qi.DownloadItem.Status = DownloadStatus.Paused;
                    qi.Status = QueueStatus.Paused;
                    await _downloadService.PauseDownloadAsync(qi.DownloadItem.Id);
                }
            }
        }

        public async Task ResumeAllAsync()
        {
            foreach (var qi in _queue.Where(q => q.Status == QueueStatus.Paused))
            {
                if (qi.DownloadItem != null)
                {
                    qi.DownloadItem.Status = DownloadStatus.Downloading;
                    qi.Status = QueueStatus.Downloading;
                    await _downloadService.ResumeDownloadAsync(qi.DownloadItem.Id);
                }
            }
        }

        public Task ClearCompletedAsync()
        {
            var completed = _queue.Where(q => q.Status == QueueStatus.Completed).ToList();
            foreach (var qi in completed)
                _queue.Remove(qi);
            _completedDownloads.Clear();
            ReorderPositions();
            return Task.CompletedTask;
        }

        public async Task StartQueueAsync()
        {
            await TryStartNextAsync();
        }

        private async Task TryStartNextAsync()
        {
            while (_activeCount < _settings.MaxSimultaneousDownloads)
            {
                var next = _queue
                    .Where(q => q.Status == QueueStatus.Waiting)
                    .OrderBy(q => q.Position)
                    .FirstOrDefault();

                if (next?.DownloadItem == null) break;

                next.Status = QueueStatus.Downloading;
                _activeCount++;

                if (!_activeDownloads.Contains(next.DownloadItem))
                    _activeDownloads.Add(next.DownloadItem);

                _ = _downloadService.StartDownloadAsync(next.DownloadItem);
            }

            await Task.CompletedTask;
        }

        private void OnDownloadCompleted(object? sender, DownloadItem item)
        {
            var qi = _queue.FirstOrDefault(q => q.DownloadItemId == item.Id);
            if (qi != null)
            {
                qi.Status = QueueStatus.Completed;
                _activeCount = Math.Max(0, _activeCount - 1);
                _activeDownloads.Remove(item);
                _completedDownloads.Add(item);
                QueueChanged?.Invoke(this, qi);
                _ = TryStartNextAsync();
            }
        }

        private void OnDownloadFailed(object? sender, DownloadItem item)
        {
            var qi = _queue.FirstOrDefault(q => q.DownloadItemId == item.Id);
            if (qi != null)
            {
                qi.Status = QueueStatus.Failed;
                _activeCount = Math.Max(0, _activeCount - 1);
                _activeDownloads.Remove(item);
                QueueChanged?.Invoke(this, qi);

                if (_settings.RetryFailedDownloads && item.RetryCount < _settings.MaxRetryAttempts)
                {
                    item.RetryCount++;
                    item.Status = DownloadStatus.Retrying;
                    qi.Status = QueueStatus.Waiting;
                    _ = TryStartNextAsync();
                }
                else
                {
                    _ = TryStartNextAsync();
                }
            }
        }

        private void OnDownloadStatusChanged(object? sender, DownloadItem item)
        {
            if (item.Status == DownloadStatus.Paused)
            {
                var qi = _queue.FirstOrDefault(q => q.DownloadItemId == item.Id);
                if (qi != null) qi.Status = QueueStatus.Paused;
            }
        }

        private void ReorderPositions()
        {
            int pos = 1;
            foreach (var qi in _queue.OrderBy(q => q.Position))
                qi.Position = pos++;
        }
    }
}
