using System.Threading.Tasks;
using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IThemeService _themeService;
        private readonly INotificationService _notifications;
        private AppSettings _s;

        // ── General ────────────────────────────────────────────────────────
        public string DownloadLocation
        {
            get => _s.DownloadLocation;
            set { _s.DownloadLocation = value; OnPropertyChanged(); }
        }
        public bool StartWithWindows
        {
            get => _s.StartWithWindows;
            set { _s.StartWithWindows = value; OnPropertyChanged(); }
        }
        public bool MinimizeToTray
        {
            get => _s.MinimizeToTray;
            set { _s.MinimizeToTray = value; OnPropertyChanged(); }
        }
        public bool ConfirmBeforeDelete
        {
            get => _s.ConfirmBeforeDelete;
            set { _s.ConfirmBeforeDelete = value; OnPropertyChanged(); }
        }
        public bool ShowNotifications
        {
            get => _s.ShowNotifications;
            set { _s.ShowNotifications = value; OnPropertyChanged(); }
        }

        // ── Download ───────────────────────────────────────────────────────
        public int MaxSimultaneousDownloads
        {
            get => _s.MaxSimultaneousDownloads;
            set { _s.MaxSimultaneousDownloads = value; OnPropertyChanged(); }
        }
        public bool AutoStartQueuedDownloads
        {
            get => _s.AutoStartQueuedDownloads;
            set { _s.AutoStartQueuedDownloads = value; OnPropertyChanged(); }
        }
        public bool RetryFailedDownloads
        {
            get => _s.RetryFailedDownloads;
            set { _s.RetryFailedDownloads = value; OnPropertyChanged(); }
        }
        public int MaxRetryAttempts
        {
            get => _s.MaxRetryAttempts;
            set { _s.MaxRetryAttempts = value; OnPropertyChanged(); }
        }
        public bool ResumeInterruptedDownloads
        {
            get => _s.ResumeInterruptedDownloads;
            set { _s.ResumeInterruptedDownloads = value; OnPropertyChanged(); }
        }

        // ── Theme ──────────────────────────────────────────────────────────
        public AppTheme SelectedTheme
        {
            get => _s.Theme;
            set
            {
                _s.Theme = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLightTheme));
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(IsSystemTheme));
                _themeService.ApplyTheme(value);
            }
        }
        public bool IsLightTheme
        {
            get => _s.Theme == AppTheme.Light;
            set { if (value) SelectedTheme = AppTheme.Light; }
        }
        public bool IsDarkTheme
        {
            get => _s.Theme == AppTheme.Dark;
            set { if (value) SelectedTheme = AppTheme.Dark; }
        }
        public bool IsSystemTheme
        {
            get => _s.Theme == AppTheme.System;
            set { if (value) SelectedTheme = AppTheme.System; }
        }

        // ── Commands ───────────────────────────────────────────────────────
        public ICommand BrowseDownloadLocationCommand { get; }
        public ICommand SaveCommand                   { get; }

        public SettingsViewModel(
            ISettingsService settingsService,
            IThemeService themeService,
            INotificationService notifications)
        {
            _settingsService = settingsService;
            _themeService    = themeService;
            _notifications   = notifications;
            _s               = settingsService.Settings;

            BrowseDownloadLocationCommand = new RelayCommand(BrowseFolder);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
        }

        private void BrowseFolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description  = "Select download location",
                SelectedPath = DownloadLocation,
                UseDescriptionForTitle = true
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                DownloadLocation = dialog.SelectedPath;
        }

        private async Task SaveAsync()
        {
            await _settingsService.SaveAsync();
            _notifications.ShowSuccess("Settings saved");
        }
    }
}
