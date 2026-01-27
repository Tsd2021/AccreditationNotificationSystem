using ANS.Model.Services;
using System;
using System.IO;
using System.Linq;
using ANS.Runtime.Guards;
using ANS.Model;
using ANS.Runtime;

namespace ANS.Runtime
{
    /// <summary>
    /// Runtime centralizado de la aplicación: modo de ejecución y configuración
    /// </summary>
    public static class AppRuntime
    {
        private static RuntimeMode _mode = RuntimeMode.Test; // Default seguro: TEST
        private static AppSettings _settings;
        private static bool _initialized = false;
        private static bool _initializing = false; // Guardar reentrancias para evitar StackOverflow
        private static readonly object _lock = new object();

        /// <summary>
        /// Indica si AppRuntime ya fue inicializado
        /// </summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// Modo de ejecución actual
        /// </summary>
        public static RuntimeMode Mode
        {
            get
            {
                EnsureInitialized();
                return _mode;
            }
            private set => _mode = value;
        }

        /// <summary>
        /// True si está en modo TEST
        /// </summary>
        public static bool IsTest
        {
            get
            {
                EnsureInitialized();
                return _mode == RuntimeMode.Test;
            }
        }

        /// <summary>
        /// True si está en modo PRODUCTION
        /// </summary>
        public static bool IsProduction
        {
            get
            {
                EnsureInitialized();
                return _mode == RuntimeMode.Production;
            }
        }

        /// <summary>
        /// Configuración cargada según el modo actual
        /// </summary>
        public static AppSettings Settings
        {
            get
            {
                EnsureInitialized();
                return _settings;
            }
            private set => _settings = value;
        }

