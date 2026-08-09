using System;
using NimeVault.Models;

namespace NimeVault.Services.Interfaces
{
    public interface IThemeService
    {
        event EventHandler<AppTheme>? ThemeChanged;
        AppTheme CurrentTheme { get; }
        void ApplyTheme(AppTheme theme);
        void ToggleTheme();
    }
}
