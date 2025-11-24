using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Services
{
    public class ServicioUtilidad
    {

        // ✅ Thread-safe: Lazy<T> garantiza inicialización única
        // Servicio de utilidades (formateo), thread-safety no es crítico pero mantiene consistencia
        private static readonly Lazy<ServicioUtilidad> _lazy = 
            new Lazy<ServicioUtilidad>(() => new ServicioUtilidad());
        
        public static ServicioUtilidad instancia => _lazy.Value;
        
        public static ServicioUtilidad getInstancia()
        {
            return _lazy.Value;
        }
        public string FormatearDoubleConPuntosYComas(double monto)
        {
            CultureInfo culture = new CultureInfo("es-ES");
            return monto.ToString("N2", culture);
        }
    }
}
