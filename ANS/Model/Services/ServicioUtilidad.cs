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

        public static ServicioUtilidad instancia { get; set; }
        public static ServicioUtilidad getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioUtilidad();
            }
            return instancia;
        }
        public string FormatearDoubleConPuntosYComas(double monto)
        {
            CultureInfo culture = new CultureInfo("es-ES");
            return monto.ToString("N2", culture);
        }
    }
}
