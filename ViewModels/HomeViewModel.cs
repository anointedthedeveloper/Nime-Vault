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
        private readonly INotificationService _notificationService;

        private Anime? _featuredAnime;
        private bool _isLoading;

        public Anime? FeaturedAnime { get => _featuredAnime; set => SetField(ref _featuredAnime, value); }
        public bool IsLoading { get => _isLoading; set => SetField(ref _isLoading, value); }

        public ObservableCollection<Anime> PopularAnime { get; } = new();
        public ObservableCollection<Anime> RecentlyAdded { get; } = new();

        public ICommand LoadCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand AddToQueueCommand { get; }

        public HomeViewModel(
            IAnimeSearchService searchService,
            IDownloadQueueService queueService,
            INotificationService notificationService)
        {
            _searchService = searchService;
            _queueService = queueService;
            _notificationService = notificationService;

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            ViewDetailsCommand = new RelayCommand<Anime>(NavigateToDetails);
            AddToQueueCommand = new RelayCommand<Anime>(AddToQueue);
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var featured = await _searchService.GetFeaturedAsync();
                FeaturedAnime = featured;

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
            if (anime == null) return;
            NavigationRequested?.Invoke(this, anime);
        }

        private void AddToQueue(Anime? anime)
        {
            if (anime == null) return;
            _notificationService.ShowSuccess($"Added {anime.Title} to queue");
        }

        public event System.EventHandler<Anime>? NavigationRequested;
    }
}
