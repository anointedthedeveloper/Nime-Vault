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
        private readonly INotificationService _notifications;

        private Anime? _anime;
        private bool _isLoading;
        private bool _isSubSelected = true;

        public Anime? Anime     { get => _anime;     set => SetField(ref _anime,     value); }
        public bool IsLoading   { get => _isLoading; set => SetField(ref _isLoading, value); }

        public bool IsSubSelected
        {
            get => _isSubSelected;
            set
            {
                SetField(ref _isSubSelected, value);
                OnPropertyChanged(nameof(IsDubSelected));
                OnPropertyChanged(nameof(SelectedLanguage));
            }
        }
        public bool IsDubSelected    => !_isSubSelected;
        public string SelectedLanguage => _isSubSelected ? "SUB" : "DUB";

        public ObservableCollection<Episode> Episodes { get; } = new();

        public ICommand LoadCommand             { get; }
        public ICommand SelectSubCommand        { get; }
        public ICommand SelectDubCommand        { get; }
        public ICommand DownloadEpisodeCommand  { get; }
        public ICommand AddEpisodeToQueueCommand { get; }
        public ICommand AddAllToQueueCommand    { get; }
        public ICommand GoBackCommand           { get; }

        public event EventHandler? BackRequested;

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
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _notifications.ShowError($"Couldn't load anime: {ex.Message}");
            }
            finally { IsLoading = false; }
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
            if (Anime == null || Episodes.Count == 0) return;
            foreach (var ep in Episodes)
                await Enqueue(ep);
            _notifications.ShowSuccess($"Added all {Episodes.Count} episodes to queue");
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

    // Generic async relay command — placed here to avoid a separate file
    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        private readonly Func<T?, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
        {
            _execute    = execute;
            _canExecute = canExecute;
        }

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
