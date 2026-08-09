using System;
using System.Globalization;
using System.Windows.Data;

namespace NimeVault.Converters
{
    public class ActiveTagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? "Active" : "Inactive";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
