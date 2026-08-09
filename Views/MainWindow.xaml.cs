using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using NimeVault.Services;
using NimeVault.Services.Interfaces;

namespace NimeVault.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Ensure title bar follows initial theme
            var themeService = App.Services.GetService(typeof(IThemeService)) as IThemeService;
            if (themeService != null)
            {
                ApplyTitleBarTheme(themeService.CurrentTheme == Models.AppTheme.Dark
                    || (themeService.CurrentTheme == Models.AppTheme.System && ThemeService.IsSystemDarkMode()));

                // Subscribe to theme changes to update title bar
                themeService.ThemeChanged += (_, t) =>
                {
                    bool dark = t == Models.AppTheme.Dark || (t == Models.AppTheme.System && ThemeService.IsSystemDarkMode());
                    Dispatcher.Invoke(() => ApplyTitleBarTheme(dark));
                };
            }

            // Wire toast notifications
            var notificationService = App.Services.GetService(typeof(NotificationService)) as NotificationService;
            if (notificationService != null)
                notificationService.NotificationRequested += (_, args) =>
                    ToastHostControl.ShowToast(args.Message, args.Type, args.DurationMs);

            // Kick off home data load
            var vm = App.Services.GetService(typeof(ViewModels.MainViewModel)) as ViewModels.MainViewModel;
            _ = vm?.HomeVM.LoadAsync();
        }

        private void ApplyTitleBarTheme(bool dark)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            // DWMWA_USE_IMMERSIVE_DARK_MODE is reported as 20 on newer builds, 19 on older.
            int attribute = 20;
            int useDark = dark ? 1 : 0;
            try
            {
                DwmSetWindowAttribute(hwnd, attribute, ref useDark, Marshal.SizeOf<int>());
            }
            catch
            {
                // Try fallback attribute
                attribute = 19;
                try { DwmSetWindowAttribute(hwnd, attribute, ref useDark, Marshal.SizeOf<int>()); } catch { }
            }
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}
