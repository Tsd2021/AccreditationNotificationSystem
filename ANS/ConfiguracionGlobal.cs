using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS
{
    public static class ConfiguracionGlobal
    {
        public static string ConexionTSD
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["conexionTSD"]?.ConnectionString
                       ?? throw new ConfigurationErrorsException("La cadena de conexión 'ConexionTSD' no está configurada en App.config.");
            }
        }

        public static string ConexionEncuesta
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["conexionENCUESTA"]?.ConnectionString
                       ?? throw new ConfigurationErrorsException("La cadena de conexión 'ConexionEncuesta' no está configurada en App.config.");
            }
        }

        public static string ConexionWebBuzones
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["conexionWebBuzones"]?.ConnectionString
                       ?? throw new ConfigurationErrorsException("La cadena de conexión 'ConexionWebBuzones' no está configurada en App.config.");
            }
        }
    }
}
