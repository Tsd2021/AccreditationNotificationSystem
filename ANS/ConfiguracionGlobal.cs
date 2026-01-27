using System.Configuration;
using ANS.Runtime;
using System.IO;

namespace ANS
{
    public static class ConfiguracionGlobal
    {
        /// <summary>
        /// Obtiene la connection string según el modo actual (TEST usa réplica, PROD usa producción)
        /// </summary>
        public static string ConexionTSD
        {
            get
            {
                AppRuntime.Initialize(); // Asegurar inicialización
                return AppRuntime.IsTest 
                    ? AppRuntime.Settings.ConnectionStrings.SqlServerTest
                    : AppRuntime.Settings.ConnectionStrings.SqlServerProd;
            }
        }

        /// <summary>
        /// Obtiene la connection string según el modo actual
        /// </summary>
        public static string ConexionEncuesta
        {
            get
            {
                AppRuntime.Initialize();
                return AppRuntime.IsTest 
                    ? AppRuntime.Settings.ConnectionStrings.EncuestaTest
                    : AppRuntime.Settings.ConnectionStrings.EncuestaProd;
            }
        }

        /// <summary>
        /// Obtiene la connection string según el modo actual
        /// </summary>
        public static string ConexionWebBuzones
        {
            get
            {
                AppRuntime.Initialize();
                return AppRuntime.IsTest 
                    ? AppRuntime.Settings.ConnectionStrings.WebBuzonesTest
                    : AppRuntime.Settings.ConnectionStrings.WebBuzonesProd;
            }
        }

        /// <summary>
        /// Obtiene la connection string según el modo actual
        /// </summary>
        public static string Conexion22
        {
            get
            {
                AppRuntime.Initialize();
                return AppRuntime.IsTest 
                    ? AppRuntime.Settings.ConnectionStrings.SqlServer22Test
                    : AppRuntime.Settings.ConnectionStrings.SqlServer22Prod;
            }
        }

        /// <summary>
        /// ✅ Clase centralizada para todas las rutas del sistema
        /// Resuelve rutas según RuntimeMode (TEST usa carpetas locales, PROD usa shares/red)
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

            private static string GetPathFromSettings(string key, string defaultValue = null)
            {
                AppRuntime.Initialize();
                if (AppRuntime.IsTest)
                {
                    // En TEST, obtener desde Settings (carpetas locales)
                    return AppRuntime.Settings.Paths.BankOutputRootTest;
                }
                else
                {
                    // En PROD, obtener desde App.config (shares/red)
                    return GetAppSetting(key, defaultValue);
                }
            }

            // ==================== RUTAS BASE ====================
            
            /// <summary>Ruta base para logs según modo (resuelve usando PathResolver)</summary>
            public static string BaseLogs
            {
                get
                {
                    AppRuntime.Initialize();
                    PathResolver.Initialize();
                    var prodPath = GetAppSetting("RutaBaseLogs");
                    return AppRuntime.IsTest 
                        ? PathResolver.ResolveLogsPath(prodPath)
                        : prodPath;
                }
            }

            /// <summary>Ruta base para archivos Excel según modo (resuelve usando PathResolver)</summary>
            public static string BaseExcel
            {
                get
                {
                    AppRuntime.Initialize();
                    PathResolver.Initialize();
                    var prodPath = GetAppSetting("RutaBaseExcel");
                    return AppRuntime.IsTest 
                        ? PathResolver.ResolveExcelPath(prodPath)
                        : prodPath;
                }
            }

            // ==================== RUTAS SANTANDER ====================
            
            /// <summary>Ruta base para archivos TXT de Santander según modo</summary>
            public static string SantanderBaseTxt => GetPathFromSettings("RutaSantanderBaseTxt");

