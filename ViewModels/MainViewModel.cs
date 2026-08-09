using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public enum AppPage
    {
        Home,
        Search,
        AnimeDetails,
        Downloads,
        Queue,
        Settings
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly IThemeService _themeService;
        private AppPage _currentPage = AppPage.Home;
        private AppPage _previousPage = AppPage.Home;
        private bool _isDarkTheme;

        public HomeViewModel HomeVM { get; }
        public SearchViewModel SearchVM { get; }
        public AnimeDetailsViewModel AnimeDetailsVM { get; }
        public DownloadsViewModel DownloadsVM { get; }
        public QueueViewModel QueueVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public AppPage CurrentPage
        {
            get => _currentPage;
            set
            {
                SetField(ref _currentPage, value);
                OnPropertyChanged(nameof(IsHomePage));
                OnPropertyChanged(nameof(IsSearchPage));
                OnPropertyChanged(nameof(IsDownloadsPage));
                OnPropertyChanged(nameof(IsQueuePage));
                OnPropertyChanged(nameof(IsSettingsPage));
                OnPropertyChanged(nameof(IsDetailsPage));
            }
        }

        public bool IsHomePage => CurrentPage == AppPage.Home;
        public bool IsSearchPage => CurrentPage == AppPage.Search;
        public bool IsDetailsPage => CurrentPage == AppPage.AnimeDetails;
        public bool IsDownloadsPage => CurrentPage == AppPage.Downloads;
        public bool IsQueuePage => CurrentPage == AppPage.Queue;
        public bool IsSettingsPage => CurrentPage == AppPage.Settings;

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set => SetField(ref _isDarkTheme, value);
        }

        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateSearchCommand { get; }
        public ICommand NavigateDownloadsCommand { get; }
        public ICommand NavigateQueueCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand NavigateSearchIconCommand { get; }

        public MainViewModel(
            IThemeService themeService,
            HomeViewModel homeVM,
            SearchViewModel searchVM,
            AnimeDetailsViewModel animeDetailsVM,
            DownloadsViewModel downloadsVM,
            QueueViewModel queueVM,
            SettingsViewModel settingsVM)
        {
            _themeService = themeService;

            HomeVM = homeVM;
            SearchVM = searchVM;
            AnimeDetailsVM = animeDetailsVM;
            DownloadsVM = downloadsVM;
            QueueVM = queueVM;
            SettingsVM = settingsVM;

            NavigateHomeCommand = new RelayCommand(() => NavigateTo(AppPage.Home));
            NavigateSearchCommand = new RelayCommand(() => NavigateTo(AppPage.Search));
            NavigateDownloadsCommand = new RelayCommand(() => NavigateTo(AppPage.Downloads));
            NavigateQueueCommand = new RelayCommand(() => NavigateTo(AppPage.Queue));
            NavigateSettingsCommand = new RelayCommand(() => NavigateTo(AppPage.Settings));
            NavigateSearchIconCommand = new RelayCommand(() => NavigateTo(AppPage.Search));

            ToggleThemeCommand = new RelayCommand(() =>
            {
                _themeService.ToggleTheme();
                IsDarkTheme = _themeService.CurrentTheme == AppTheme.Dark;
            });

            _themeService.ThemeChanged += (_, theme) =>
            {
                IsDarkTheme = theme == AppTheme.Dark;
            };

            HomeVM.NavigationRequested += (_, anime) => NavigateToDetails(anime);
            SearchVM.AnimeSelected += (_, anime) => NavigateToDetails(anime);
            AnimeDetailsVM.BackRequested += (_, _) => NavigateTo(_previousPage);
        }

        private void NavigateTo(AppPage page)
        {
            if (CurrentPage != AppPage.AnimeDetails)
                _previousPage = CurrentPage;
            CurrentPage = page;
        }

        public void NavigateToDetails(Anime anime)
        {
            _previousPage = CurrentPage;
            CurrentPage = AppPage.AnimeDetails;
            _ = AnimeDetailsVM.LoadAsync(anime.Id);
        }
    }
}
