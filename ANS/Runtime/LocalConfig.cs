using System;
using System.IO;
using System.Configuration;

namespace ANS.Runtime
{
    /// <summary>
    /// Cargador de configuración de TestBasePath
    /// Lee desde App.config (key "TestBasePath") o usa default
    /// IMPORTANTE: NO usa ServicioLog aquí para evitar dependencias circulares durante inicialización
    /// </summary>
    public static class LocalConfigLoader
    {
        private const string DefaultTestRoot = @"C:\Users\dchiquiar.ABUDIL\Desktop\TAAS TEST";
        private const string ConfigKey = "TestBasePath";
        private static bool _loggingEnabled = false;

        /// <summary>
        /// Habilita logging (solo después de que ServicioLog esté inicializado)
        /// </summary>
        internal static void EnableLogging()
        {
            _loggingEnabled = true;
        }

        /// <summary>
        /// Obtiene el TestBasePath efectivo (de App.config o default)
        /// NO usa ServicioLog durante inicialización para evitar StackOverflow
        /// </summary>
        public static string GetEffectiveTestRoot()
        {
            try
            {
                // ✅ Acceso directo a ConfigurationManager sin dependencias
                var testBasePath = ConfigurationManager.AppSettings[ConfigKey];
                if (!string.IsNullOrWhiteSpace(testBasePath))
                {
                    var testRoot = Path.GetFullPath(testBasePath);
                    
                    // Solo loggear si ServicioLog ya está disponible (evita recursión)
                    if (_loggingEnabled)
                    {
                        try
                        {
                            ANS.Model.Services.ServicioLog.instancia.WriteInfo(
                                $"TestBasePath desde App.config | Ruta: {testRoot}",
                                "LocalConfigLoader | GetEffectiveTestRoot");
                        }
                        catch
                        {
                            // Ignorar errores de logging durante inicialización
                        }
                    }
                    
                    return testRoot;
                }
            }
            catch (Exception ex)
            {
                // Solo loggear si ServicioLog ya está disponible
                if (_loggingEnabled)
                {
                    try
                    {
                        ANS.Model.Services.ServicioLog.instancia.WriteWarning(
                            $"Error al leer TestBasePath de App.config: {ex.Message}. Usando default.",
                            "LocalConfigLoader | GetEffectiveTestRoot");
                    }
                    catch
                    {
                        // Ignorar errores de logging durante inicialización
                    }
                }
            }

            // Default
            var defaultRoot = Path.GetFullPath(DefaultTestRoot);
            
            // Solo loggear si ServicioLog ya está disponible
            if (_loggingEnabled)
            {
                try
                {
                    ANS.Model.Services.ServicioLog.instancia.WriteInfo(
                        $"Usando TestBasePath por defecto: {defaultRoot}",
                        "LocalConfigLoader | GetEffectiveTestRoot");
                }
                catch
                {
                    // Ignorar errores de logging durante inicialización
                }
            }
            
            return defaultRoot;
        }
    }
}

