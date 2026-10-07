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

            // Find index of existing theme dict
            int existingIndex = -1;
            ResourceDictionary? existing = null;
            for (int i = 0; i < app.Resources.MergedDictionaries.Count; i++)
            {
                var src = app.Resources.MergedDictionaries[i].Source?.ToString() ?? "";
                if (src.Contains("LightTheme") || src.Contains("DarkTheme"))
                {
                    existing = app.Resources.MergedDictionaries[i];
                    existingIndex = i;
                    break;
                }
            }

            var newDict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/Resources/Themes/{(dark ? "Dark" : "Light")}Theme.xaml", UriKind.Absolute)
            };

            if (existing != null)
            {
                // Replace in-place to keep ordering
                app.Resources.MergedDictionaries[existingIndex] = newDict;
            }
            else
            {
                app.Resources.MergedDictionaries.Add(newDict);
            }
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
