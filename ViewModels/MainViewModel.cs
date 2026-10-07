using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public enum AppPage
    {
        Home, Search, Browse, AZList, AnimeDetails, Downloads, Queue, Settings, Player
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly IThemeService _themeService;
        private AppPage _currentPage = AppPage.Home;
        private AppPage _previousPage = AppPage.Home;
        private bool _isDarkTheme;

        public HomeViewModel        HomeVM        { get; }
        public SearchViewModel      SearchVM      { get; }
        public BrowseViewModel      BrowseVM      { get; }
        public AZListViewModel      AZListVM      { get; }
        public AnimeDetailsViewModel AnimeDetailsVM { get; }
        public DownloadsViewModel   DownloadsVM   { get; }
        public QueueViewModel       QueueVM       { get; }
        public SettingsViewModel    SettingsVM    { get; }
        public PlayerViewModel      PlayerVM      { get; }

        public AppPage CurrentPage
        {
            get => _currentPage;
            set
            {
                SetField(ref _currentPage, value);
                OnPropertyChanged(nameof(IsHomePage));
                OnPropertyChanged(nameof(IsSearchPage));
                OnPropertyChanged(nameof(IsBrowsePage));
                OnPropertyChanged(nameof(IsAZPage));
                OnPropertyChanged(nameof(IsDetailsPage));
                OnPropertyChanged(nameof(IsDownloadsPage));
                OnPropertyChanged(nameof(IsQueuePage));
                OnPropertyChanged(nameof(IsSettingsPage));
                OnPropertyChanged(nameof(IsPlayerPage));
                OnPropertyChanged(nameof(HomeNavTag));
                OnPropertyChanged(nameof(SearchNavTag));
                OnPropertyChanged(nameof(BrowseNavTag));
                OnPropertyChanged(nameof(AZNavTag));
                OnPropertyChanged(nameof(DownloadsNavTag));
                OnPropertyChanged(nameof(QueueNavTag));
                OnPropertyChanged(nameof(SettingsNavTag));
            }
        }

        public bool IsHomePage      => CurrentPage == AppPage.Home;
        public bool IsSearchPage    => CurrentPage == AppPage.Search;
        public bool IsBrowsePage    => CurrentPage == AppPage.Browse;
        public bool IsAZPage        => CurrentPage == AppPage.AZList;
        public bool IsDetailsPage   => CurrentPage == AppPage.AnimeDetails;
        public bool IsDownloadsPage => CurrentPage == AppPage.Downloads;
        public bool IsQueuePage     => CurrentPage == AppPage.Queue;
        public bool IsSettingsPage  => CurrentPage == AppPage.Settings;
        public bool IsPlayerPage    => CurrentPage == AppPage.Player;

        // "Active" only for the exact matching page — clears automatically when page changes
        public string HomeNavTag      => IsHomePage      ? "Active" : "";
        public string SearchNavTag    => IsSearchPage    ? "Active" : "";
        public string BrowseNavTag    => IsBrowsePage    ? "Active" : "";
        public string AZNavTag        => IsAZPage        ? "Active" : "";
        public string DownloadsNavTag => IsDownloadsPage ? "Active" : "";
        public string QueueNavTag     => IsQueuePage     ? "Active" : "";
        public string SettingsNavTag  => IsSettingsPage  ? "Active" : "";

        public bool IsDarkTheme { get => _isDarkTheme; set => SetField(ref _isDarkTheme, value); }

        public ICommand NavigateHomeCommand       { get; }
        public ICommand NavigateSearchCommand     { get; }
        public ICommand NavigateBrowseCommand     { get; }
        public ICommand NavigateAZCommand         { get; }
        public ICommand NavigateDownloadsCommand  { get; }
        public ICommand NavigateQueueCommand      { get; }
        public ICommand NavigateSettingsCommand   { get; }
        public ICommand ToggleThemeCommand        { get; }
        public ICommand NavigateSearchIconCommand { get; }

        public MainViewModel(
            IThemeService themeService,
            HomeViewModel homeVM,
            SearchViewModel searchVM,
            BrowseViewModel browseVM,
            AZListViewModel azListVM,
            AnimeDetailsViewModel animeDetailsVM,
            DownloadsViewModel downloadsVM,
            QueueViewModel queueVM,
            SettingsViewModel settingsVM,
            PlayerViewModel playerVM)
        {
            _themeService  = themeService;
            HomeVM         = homeVM;
            SearchVM       = searchVM;
            BrowseVM       = browseVM;
            AZListVM       = azListVM;
            AnimeDetailsVM = animeDetailsVM;
            DownloadsVM    = downloadsVM;
            QueueVM        = queueVM;
            SettingsVM     = settingsVM;
            PlayerVM       = playerVM;

            NavigateHomeCommand       = new RelayCommand(() => NavigateTo(AppPage.Home));
            NavigateSearchCommand     = new RelayCommand(() => NavigateTo(AppPage.Search));
            NavigateBrowseCommand     = new RelayCommand(() => NavigateTo(AppPage.Browse));
            NavigateAZCommand         = new RelayCommand(() => NavigateTo(AppPage.AZList));
            NavigateDownloadsCommand  = new RelayCommand(() => NavigateTo(AppPage.Downloads));
            NavigateQueueCommand      = new RelayCommand(() => NavigateTo(AppPage.Queue));
            NavigateSettingsCommand   = new RelayCommand(() => NavigateTo(AppPage.Settings));
            NavigateSearchIconCommand = new RelayCommand(() => NavigateTo(AppPage.Search));

            ToggleThemeCommand = new RelayCommand(() => { _themeService.ToggleTheme(); SyncDarkFlag(); });
            _themeService.ThemeChanged += (_, _) => SyncDarkFlag();
            SyncDarkFlag();

            HomeVM.NavigationRequested        += (_, a) => NavigateToDetails(a);
            SearchVM.AnimeSelected            += (_, a) => NavigateToDetails(a);
            BrowseVM.NavigationRequested      += (_, a) => NavigateToDetails(a);
            AZListVM.NavigationRequested      += (_, a) => NavigateToDetails(a);
            AnimeDetailsVM.BackRequested      += (_, _) => NavigateTo(_previousPage);
            AnimeDetailsVM.WatchRequested     += (_, args) =>
            {
                if (AnimeDetailsVM.Anime == null) return;
                _previousPage = CurrentPage;
                CurrentPage = AppPage.Player;
                _ = PlayerVM.LoadAsync(AnimeDetailsVM.Anime.Id, args.ep.Number,
                    args.language.ToLower(), AnimeDetailsVM.Anime.Title);
            };
            PlayerVM.CloseRequested += (_, _) => NavigateTo(_previousPage);
        }

        private void SyncDarkFlag()
        {
            IsDarkTheme = ThemeService.IsSystemDarkMode() && _themeService.CurrentTheme == AppTheme.System
                       || _themeService.CurrentTheme == AppTheme.Dark;
        }

        private void NavigateTo(AppPage page)
        {
            if (CurrentPage != AppPage.AnimeDetails && CurrentPage != AppPage.Player)
                _previousPage = CurrentPage;
            CurrentPage = page;

            // Lazy-load pages on first visit
            if (page == AppPage.Browse && BrowseVM.TopAnime.Count == 0)
                _ = BrowseVM.LoadAllAsync();
            if (page == AppPage.AZList && AZListVM.Anime.Count == 0)
                _ = AZListVM.LoadAsync();
        }

        public void NavigateToDetails(Anime anime)
        {
            _previousPage = CurrentPage;
            CurrentPage   = AppPage.AnimeDetails;
            _ = AnimeDetailsVM.LoadAsync(anime.Id);
        }
    }
}
