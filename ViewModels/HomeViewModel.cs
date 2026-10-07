using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using NimeVault.Models;
using NimeVault.Services;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private readonly IAnimeSearchService _searchService;
        private readonly IAnimeDetailsService _detailsService;
        private readonly IDownloadQueueService _queueService;
        private readonly INotificationService _notifications;
        private readonly DispatcherTimer _spotlightTimer;

        private bool _isLoading;
        private int _spotlightIndex;
        private Anime? _currentSpotlight;

        public bool IsLoading { get => _isLoading; set => SetField(ref _isLoading, value); }

        public Anime? CurrentSpotlight { get => _currentSpotlight; set => SetField(ref _currentSpotlight, value); }

        public int SpotlightIndex
        {
            get => _spotlightIndex;
            set
            {
                SetField(ref _spotlightIndex, value);
                if (SpotlightAnime.Count > 0 && value >= 0 && value < SpotlightAnime.Count)
                    CurrentSpotlight = SpotlightAnime[value];
            }
        }

        public ObservableCollection<Anime> SpotlightAnime { get; } = new();
        public ObservableCollection<Anime> PopularAnime   { get; } = new();
        public ObservableCollection<Anime> RecentlyAdded  { get; } = new();

        public ICommand LoadCommand        { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand SpotlightNextCommand { get; }
        public ICommand SpotlightPrevCommand { get; }

        public event EventHandler<Anime>? NavigationRequested;

        public HomeViewModel(
            IAnimeSearchService searchService,
            IAnimeDetailsService detailsService,
            IDownloadQueueService queueService,
            INotificationService notifications)
        {
            _searchService = searchService;
            _detailsService = detailsService;
            _queueService  = queueService;
            _notifications = notifications;

            LoadCommand          = new AsyncRelayCommand(LoadAsync);
            ViewDetailsCommand   = new RelayCommand<Anime>(a => { if (a != null) NavigationRequested?.Invoke(this, a); });
            SpotlightNextCommand = new RelayCommand(SpotlightNext);
            SpotlightPrevCommand = new RelayCommand(SpotlightPrev);

            _spotlightTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _spotlightTimer.Tick += (_, _) => SpotlightNext();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                // Spotlight slides
                if (_searchService is AniWavesSearchService aws)
                {
                    var spotlight = await aws.GetSpotlightAsync();
                    SpotlightAnime.Clear();
                    foreach (var a in spotlight) SpotlightAnime.Add(a);
                    SpotlightIndex = 0;
                    CurrentSpotlight = SpotlightAnime.Count > 0 ? SpotlightAnime[0] : null;
                    if (SpotlightAnime.Count > 1) _spotlightTimer.Start();
                }

                // Popular (latest episode section)
                var popular = await _searchService.GetPopularAsync();
                PopularAnime.Clear();
                foreach (var a in popular) PopularAnime.Add(a);

                // Recently added
                var recent = await _searchService.GetRecentlyAddedAsync();
                RecentlyAdded.Clear();
                foreach (var a in recent) RecentlyAdded.Add(a);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _notifications.ShowError($"Couldn't load AnimePahe: {ex.Message}");
            }
            finally { IsLoading = false; }
        }

        private void SpotlightNext()
        {
            if (SpotlightAnime.Count == 0) return;
            SpotlightIndex = (SpotlightIndex + 1) % SpotlightAnime.Count;
        }

        private void SpotlightPrev()
        {
            if (SpotlightAnime.Count == 0) return;
            SpotlightIndex = (SpotlightIndex - 1 + SpotlightAnime.Count) % SpotlightAnime.Count;
        }
    }
}
