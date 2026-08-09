using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NimeVault.Services;
using NimeVault.Services.Interfaces;
using NimeVault.Services.Mock;
using NimeVault.ViewModels;

namespace NimeVault
{
    public partial class App : Application
    {
        public static ServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // Services
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<NotificationService>();
            services.AddSingleton<INotificationService>(p => p.GetRequiredService<NotificationService>());

            // Mock services
            services.AddSingleton<IAnimeSearchService, MockAnimeSearchService>();
            services.AddSingleton<IAnimeDetailsService, MockAnimeDetailsService>();
            services.AddSingleton<IDownloadService, MockDownloadService>();
            services.AddSingleton<IDownloadQueueService, MockDownloadQueueService>();

            // ViewModels
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<SearchViewModel>();
            services.AddSingleton<AnimeDetailsViewModel>();
            services.AddSingleton<DownloadsViewModel>();
            services.AddSingleton<QueueViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<LockViewModel>();
            services.AddSingleton<MainViewModel>();

            Services = services.BuildServiceProvider();

            var mainWindow = new Views.MainWindow();
            mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
            mainWindow.Show();
        }
    }
}
