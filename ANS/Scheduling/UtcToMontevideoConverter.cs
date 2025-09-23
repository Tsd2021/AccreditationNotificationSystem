using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ANS.Scheduling
{
    public class UtcToMontevideoConverter : IValueConverter
    {
        private static readonly TimeZoneInfo Tz =
            TimeZoneInfo.FindSystemTimeZoneById("Montevideo Standard Time");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return null;

            DateTimeOffset dto;
            if (value is DateTimeOffset off)
                dto = off;
            else if (value is DateTime dt)
                dto = new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
            else if (value is string s && DateTimeOffset.TryParse(s, out var parsed))
                dto = parsed;
            else
                return value; // tipo no esperado, devuelvo tal cual

            // El valor viene en UTC → lo convierto a Montevideo
            return TimeZoneInfo.ConvertTime(dto, Tz);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
