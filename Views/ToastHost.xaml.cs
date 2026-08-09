using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NimeVault.Services.Interfaces;

namespace NimeVault.Views
{
    public class ToastItem
    {
        public string Message { get; set; } = string.Empty;
        public Brush Background { get; set; } = Brushes.White;
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush AccentColor { get; set; } = Brushes.Purple;
    }

    public partial class ToastHost : UserControl
    {
        private readonly ObservableCollection<ToastItem> _toasts = new();

        public ToastHost()
        {
            InitializeComponent();
            ToastList.ItemsSource = _toasts;
        }

        public async void ShowToast(string message, NotificationType type, int durationMs = 3000)
        {
            var (bg, fg, accent) = type switch
            {
                NotificationType.Success => (
                    new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4)),
                    new SolidColorBrush(Color.FromRgb(0x16, 0x6A, 0x34)),
                    new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))),
                NotificationType.Error => (
                    new SolidColorBrush(Color.FromRgb(0xFF, 0xF1, 0xF2)),
                    new SolidColorBrush(Color.FromRgb(0x9B, 0x1C, 0x1C)),
                    new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))),
                NotificationType.Warning => (
                    new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xEB)),
                    new SolidColorBrush(Color.FromRgb(0x78, 0x35, 0x00)),
                    new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B))),
                _ => (
                    new SolidColorBrush(Color.FromRgb(0xF5, 0xF3, 0xFF)),
                    new SolidColorBrush(Color.FromRgb(0x4C, 0x1D, 0x95)),
                    new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)))
            };

            var toast = new ToastItem
            {
                Message = message,
                Background = (Brush)bg,
                Foreground = (Brush)fg,
                AccentColor = (Brush)accent
            };

            _toasts.Add(toast);

            await Task.Delay(durationMs);

            if (_toasts.Contains(toast))
                _toasts.Remove(toast);
        }
    }
}
