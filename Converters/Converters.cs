using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NimeVault.Models;

namespace NimeVault.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool bv && bv;
            bool invert = parameter?.ToString() == "Invert";
            if (invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool bv && bv;
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrWhiteSpace(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class RatingToStarsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d) return $"★ {d:F1}";
            return "★ --";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class GenreListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Collections.Generic.List<string> list)
                return string.Join(" · ", list);
            return string.Empty;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DownloadStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DownloadStatus status)
            {
                return status switch
                {
                    DownloadStatus.Downloading => new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)),
                    DownloadStatus.Paused => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                    DownloadStatus.Completed => new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E)),
                    DownloadStatus.Failed => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
                    DownloadStatus.Retrying => new SolidColorBrush(Color.FromRgb(0xEC, 0x48, 0x99)),
                    DownloadStatus.Cancelled => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                    _ => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B))
                };
            }
            return Brushes.Gray;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class DownloadStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DownloadStatus status)
            {
                return status switch
                {
                    DownloadStatus.Queued => "Queued",
                    DownloadStatus.Downloading => "Downloading",
                    DownloadStatus.Paused => "Paused",
                    DownloadStatus.Completed => "Completed",
                    DownloadStatus.Failed => "Failed",
                    DownloadStatus.Retrying => "Retrying",
                    DownloadStatus.Cancelled => "Cancelled",
                    _ => status.ToString()
                };
            }
            return string.Empty;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class EqualityToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() == parameter?.ToString();
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value == null ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToActiveNavConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool active = value is bool b && b;
            return active
                ? new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED))
                : new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

namespace NimeVault.Converters
{
    public class ProgressWidthConverter : System.Windows.Data.IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3 &&
                values[0] is double value &&
                values[1] is double maximum &&
                values[2] is double totalWidth &&
                maximum > 0)
            {
                return totalWidth * (value / maximum);
            }
            return 0.0;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
