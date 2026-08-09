using System;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services
{
    public class ThemeService : IThemeService
    {
        public event EventHandler<AppTheme>? ThemeChanged;
        public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

        // Track the resolved theme (Light or Dark) separately from the setting
        private bool _resolvedDark = false;

        public void ApplyTheme(AppTheme theme)
        {
            CurrentTheme = theme;

            bool useDark = theme switch
            {
                AppTheme.Dark   => true,
                AppTheme.Light  => false,
                AppTheme.System => IsSystemDarkMode(),
                _               => false
            };

            _resolvedDark = useDark;
            SwapResourceDictionary(useDark);
            ThemeChanged?.Invoke(this, theme);
        }

        public void ToggleTheme()
        {
            // Toggle between light and dark explicitly (not system)
            ApplyTheme(_resolvedDark ? AppTheme.Light : AppTheme.Dark);
        }

        private void SwapResourceDictionary(bool dark)
        {
            var app = Application.Current;
            if (app == null) return;

            // Find and remove existing theme dict
            ResourceDictionary? existing = null;
            foreach (ResourceDictionary rd in app.Resources.MergedDictionaries)
            {
                var src = rd.Source?.ToString() ?? "";
                if (src.Contains("LightTheme") || src.Contains("DarkTheme"))
                {
                    existing = rd;
                    break;
                }
            }
            if (existing != null)
                app.Resources.MergedDictionaries.Remove(existing);

            string path = dark
                ? "Resources/Themes/DarkTheme.xaml"
                : "Resources/Themes/LightTheme.xaml";

            app.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/{path}", UriKind.Absolute)
            });
        }

        public static bool IsSystemDarkMode()
        {
            try
            {
                const string key = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                using var rk = Registry.CurrentUser.OpenSubKey(key);
                if (rk?.GetValue("AppsUseLightTheme") is int v)
                    return v == 0;
            }
            catch { }
            return false;
        }
    }
}
