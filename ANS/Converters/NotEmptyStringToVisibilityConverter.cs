using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ANS.Converters
{
    /// <summary>
    /// Convierte string a Visibility: Visible cuando no es null ni vacío, si no Collapsed.
    /// </summary>
    public class NotEmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
