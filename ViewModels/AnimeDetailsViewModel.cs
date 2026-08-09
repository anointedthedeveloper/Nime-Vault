using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public class AnimeDetailsViewModel : BaseViewModel
    {
        private readonly IAnimeDetailsService _detailsService;
        private readonly IDownloadQueueService _queueService;
        private readonly INotificationService _notificationService;

        private Anime? _anime;
        private bool _isLoading;
        private bool _isSubSelected = true;
        private string _selectedLanguage = "SUB";

        public Anime? Anime { get => _anime; set => SetField(ref _anime, value); }
        public bool IsLoading { get => _isLoading; set => SetField(ref _isLoading, value); }
        public bool IsSubSelected
        {
            get => _isSubSelected;
            set
            {
                SetField(ref _isSubSelected, value);
                _selectedLanguage = value ? "SUB" : "DUB";
                OnPropertyChanged(nameof(IsDubSelected));
                OnPropertyChanged(nameof(SelectedLanguage));
            }
        }
        public bool IsDubSelected => !_isSubSelected;
        public string SelectedLanguage => _selectedLanguage;

        public ObservableCollection<Episode> Episodes { get; } = new();

        public ICommand LoadCommand { get; }
        public ICommand SelectSubCommand { get; }
        public ICommand SelectDubCommand { get; }
        public ICommand DownloadEpisodeCommand { get; }
        public ICommand AddEpisodeToQueueCommand { get; }
        public ICommand GoBackCommand { get; }

        public event EventHandler? BackRequested;

        public AnimeDetailsViewModel(
            IAnimeDetailsService detailsService,
            IDownloadQueueService queueService,
            INotificationService notificationService)
        {
            _detailsService = detailsService;
            _queueService = queueService;
            _notificationService = notificationService;

            LoadCommand = new AsyncRelayCommand<string>(LoadAsync);
            SelectSubCommand = new RelayCommand(() => IsSubSelected = true);
            SelectDubCommand = new RelayCommand(() => IsSubSelected = false);
            DownloadEpisodeCommand = new AsyncRelayCommand<Episode>(DownloadEpisodeAsync);
            AddEpisodeToQueueCommand = new AsyncRelayCommand<Episode>(AddEpisodeToQueueAsync);
            GoBackCommand = new RelayCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));
        }

        public async Task LoadAsync(string? animeId)
        {
            if (string.IsNullOrWhiteSpace(animeId)) return;

            IsLoading = true;
            try
            {
                Anime = await _detailsService.GetDetailsAsync(animeId);
                var eps = await _detailsService.GetEpisodesAsync(animeId);
                Episodes.Clear();
                foreach (var ep in eps) Episodes.Add(ep);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DownloadEpisodeAsync(Episode? episode)
        {
            if (episode == null || Anime == null) return;
            await AddToQueueInternal(episode);
            _notificationService.ShowSuccess($"Download started: {Anime.Title} Ep {episode.Number}");
        }

        private async Task AddEpisodeToQueueAsync(Episode? episode)
        {
            if (episode == null || Anime == null) return;
            await AddToQueueInternal(episode);
            _notificationService.Show($"Added to queue: {Anime.Title} Ep {episode.Number}");
        }

        private async Task AddToQueueInternal(Episode episode)
        {
            if (Anime == null) return;
            var item = new DownloadItem
            {
                AnimeId = Anime.Id,
                EpisodeId = episode.Id,
                AnimeTitle = Anime.Title,
                EpisodeTitle = episode.Title,
                EpisodeNumber = episode.Number,
                Language = _selectedLanguage,
                PosterUrl = Anime.PosterUrl
            };
            await _queueService.EnqueueAsync(item);
        }
    }

    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        private readonly Func<T?, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (_isExecuting) return false;
            if (parameter is T t) return _canExecute?.Invoke(t) ?? true;
            return _canExecute?.Invoke(default) ?? true;
        }

        public async void Execute(object? parameter)
        {
            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();
            try
            {
                if (parameter is T t) await _execute(t);
                else await _execute(default);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
