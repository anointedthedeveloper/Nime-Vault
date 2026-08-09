namespace NimeVault.Models
{
    public enum AppTheme
    {
        Light,
        Dark,
        System
    }

    public class AppSettings
    {
        public string DownloadLocation { get; set; } =
            System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads", "Nime Vault");
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public bool ConfirmBeforeDelete { get; set; } = true;
        public int MaxSimultaneousDownloads { get; set; } = 3;
        public bool AutoStartQueuedDownloads { get; set; } = true;
        public bool RetryFailedDownloads { get; set; } = true;
        public int MaxRetryAttempts { get; set; } = 3;
        public bool ResumeInterruptedDownloads { get; set; } = true;
        public AppTheme Theme { get; set; } = AppTheme.Light;
        public bool ShowNotifications { get; set; } = true;
    }
}
