using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private readonly INotificationService _notifications;

        private Anime? _anime;
        private bool _isLoading;
        private bool _isSubSelected = true;
        private List<Episode> _allEpisodes = new();
        private int _currentPage = 1;
        private const int PageSize = 20;

        public Anime? Anime     { get => _anime;     set => SetField(ref _anime,     value); }
        public bool IsLoading   { get => _isLoading; set => SetField(ref _isLoading, value); }

        public bool IsSubSelected
        {
            get => _isSubSelected;
            set { SetField(ref _isSubSelected, value); OnPropertyChanged(nameof(IsDubSelected)); OnPropertyChanged(nameof(SelectedLanguage)); }
        }
        public bool IsDubSelected    => !_isSubSelected;
        public string SelectedLanguage => _isSubSelected ? "SUB" : "DUB";

        public int CurrentPage
        {
            get => _currentPage;
            set { SetField(ref _currentPage, value); RefreshPage(); OnPropertyChanged(nameof(TotalPages)); OnPropertyChanged(nameof(CanPrevPage)); OnPropertyChanged(nameof(CanNextPage)); OnPropertyChanged(nameof(PageLabel)); }
        }
        public int TotalPages  => (int)Math.Ceiling(_allEpisodes.Count / (double)PageSize);
        public bool CanPrevPage => _currentPage > 1;
        public bool CanNextPage => _currentPage < TotalPages;
        public string PageLabel => TotalPages > 0 ? $"Page {_currentPage} / {TotalPages}" : "";

        public ObservableCollection<Episode> Episodes { get; } = new();

        public ICommand LoadCommand              { get; }
        public ICommand SelectSubCommand         { get; }
        public ICommand SelectDubCommand         { get; }
        public ICommand DownloadEpisodeCommand   { get; }
        public ICommand AddEpisodeToQueueCommand { get; }
        public ICommand AddAllToQueueCommand     { get; }
        public ICommand GoBackCommand            { get; }
        public ICommand WatchEpisodeCommand      { get; }
        public ICommand PrevPageCommand          { get; }
        public ICommand NextPageCommand          { get; }

        public event EventHandler? BackRequested;
        public event EventHandler<(Episode ep, string language)>? WatchRequested;

        public AnimeDetailsViewModel(
            IAnimeDetailsService detailsService,
            IDownloadQueueService queueService,
            INotificationService notifications)
        {
            _detailsService = detailsService;
            _queueService   = queueService;
            _notifications  = notifications;

            LoadCommand              = new AsyncRelayCommand<string>(LoadAsync);
            SelectSubCommand         = new RelayCommand(() => IsSubSelected = true);
            SelectDubCommand         = new RelayCommand(() => IsSubSelected = false);
            DownloadEpisodeCommand   = new AsyncRelayCommand<Episode>(DownloadEpisodeAsync);
            AddEpisodeToQueueCommand = new AsyncRelayCommand<Episode>(AddEpisodeToQueueAsync);
            AddAllToQueueCommand     = new AsyncRelayCommand(AddAllToQueueAsync);
            GoBackCommand            = new RelayCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));
            WatchEpisodeCommand      = new RelayCommand<Episode>(ep => { if (ep != null) WatchRequested?.Invoke(this, (ep, SelectedLanguage)); });
            PrevPageCommand          = new RelayCommand(() => { if (CanPrevPage) CurrentPage--; });
            NextPageCommand          = new RelayCommand(() => { if (CanNextPage) CurrentPage++; });
        }

        public async Task LoadAsync(string? animeId)
        {
            if (string.IsNullOrWhiteSpace(animeId)) return;
            IsLoading = true;
            _allEpisodes.Clear();
            Episodes.Clear();
            _currentPage = 1;
            try
            {
                Anime = await _detailsService.GetDetailsAsync(animeId);
                _allEpisodes = await _detailsService.GetEpisodesAsync(animeId);
                RefreshPage();
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CanPrevPage));
                OnPropertyChanged(nameof(CanNextPage));
                OnPropertyChanged(nameof(PageLabel));
            }
            finally { IsLoading = false; }
        }

        private void RefreshPage()
        {
            Episodes.Clear();
            var page = _allEpisodes
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize);
            foreach (var ep in page) Episodes.Add(ep);
        }

        private async Task DownloadEpisodeAsync(Episode? ep)
        {
            if (ep == null || Anime == null) return;
            await Enqueue(ep);
            _notifications.ShowSuccess($"Download started: {Anime.Title} Ep {ep.Number}");
        }

        private async Task AddEpisodeToQueueAsync(Episode? ep)
        {
            if (ep == null || Anime == null) return;
            await Enqueue(ep);
            _notifications.Show($"Added to queue: {Anime.Title} Ep {ep.Number}");
        }

        private async Task AddAllToQueueAsync()
        {
            if (Anime == null || _allEpisodes.Count == 0) return;
            foreach (var ep in _allEpisodes) await Enqueue(ep);
            _notifications.ShowSuccess($"Added all {_allEpisodes.Count} episodes to queue");
        }

        private async Task Enqueue(Episode ep)
        {
            if (Anime == null) return;
            await _queueService.EnqueueAsync(new DownloadItem
            {
                AnimeId       = Anime.Id,
                EpisodeId     = ep.Id,
                AnimeTitle    = Anime.Title,
                EpisodeTitle  = ep.Title,
                EpisodeNumber = ep.Number,
                Language      = SelectedLanguage,
                PosterUrl     = Anime.PosterUrl
            });
        }
    }

    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        private readonly Func<T?, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
        { _execute = execute; _canExecute = canExecute; }

        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
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
            try { if (parameter is T t) await _execute(t); else await _execute(default); }
            finally { _isExecuting = false; CommandManager.InvalidateRequerySuggested(); }
        }
    }
}
