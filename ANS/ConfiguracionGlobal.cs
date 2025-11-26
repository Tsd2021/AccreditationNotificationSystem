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

        /// <summary>
        /// ✅ Clase centralizada para todas las rutas del sistema
        /// Lee desde App.config para fácil configuración sin recompilar
        /// </summary>
        public static class Rutas
        {
            private static string GetAppSetting(string key, string defaultValue = null)
            {
                var value = ConfigurationManager.AppSettings[key];
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (defaultValue != null)
                        return defaultValue;
                    throw new ConfigurationErrorsException($"La configuración '{key}' no está definida en App.config.");
                }
                return value;
            }

            // ==================== RUTAS BASE ====================
            
            /// <summary>Ruta base para logs (ej: C:\Logs\ o \\servidor\logs\)</summary>
            public static string BaseLogs => GetAppSetting("RutaBaseLogs");

            /// <summary>Ruta base para archivos Excel (ej: C:\Excel\ o \\servidor\excel\)</summary>
            public static string BaseExcel => GetAppSetting("RutaBaseExcel");

            // ==================== RUTAS SANTANDER ====================
            
            /// <summary>Ruta base para archivos TXT de Santander</summary>
            public static string SantanderBaseTxt => GetAppSetting("RutaSantanderBaseTxt");

            /// <summary>Ruta para archivos Punto a Punto de Santander (sin ciudad, solo divisa)</summary>
            public static string SantanderPuntoAPunto => GetAppSetting("RutaSantanderPuntoAPunto");

            /// <summary>Ruta para archivos Tanda de Santander</summary>
            public static string SantanderTanda => GetAppSetting("RutaSantanderTanda");

            /// <summary>Ruta para archivos Día a Día de Santander</summary>
            public static string SantanderDiaADia => GetAppSetting("RutaSantanderDiaADia");

            /// <summary>Ruta para CashOffice de Santander (P2P)</summary>
            public static string SantanderCashOfficeP2P => GetAppSetting("RutaSantanderCashOfficeP2P");

            // ==================== RUTAS SCOTIABANK ====================
            
            /// <summary>Ruta base para archivos TXT de Scotiabank</summary>
            public static string ScotiabankBaseTxt => GetAppSetting("RutaScotiabankBaseTxt");

            /// <summary>Ruta para archivos de Montevideo (Scotiabank)</summary>
            public static string ScotiabankMontevideo => GetAppSetting("RutaScotiabankMontevideo");

            /// <summary>Ruta para archivos de Maldonado (Scotiabank)</summary>
            public static string ScotiabankMaldonado => GetAppSetting("RutaScotiabankMaldonado");

            /// <summary>Ruta para CashOffice de Scotiabank</summary>
            public static string ScotiabankCashOffice => GetAppSetting("RutaScotiabankCashOffice");
        }
    }
}
