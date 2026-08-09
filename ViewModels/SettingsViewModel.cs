using System.Threading.Tasks;
using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IThemeService _themeService;
        private readonly INotificationService _notificationService;

        private AppSettings _settings;

        public string DownloadLocation
        {
            get => _settings.DownloadLocation;
            set { _settings.DownloadLocation = value; OnPropertyChanged(); }
        }

        public bool StartWithWindows
        {
            get => _settings.StartWithWindows;
            set { _settings.StartWithWindows = value; OnPropertyChanged(); }
        }

        public bool MinimizeToTray
        {
            get => _settings.MinimizeToTray;
            set { _settings.MinimizeToTray = value; OnPropertyChanged(); }
        }

        public bool ConfirmBeforeDelete
        {
            get => _settings.ConfirmBeforeDelete;
            set { _settings.ConfirmBeforeDelete = value; OnPropertyChanged(); }
        }

        public int MaxSimultaneousDownloads
        {
            get => _settings.MaxSimultaneousDownloads;
            set { _settings.MaxSimultaneousDownloads = value; OnPropertyChanged(); }
        }

        public bool AutoStartQueuedDownloads
        {
            get => _settings.AutoStartQueuedDownloads;
            set { _settings.AutoStartQueuedDownloads = value; OnPropertyChanged(); }
        }

        public bool RetryFailedDownloads
        {
            get => _settings.RetryFailedDownloads;
            set { _settings.RetryFailedDownloads = value; OnPropertyChanged(); }
        }

        public int MaxRetryAttempts
        {
            get => _settings.MaxRetryAttempts;
            set { _settings.MaxRetryAttempts = value; OnPropertyChanged(); }
        }

        public bool ResumeInterruptedDownloads
        {
            get => _settings.ResumeInterruptedDownloads;
            set { _settings.ResumeInterruptedDownloads = value; OnPropertyChanged(); }
        }

        public AppTheme SelectedTheme
        {
            get => _settings.Theme;
            set
            {
                _settings.Theme = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLightTheme));
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(IsSystemTheme));
                _themeService.ApplyTheme(value);
            }
        }

        public bool IsLightTheme
        {
            get => _settings.Theme == AppTheme.Light;
            set { if (value) SelectedTheme = AppTheme.Light; }
        }

        public bool IsDarkTheme
        {
            get => _settings.Theme == AppTheme.Dark;
            set { if (value) SelectedTheme = AppTheme.Dark; }
        }

        public bool IsSystemTheme
        {
            get => _settings.Theme == AppTheme.System;
            set { if (value) SelectedTheme = AppTheme.System; }
        }

        public ICommand BrowseDownloadLocationCommand { get; }
        public ICommand SaveCommand { get; }

        public SettingsViewModel(
            ISettingsService settingsService,
            IThemeService themeService,
            INotificationService notificationService)
        {
            _settingsService = settingsService;
            _themeService = themeService;
            _notificationService = notificationService;
            _settings = settingsService.Settings;

            BrowseDownloadLocationCommand = new RelayCommand(BrowseDownloadLocation);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
        }

        private void BrowseDownloadLocation()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Download Location",
                SelectedPath = DownloadLocation
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                DownloadLocation = dialog.SelectedPath;
        }

        private async Task SaveAsync()
        {
            await _settingsService.SaveAsync();
            _notificationService.ShowSuccess("Settings saved");
        }
    }
}
