using System;
using System.Globalization;
using System.Windows.Data;

namespace ANS.Converters
{
    /// <summary>
    /// Convierte bool IsAcreditado a texto "Acreditado" / "No acreditado" para columna Estado.
    /// </summary>
    public class BoolToEstadoAcreditacionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? "Acreditado" : "No acreditado";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
