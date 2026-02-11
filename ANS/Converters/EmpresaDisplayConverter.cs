using ANS.Model.DTOs;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ANS.Converters
{
    /// <summary>
    /// Para el ComboBox de empresas: muestra "TODAS" cuando IdCuenta == 0, sino DisplayNameConCuenta.
    /// </summary>
    public class EmpresaDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EmpresaDto e && e.IdCuenta == 0)
                return "TODAS";
            return (value as EmpresaDto)?.DisplayNameConCuenta ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
