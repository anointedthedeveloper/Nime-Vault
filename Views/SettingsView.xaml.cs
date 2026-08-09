using System.Windows;
using System.Windows.Controls;
using NimeVault.ViewModels;

namespace NimeVault.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private SettingsViewModel? VM => DataContext as SettingsViewModel;

        private void DecrementSimultaneous(object sender, RoutedEventArgs e)
        {
            if (VM != null && VM.MaxSimultaneousDownloads > 1)
                VM.MaxSimultaneousDownloads--;
        }

        private void IncrementSimultaneous(object sender, RoutedEventArgs e)
        {
            if (VM != null && VM.MaxSimultaneousDownloads < 10)
                VM.MaxSimultaneousDownloads++;
        }

        private void DecrementRetry(object sender, RoutedEventArgs e)
        {
            if (VM != null && VM.MaxRetryAttempts > 0)
                VM.MaxRetryAttempts--;
        }

        private void IncrementRetry(object sender, RoutedEventArgs e)
        {
            if (VM != null && VM.MaxRetryAttempts < 10)
                VM.MaxRetryAttempts++;
        }

        private void LightThemeSelected(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (VM != null) VM.IsLightTheme = true;
        }

        private void DarkThemeSelected(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (VM != null) VM.IsDarkTheme = true;
        }

        private void SystemThemeSelected(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (VM != null) VM.IsSystemTheme = true;
        }
    }
}
