using System.Configuration;

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

        public static string Conexion22
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["conexionTSD22"]?.ConnectionString
                       ?? throw new ConfigurationErrorsException("La cadena de conexión 'Conexion22' no está configurada en App.config.");
            }
        }
    }
}
