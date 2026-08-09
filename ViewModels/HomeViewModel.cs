using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private readonly IAnimeSearchService _searchService;
        private readonly IDownloadQueueService _queueService;
        private readonly INotificationService _notifications;

        private Anime? _featuredAnime;
        private bool _isLoading;

        public Anime? FeaturedAnime { get => _featuredAnime; set => SetField(ref _featuredAnime, value); }
        public bool IsLoading       { get => _isLoading;     set => SetField(ref _isLoading,     value); }

        public ObservableCollection<Anime> PopularAnime  { get; } = new();
        public ObservableCollection<Anime> RecentlyAdded { get; } = new();

        public ICommand LoadCommand      { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand AddToQueueCommand { get; }

        public event EventHandler<Anime>? NavigationRequested;

        public HomeViewModel(
            IAnimeSearchService searchService,
            IDownloadQueueService queueService,
            INotificationService notifications)
        {
            _searchService = searchService;
            _queueService  = queueService;
            _notifications = notifications;

            LoadCommand       = new AsyncRelayCommand(LoadAsync);
            ViewDetailsCommand = new RelayCommand<Anime>(NavigateToDetails);
            AddToQueueCommand  = new AsyncRelayCommand<Anime>(AddFeaturedToQueueAsync);
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                FeaturedAnime = await _searchService.GetFeaturedAsync();

                var popular = await _searchService.GetPopularAsync();
                PopularAnime.Clear();
                foreach (var a in popular) PopularAnime.Add(a);

                var recent = await _searchService.GetRecentlyAddedAsync();
                RecentlyAdded.Clear();
                foreach (var a in recent) RecentlyAdded.Add(a);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NavigateToDetails(Anime? anime)
        {
            if (anime != null)
                NavigationRequested?.Invoke(this, anime);
        }

        private async Task AddFeaturedToQueueAsync(Anime? anime)
        {
            if (anime == null) return;
            // Queue Episode 1 of the featured anime as a quick-add
            var item = new DownloadItem
            {
                AnimeId       = anime.Id,
                EpisodeId     = $"{anime.Id}-ep-1",
                AnimeTitle    = anime.Title,
                EpisodeTitle  = "Episode 1",
                EpisodeNumber = 1,
                Language      = "SUB",
                PosterUrl     = anime.PosterUrl
            };
            await _queueService.EnqueueAsync(item);
            _notifications.ShowSuccess($"Added {anime.Title} Ep 1 to queue");
        }
    }
}
