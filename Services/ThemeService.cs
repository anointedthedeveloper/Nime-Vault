using System;
using System.Windows;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.Services
{
    public class ThemeService : IThemeService
    {
        public event EventHandler<AppTheme>? ThemeChanged;
        public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

        public void ApplyTheme(AppTheme theme)
        {
            CurrentTheme = theme;

            var app = Application.Current;
            if (app == null) return;

            // Remove existing theme dictionaries
            ResourceDictionary? toRemove = null;
            foreach (ResourceDictionary rd in app.Resources.MergedDictionaries)
            {
                var src = rd.Source?.ToString() ?? "";
                if (src.Contains("LightTheme") || src.Contains("DarkTheme"))
                {
                    toRemove = rd;
                    break;
                }
            }
            if (toRemove != null)
                app.Resources.MergedDictionaries.Remove(toRemove);

            string themePath = theme == AppTheme.Dark
                ? "Resources/Themes/DarkTheme.xaml"
                : "Resources/Themes/LightTheme.xaml";

            var dict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/{themePath}", UriKind.Absolute)
            };
            app.Resources.MergedDictionaries.Add(dict);

            ThemeChanged?.Invoke(this, theme);
        }

        public void ToggleTheme()
        {
            ApplyTheme(CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
        }
    }
}