            /// <summary>Ruta para archivos Punto a Punto de Santander según modo (resuelve usando PathResolver)</summary>
            public static string SantanderPuntoAPunto
            {
                get
                {
                    AppRuntime.Initialize();
                    PathResolver.Initialize();
                    var prodPath = GetAppSetting("RutaSantanderPuntoAPunto");
                    return AppRuntime.IsTest
                        ? PathResolver.ResolveBankPath(prodPath, "Santander")
                        : prodPath;
                }
            }

            /// <summary>Ruta para archivos Tanda de Santander según modo (resuelve usando PathResolver)</summary>
            public static string SantanderTanda
            {
                get
                {
                    AppRuntime.Initialize();
                    PathResolver.Initialize();
                    var prodPath = GetAppSetting("RutaSantanderTanda");
                    return AppRuntime.IsTest
                        ? PathResolver.ResolveBankPath(prodPath, "Santander")
                        : prodPath;
                }
            }

            /// <summary>Ruta para archivos Día a Día de Santander según modo (resuelve usando PathResolver)</summary>
            public static string SantanderDiaADia
            {
                get
                {
                    AppRuntime.Initialize();
                    PathResolver.Initialize();
                    var prodPath = GetAppSetting("RutaSantanderDiaADia");
                    return AppRuntime.IsTest
                        ? PathResolver.ResolveBankPath(prodPath, "Santander")
                        : prodPath;
                }
            }

            /// <summary>Ruta para CashOffice de Santander (P2P) según modo (resuelve usando PathResolver)</summary>
            public static string SantanderCashOfficeP2P
            {
                get
                {
                    AppRuntime.Initialize();
                    PathResolver.Initialize();
                    var prodPath = GetAppSetting("RutaSantanderCashOfficeP2P");
                    return AppRuntime.IsTest
                        ? PathResolver.ResolveBankPath(prodPath, "Santander")
                        : prodPath;
                }
            }

            // ==================== RUTAS SCOTIABANK ====================
            
            /// <summary>Ruta base para archivos TXT de Scotiabank según modo</summary>
            public static string ScotiabankBaseTxt => GetPathFromSettings("RutaScotiabankBaseTxt");

            /// <summary>Ruta para archivos de Montevideo (Scotiabank) según modo (resuelve usando PathResolver)</summary>
            public static string ScotiabankMontevideo
            {
                get
                {
                    AppRuntime.Initialize();
                    PathResolver.Initialize();
                    var prodPath = GetAppSetting("RutaScotiabankMontevideo");
                    return AppRuntime.IsTest
                        ? PathResolver.ResolveBankPath(prodPath, "Scotiabank")
                        : prodPath;
                }
            }

            /// <summary>Ruta para archivos de Maldonado (Scotiabank) según modo (resuelve usando PathResolver)</summary>
            public static string ScotiabankMaldonado
            {
                get
                {
                    AppRuntime.Initialize();
                    PathResolver.Initialize();
                    var prodPath = GetAppSetting("RutaScotiabankMaldonado");
                    return AppRuntime.IsTest
                        ? PathResolver.ResolveBankPath(prodPath, "Scotiabank")
                        : prodPath;
                }
            }

            /// <summary>Ruta para CashOffice de Scotiabank según modo (resuelve usando PathResolver)</summary>
            public static string ScotiabankCashOffice
            {
                get
                {
                    AppRuntime.Initialize();
                    PathResolver.Initialize();
                    var prodPath = GetAppSetting("RutaScotiabankCashOffice");
                    return AppRuntime.IsTest
                        ? PathResolver.ResolveBankPath(prodPath, "Scotiabank")
                        : prodPath;
                }
            }
            
            /// <summary>Ruta para archivos de combinación de Scotiabank según modo (resuelve usando PathResolver)</summary>
            public static string ScotiabankCombinacion
            {
                get
                {
                    AppRuntime.Initialize();
                    PathResolver.Initialize();
                    var prodPath = GetAppSetting("RutaScotiabankCombinacion");
                    return AppRuntime.IsTest
                        ? PathResolver.ResolveBankPath(prodPath, "Scotiabank")
                        : prodPath;
                }
            }
        }
    }
}
