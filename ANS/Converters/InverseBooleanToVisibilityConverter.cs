using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ANS.Converters
{
    /// <summary>
    /// Convierte bool a Visibility invertido: true -> Collapsed, false -> Visible.
    /// Útil para mostrar controles cuando NO hay selección (ej. lista de resultados de búsqueda).
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