        /// <summary>
        /// Inicializa el runtime leyendo la configuración
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            lock (_lock)
            {
                if (_initialized)
                    return;

                if (_initializing)
                    throw new InvalidOperationException("Reentrada detectada en AppRuntime.Initialize. Revise dependencias circulares.");

                _initializing = true;

                try
                {
                    // 1) PathResolver primero (pero idealmente PathResolver NO debe depender de AppRuntime)
                    PathResolver.Initialize();

                    // 2) Determinar modo
                    _mode = DetermineRuntimeMode();
                    var isTest = (_mode == RuntimeMode.Test);

                    // 3) Cargar settings
                    _settings = LoadSettings(_mode);

                    // 4) IMPORTANTÍSIMO: marcar inicializado AHORA.
                    // A partir de aquí, si cualquier cosa consulta AppRuntime, NO reentra.
                    _initialized = true;

                    // 5) Ahora sí: inicializadores que podrían consultar AppRuntime
                    TableNameResolver.EnsureInitialized();
                    LocalConfigLoader.EnableLogging();

                    // 6) Crear carpetas usando isTest y _settings (NO IsTest)
                    try
                    {
                        if (isTest)
                        {
                            if (!string.IsNullOrWhiteSpace(_settings.Sqlite.BaseFolderTest))
                                Directory.CreateDirectory(_settings.Sqlite.BaseFolderTest);
                            if (!string.IsNullOrWhiteSpace(_settings.Paths.BankOutputRootTest))
                                Directory.CreateDirectory(_settings.Paths.BankOutputRootTest);
                            if (!string.IsNullOrWhiteSpace(_settings.Paths.ExcelOutputRootTest))
                                Directory.CreateDirectory(_settings.Paths.ExcelOutputRootTest);
                            if (!string.IsNullOrWhiteSpace(_settings.Paths.LogsRootTest))
                                Directory.CreateDirectory(_settings.Paths.LogsRootTest);
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(_settings.Sqlite.BaseFolderProd))
                                Directory.CreateDirectory(_settings.Sqlite.BaseFolderProd);
                            if (!string.IsNullOrWhiteSpace(_settings.Paths.BankOutputRootProd))
                                Directory.CreateDirectory(_settings.Paths.BankOutputRootProd);
                            if (!string.IsNullOrWhiteSpace(_settings.Paths.ExcelOutputRootProd))
                                Directory.CreateDirectory(_settings.Paths.ExcelOutputRootProd);
                            if (!string.IsNullOrWhiteSpace(_settings.Paths.LogsRootProd))
                                Directory.CreateDirectory(_settings.Paths.LogsRootProd);
                        }
                    }
                    catch
                    {
                        // No abortar init por carpetas
                    }

                    // 7) Logging inicial (ya es seguro; _initialized=true)
                    ServicioLog.instancia.WriteInfo("═══════════════════════════════════════════════════════════════", "AppRuntime | Initialize");
                    ServicioLog.instancia.WriteInfo(
                        $"RUNTIME MODE: {_mode} | IsTest: {isTest} | IsProduction: {!isTest}",
                        "AppRuntime | Initialize");

                    if (isTest)
                    {
                        var connTSD = _settings.ConnectionStrings.SqlServerTest;
                        var dbName = ExtractDatabaseName(connTSD);
                        ServicioLog.instancia.WriteInfo(
                            $"CONNECTION STRING TEST | Nombre: conexionTSDTest | Database: {dbName}",
                            "AppRuntime | Initialize");

                        var tablaEfectiva = TableNameResolver.AcreditacionDeposito;
                        ServicioLog.instancia.WriteInfo(
                            $"TABLA DE ACREDITACIONES EFECTIVA: {tablaEfectiva}",
                            "AppRuntime | Initialize");

                        ServicioLog.instancia.WriteInfo(
                            $"EMAIL TEST | TestAllowSmtp: {_settings.Email.TestAllowSmtp} | TestEmailWhitelist: {_settings.Email.TestEmailWhitelist}",
                            "AppRuntime | Initialize");

                        ServicioLog.instancia.WriteInfo(
                            $"EffectiveTestRoot: {PathResolver.EffectiveTestRoot}",
                            "AppRuntime | Initialize");
                    }
                    else
                    {
                        var connTSD = _settings.ConnectionStrings.SqlServerProd;
                        var dbName = ExtractDatabaseName(connTSD);
                        ServicioLog.instancia.WriteInfo(
                            $"CONNECTION STRING PRODUCTION | Nombre: conexionTSD | Database: {dbName}",
                            "AppRuntime | Initialize");

                        var tablaEfectiva = TableNameResolver.AcreditacionDeposito;
                        ServicioLog.instancia.WriteInfo(
                            $"TABLA DE ACREDITACIONES EFECTIVA: {tablaEfectiva}",
                            "AppRuntime | Initialize");
                    }

                    ServicioLog.instancia.WriteInfo("═══════════════════════════════════════════════════════════════", "AppRuntime | Initialize");
                }
                finally
                {
                    _initializing = false;
                }
            }
        }


        /// <summary>
        /// Determina el modo de ejecución desde archivo o configuración
        /// Sin heurísticas de servidor - solo configuración explícita
        /// IMPORTANTE: NO usa ServicioLog durante inicialización para evitar recursión
        /// </summary>
        private static RuntimeMode DetermineRuntimeMode()
        {
            RuntimeMode? modeFromConfig = null;

            // Prioridad 1: Archivo ans.mode en la carpeta del ejecutable
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var modeFile = Path.Combine(exeDir, "ans.mode");
            if (File.Exists(modeFile))
            {
                try
                {
                    var content = File.ReadAllText(modeFile).Trim().ToUpperInvariant();
                    if (content == "PRODUCTION" || content == "PROD")
                        modeFromConfig = RuntimeMode.Production;
                    else if (content == "TEST")
                        modeFromConfig = RuntimeMode.Test;
                }
                catch (Exception ex)
                {
                    var msg = $"ERROR CRÍTICO: No se pudo leer ans.mode: {ex.Message}. La aplicación no puede continuar.";
                    // NO usar ServicioLog aquí - se loggeará después de la inicialización
                    throw new InvalidOperationException(msg, ex);
                }
            }

            // Prioridad 2: App.config AppSettings["RuntimeMode"]
            if (!modeFromConfig.HasValue)
            {
                try
                {
                    var configMode = System.Configuration.ConfigurationManager.AppSettings["RuntimeMode"];
                    if (!string.IsNullOrWhiteSpace(configMode))
                    {
                        if (configMode.Equals("Production", StringComparison.OrdinalIgnoreCase) ||
                            configMode.Equals("Prod", StringComparison.OrdinalIgnoreCase))
                            modeFromConfig = RuntimeMode.Production;
                        else if (configMode.Equals("Test", StringComparison.OrdinalIgnoreCase))
                            modeFromConfig = RuntimeMode.Test;
                    }
                }
                catch (StackOverflowException)
                {
                    // Si hay StackOverflow, usar default TEST
                    modeFromConfig = RuntimeMode.Test;
                }
                catch (Exception)
                {
                    // Cualquier otro error, ignorar y usar default
                    // NO usar ServicioLog aquí - se loggeará después de la inicialización
                }
            }

            // Si no hay configuración explícita, usar TEST (modo seguro)
            if (!modeFromConfig.HasValue)
            {
                // NO usar ServicioLog aquí - se loggeará después de la inicialización
                return RuntimeMode.Test;
            }

            return modeFromConfig.Value;
        }

        /// <summary>
        /// Carga la configuración según el modo
        /// </summary>
        private static AppSettings LoadSettings(RuntimeMode mode)
        {
            var settings = new AppSettings();

            if (mode == RuntimeMode.Production)
            {
                // PRODUCCIÓN: leer directamente desde ConfigurationManager para evitar recursión
                // NO usar ConfiguracionGlobal aquí porque causa StackOverflow (llama a AppRuntime.Initialize())
                try
                {
                    settings.ConnectionStrings.SqlServerProd = 
                        System.Configuration.ConfigurationManager.ConnectionStrings["conexionTSD"]?.ConnectionString ?? "";
                    settings.ConnectionStrings.SqlServer22Prod = 
                        System.Configuration.ConfigurationManager.ConnectionStrings["conexionTSD22"]?.ConnectionString ?? "";
                    settings.ConnectionStrings.EncuestaProd = 
                        System.Configuration.ConfigurationManager.ConnectionStrings["conexionENCUESTA"]?.ConnectionString ?? "";
                    settings.ConnectionStrings.WebBuzonesProd = 
                        System.Configuration.ConfigurationManager.ConnectionStrings["conexionWebBuzones"]?.ConnectionString ?? "";

                    settings.Sqlite.BaseFolderProd = AppDomain.CurrentDomain.BaseDirectory;
                    settings.Sqlite.FileNameProd = "QuartzRuns.db";

                    // Rutas desde App.config directamente
                    settings.Paths.BankOutputRootProd = 
                        System.Configuration.ConfigurationManager.AppSettings["RutaSantanderBaseTxt"] ?? "";
                    settings.Paths.ExcelOutputRootProd = 
                        System.Configuration.ConfigurationManager.AppSettings["RutaBaseExcel"] ?? "";
                    settings.Paths.LogsRootProd = 
                        System.Configuration.ConfigurationManager.AppSettings["RutaBaseLogs"] ?? "";

                    // Rutas específicas Santander
                    settings.Paths.SantanderPathsProd["PuntoAPunto"] = 
                        System.Configuration.ConfigurationManager.AppSettings["RutaSantanderPuntoAPunto"] ?? "";
                    settings.Paths.SantanderPathsProd["Tanda"] = 
                        System.Configuration.ConfigurationManager.AppSettings["RutaSantanderTanda"] ?? "";
                    settings.Paths.SantanderPathsProd["DiaADia"] = 
                        System.Configuration.ConfigurationManager.AppSettings["RutaSantanderDiaADia"] ?? "";
                    settings.Paths.SantanderPathsProd["CashOfficeP2P"] = 
                        System.Configuration.ConfigurationManager.AppSettings["RutaSantanderCashOfficeP2P"] ?? "";

                    // Rutas específicas Scotiabank
                    settings.Paths.ScotiabankPathsProd["Montevideo"] = 
                        System.Configuration.ConfigurationManager.AppSettings["RutaScotiabankMontevideo"] ?? "";
                    settings.Paths.ScotiabankPathsProd["Maldonado"] = 
                        System.Configuration.ConfigurationManager.AppSettings["RutaScotiabankMaldonado"] ?? "";
                    settings.Paths.ScotiabankPathsProd["CashOffice"] = 
                        System.Configuration.ConfigurationManager.AppSettings["RutaScotiabankCashOffice"] ?? "";
                    settings.Paths.ScotiabankPathsProd["Combinacion"] = 
                        System.Configuration.ConfigurationManager.AppSettings["RutaScotiabankCombinacion"] ?? "";
                }
                catch (StackOverflowException)
                {
                    // Si hay StackOverflow, usar valores por defecto o vacíos
                    // Esto no debería pasar, pero es una protección adicional
                    throw new InvalidOperationException(
                        "StackOverflowException al leer configuración de PRODUCTION. " +
                        "Verificar que no haya dependencias circulares en ConfiguracionGlobal.");
                }

                settings.WebServices.SantanderEnabledInTest = false; // No aplica en prod
                settings.WebServices.SantanderEndpointProd = VariablesGlobales.endPointTens;
                settings.WebServices.SantanderUsername = "urprmaetecnisegur";
                settings.WebServices.SantanderPassword = "9Nw$d9aQ";
            }
            else
            {
                // TEST: usar valores locales/aislados
                // TestRoot ya está inicializado en PathResolver

                // ✅ Connection strings de TEST: OBLIGATORIAS, sin fallback a PROD
                // Si falta alguna, la aplicación debe fallar al iniciar
                var missingConnections = new List<string>();

                settings.ConnectionStrings.SqlServerTest = 
                    System.Configuration.ConfigurationManager.ConnectionStrings["conexionTSDTest"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(settings.ConnectionStrings.SqlServerTest))
                    missingConnections.Add("conexionTSDTest");
                
                settings.ConnectionStrings.SqlServer22Test = 
                    System.Configuration.ConfigurationManager.ConnectionStrings["conexionTSD22Test"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(settings.ConnectionStrings.SqlServer22Test))
                    missingConnections.Add("conexionTSD22Test");
                
                settings.ConnectionStrings.EncuestaTest = 
                    System.Configuration.ConfigurationManager.ConnectionStrings["conexionENCUESTATest"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(settings.ConnectionStrings.EncuestaTest))
                    missingConnections.Add("conexionENCUESTATest");
                
                settings.ConnectionStrings.WebBuzonesTest = 
                    System.Configuration.ConfigurationManager.ConnectionStrings["conexionWebBuzonesTest"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(settings.ConnectionStrings.WebBuzonesTest))
                    missingConnections.Add("conexionWebBuzonesTest");

                // ✅ ERROR CRÍTICO: Si falta alguna connection string de TEST, fallar
                // NO usar ServicioLog aquí para evitar recursión durante inicialización
                if (missingConnections.Any())
                {
                    var msg = $"ERROR CRÍTICO: Faltan connection strings de TEST en App.config. " +
                             $"En modo TEST, todas las connection strings de TEST son OBLIGATORIAS. " +
                             $"Faltantes: {string.Join(", ", missingConnections)}. " +
                             $"La aplicación no puede continuar en modo TEST sin estas configuraciones.";
                    // NO usar ServicioLog aquí - se loggeará después de la inicialización
                    throw new InvalidOperationException(msg);
                }

                // Rutas resueltas usando PathResolver (replica estructura de PROD)
                var testRoot = PathResolver.EffectiveTestRoot;

                // SQLite
                settings.Sqlite.BaseFolderTest = Path.Combine(testRoot, "sqlite");
                // ✅ NO crear carpetas aquí - se crearán después de LoadSettings en Initialize() para evitar side-effects durante init
                settings.Sqlite.FileNameTest = "ans_test.db";

                // Rutas base (se resolverán dinámicamente usando PathResolver)
                settings.Paths.BankOutputRootTest = Path.Combine(testRoot, "Bancos");
                settings.Paths.ExcelOutputRootTest = Path.Combine(testRoot, "Excel");
                settings.Paths.LogsRootTest = Path.Combine(testRoot, "Logs");

                // WebServices: NUNCA habilitado en TEST
                settings.WebServices.SantanderEnabledInTest = false;

                // ✅ Email: Leer configuración de SMTP en TEST desde App.config
                // ✅ Usar try-catch para evitar StackOverflow si ConfigurationManager causa recursión
                try
                {
                    var testAllowSmtpStr = System.Configuration.ConfigurationManager.AppSettings["TestAllowSmtp"];
                    if (bool.TryParse(testAllowSmtpStr, out bool testAllowSmtp))
                    {
                        settings.Email.TestAllowSmtp = testAllowSmtp;
                    }
                    else
                    {
                        settings.Email.TestAllowSmtp = false; // Default seguro
                    }
                }
                catch (StackOverflowException)
                {
                    // Si hay StackOverflow, usar default seguro
                    settings.Email.TestAllowSmtp = false;
                }
                catch
                {
                    // Cualquier otro error, usar default seguro
                    settings.Email.TestAllowSmtp = false;
                }

                try
                {
                    var testEmailWhitelist = System.Configuration.ConfigurationManager.AppSettings["TestEmailWhitelist"];
                    if (!string.IsNullOrWhiteSpace(testEmailWhitelist))
                    {
                        settings.Email.TestEmailWhitelist = testEmailWhitelist;
                    }
                    else
                    {
                        settings.Email.TestEmailWhitelist = "acreditaciones@tecnisegur.com.uy"; // Default
                    }
                }
                catch (StackOverflowException)
                {
                    // Si hay StackOverflow, usar default
                    settings.Email.TestEmailWhitelist = "acreditaciones@tecnisegur.com.uy";
                }
                catch
                {
                    // Cualquier otro error, usar default
                    settings.Email.TestEmailWhitelist = "acreditaciones@tecnisegur.com.uy";
                }
            }

            return settings;
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
                return;

            // Si alguien consulta durante el bootstrap, NO reentrar.
            if (_initializing)
                return;

            Initialize();
        }


        /// <summary>
        /// Extrae el nombre de la base de datos de una connection string
        /// </summary>
        private static string ExtractDatabaseName(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return "N/A";

            try
            {
                // Buscar "Database=" o "Initial Catalog="
                var dbIndex = connectionString.IndexOf("Database=", StringComparison.OrdinalIgnoreCase);
                if (dbIndex < 0)
                    dbIndex = connectionString.IndexOf("Initial Catalog=", StringComparison.OrdinalIgnoreCase);
                
                if (dbIndex >= 0)
                {
                    var start = dbIndex + (connectionString.IndexOf("Database=", StringComparison.OrdinalIgnoreCase) >= 0 ? 9 : 16);
                    var end = connectionString.IndexOf(';', start);
                    if (end < 0) end = connectionString.Length;
                    return connectionString.Substring(start, end - start).Trim();
                }
            }
            catch
            {
                // Ignorar errores de parsing
            }

            return "N/A";
        }

        /// <summary>
        /// Guardia: lanza excepción si se intenta ejecutar en modo no permitido
        /// </summary>
        public static void EnsureProductionAllowed(string actionName)
        {
            if (IsTest)
            {
                var msg = $"ACCION BLOQUEADA EN MODO TEST: {actionName}. Esta acción solo está permitida en modo PRODUCTION.";
                ServicioLog.instancia.WriteError(msg, "AppRuntime | EnsureProductionAllowed");
                throw new InvalidOperationException(msg);
            }
        }

        /// <summary>
        /// Guardia: lanza excepción si se intenta ejecutar en modo no permitido
        /// </summary>
        public static void EnsureTestAllowed(string actionName)
        {
            if (IsProduction)
            {
                var msg = $"ACCION BLOQUEADA EN MODO PRODUCTION: {actionName}. Esta acción solo está permitida en modo TEST.";
                ServicioLog.instancia.WriteError(msg, "AppRuntime | EnsureTestAllowed");
                throw new InvalidOperationException(msg);
            }
        }
    }
}
