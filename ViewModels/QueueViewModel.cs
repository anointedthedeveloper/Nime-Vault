using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public class QueueItemViewModel : BaseViewModel
    {
        private QueueItem _item;

        public QueueItemViewModel(QueueItem item)
        {
            _item = item;
        }

        public string Id => _item.Id;
        public int Position => _item.Position;
        public QueueStatus Status => _item.Status;
        public QueuePriority Priority => _item.Priority;
        public DownloadItem? DownloadItem => _item.DownloadItem;
        public string AnimeTitle => _item.DownloadItem?.AnimeTitle ?? string.Empty;
        public int EpisodeNumber => _item.DownloadItem?.EpisodeNumber ?? 0;
        public string Language => _item.DownloadItem?.Language ?? string.Empty;
        public string PosterUrl => _item.DownloadItem?.PosterUrl ?? string.Empty;
        public bool IsActive => _item.Status == QueueStatus.Downloading;
        public bool IsWaiting => _item.Status == QueueStatus.Waiting;
        public string StatusText => _item.Status.ToString();

        public void Refresh()
        {
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(IsWaiting));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public class QueueViewModel : BaseViewModel
    {
        private readonly IDownloadQueueService _queueService;
        private readonly INotificationService _notificationService;

        public ObservableCollection<QueueItemViewModel> WaitingItems { get; } = new();
        public ObservableCollection<QueueItemViewModel> ActiveItems { get; } = new();

        public bool HasItems => WaitingItems.Count > 0 || ActiveItems.Count > 0;
        public bool HasWaiting => WaitingItems.Count > 0;
        public bool HasActive => ActiveItems.Count > 0;

        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand PauseAllCommand { get; }
        public ICommand ResumeAllCommand { get; }
        public ICommand ClearCompletedCommand { get; }
        public ICommand StartQueueCommand { get; }

        public QueueViewModel(IDownloadQueueService queueService, INotificationService notificationService)
        {
            _queueService = queueService;
            _notificationService = notificationService;

            MoveUpCommand = new AsyncRelayCommand<string>(async id =>
            {
                if (id == null) return;
                await _queueService.MoveUpAsync(id);
                RefreshQueue();
            });

            MoveDownCommand = new AsyncRelayCommand<string>(async id =>
            {
                if (id == null) return;
                await _queueService.MoveDownAsync(id);
                RefreshQueue();
            });

            RemoveCommand = new AsyncRelayCommand<string>(async id =>
            {
                if (id == null) return;
                await _queueService.DequeueAsync(id);
                RefreshQueue();
                _notificationService.Show("Item removed from queue");
            });

            PauseAllCommand = new AsyncRelayCommand(async () =>
            {
                await _queueService.PauseAllAsync();
                RefreshQueue();
                _notificationService.Show("Queue paused");
            });

            ResumeAllCommand = new AsyncRelayCommand(async () =>
            {
                await _queueService.ResumeAllAsync();
                RefreshQueue();
                _notificationService.Show("Queue resumed");
            });

            ClearCompletedCommand = new AsyncRelayCommand(async () =>
            {
                await _queueService.ClearCompletedAsync();
                RefreshQueue();
            });

            StartQueueCommand = new AsyncRelayCommand(async () =>
            {
                await _queueService.StartQueueAsync();
                RefreshQueue();
            });

            _queueService.QueueChanged += (_, _) =>
            {
                Application.Current?.Dispatcher.Invoke(RefreshQueue);
            };
        }

        private void RefreshQueue()
        {
            WaitingItems.Clear();
            ActiveItems.Clear();

            foreach (var qi in _queueService.Queue.OrderBy(q => q.Position))
            {
                var vm = new QueueItemViewModel(qi);
                if (qi.Status == QueueStatus.Downloading)
                    ActiveItems.Add(vm);
                else if (qi.Status == QueueStatus.Waiting || qi.Status == QueueStatus.Paused)
                    WaitingItems.Add(vm);
            }

            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(HasWaiting));
            OnPropertyChanged(nameof(HasActive));
        }
    }
}
