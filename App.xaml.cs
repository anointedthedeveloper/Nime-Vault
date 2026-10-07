using System;
using System.IO;
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

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                Log("OnStartup begin");
                base.OnStartup(e);

                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                DispatcherUnhandledException += App_DispatcherUnhandledException;

                var services = new ServiceCollection();

                // Core services
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<NotificationService>();
                services.AddSingleton<INotificationService>(p => p.GetRequiredService<NotificationService>());

                // Real anime data services (aniwaves.ru)
                services.AddSingleton<IAnimeSearchService, AniWavesSearchService>();
                services.AddSingleton<IAnimeDetailsService, AniWavesDetailsService>();

                // Real stream + download services
                services.AddSingleton<IStreamService, PlaywrightStreamService>();
                services.AddSingleton<IDownloadService, HlsDownloadService>();

                // Queue service (still uses the real queue logic, just with real download service)
                services.AddSingleton<IDownloadQueueService, MockDownloadQueueService>();

                // ViewModels
                services.AddSingleton<HomeViewModel>();
                services.AddSingleton<SearchViewModel>();
                services.AddSingleton<BrowseViewModel>();
                services.AddSingleton<AZListViewModel>();
                services.AddSingleton<AnimeDetailsViewModel>();
                services.AddSingleton<DownloadsViewModel>();
                services.AddSingleton<QueueViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<PlayerViewModel>();
                services.AddSingleton<MainViewModel>();

                Services = services.BuildServiceProvider();

                var settingsService = Services.GetRequiredService<ISettingsService>();
                await settingsService.LoadAsync();

                var themeService = Services.GetRequiredService<IThemeService>();
                themeService.ApplyTheme(settingsService.Settings.Theme);

                DispatcherUnhandledException += App_DispatcherUnhandledException;

                var mainWindow = new Views.MainWindow();
                mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
                mainWindow.Show();
                Log("Main window shown");
            }
            catch (Exception ex)
            {
                Log($"Startup failure: {ex}");
                System.Windows.MessageBox.Show(ex.Message, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log($"Unhandled domain exception: {e.ExceptionObject}");
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var text = $"Unhandled exception: {e.Exception.Message}\n\n{e.Exception}";
            Log(text);
            System.Windows.MessageBox.Show(text, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "startup-error.txt"), e.Exception.ToString());
            e.Handled = true;
            Shutdown();
        }

        private static readonly string _logPath = Path.Combine(AppContext.BaseDirectory, "startup-log.txt");
        private static bool _logInitialized;

        private static void Log(string message)
        {
            try
            {
                var line = $"[{DateTime.Now:O}] {message}{Environment.NewLine}";
                if (!_logInitialized)
                {
                    File.WriteAllText(_logPath, line);
                    _logInitialized = true;
                }
                else
                {
                    File.AppendAllText(_logPath, line);
                }
            }
            catch { }
        }
    }
}
