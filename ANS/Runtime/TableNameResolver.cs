using ANS.Model.Services;
using System;

namespace ANS.Runtime
{
    /// <summary>
    /// Resuelve nombres de tablas según RuntimeMode
    /// En TEST usa tablas réplica, en PRODUCTION usa tablas de producción
    /// </summary>
    public static class TableNameResolver
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Nombre de la tabla de acreditaciones según el modo
        /// PRODUCTION: "AcreditacionDepositoDiegoTest"
        /// TEST: "AcreditacionDepositoDiegoTest_Replica"
        /// </summary>
        public static string AcreditacionDeposito
        {
            get
            {
                EnsureInitialized();
                return AppRuntime.IsTest 
                    ? "AcreditacionDepositoDiegoTest_Replica"
                    : "AcreditacionDepositoDiegoTest";
            }
        }

        /// <summary>
        /// Valida que el nombre de tabla usado sea el correcto según el modo
        /// Guardia anti-accidente: si en TEST se intenta usar tabla de PROD, aborta
        /// </summary>
        /// <param name="tableNameUsed">Nombre de tabla que se está intentando usar (debe venir de TableNameResolver)</param>
        /// <param name="context">Contexto de la operación (para logging)</param>
        /// <exception cref="InvalidOperationException">Si en TEST se intenta usar tabla de PROD</exception>
        public static void ValidateTableName(string tableNameUsed, string context = "Operación SQL")
        {
            EnsureInitialized();

            if (AppRuntime.IsTest)
            {
                // En TEST, bloquear uso de tabla de PRODUCCIÓN
                if (tableNameUsed.Equals("AcreditacionDepositoDiegoTest", StringComparison.OrdinalIgnoreCase))
                {
                    var msg = $"ERROR CRÍTICO DE SEGURIDAD: En modo TEST se intentó usar la tabla de PRODUCCIÓN 'AcreditacionDepositoDiegoTest'. " +
                             $"En TEST debe usarse 'AcreditacionDepositoDiegoTest_Replica'. " +
                             $"Contexto: {context}. " +
                             $"Use TableNameResolver.AcreditacionDeposito en lugar de hardcodear el nombre.";
                    ServicioLog.instancia.WriteError(msg, "TableNameResolver | ValidateTableName");
                    throw new InvalidOperationException(msg);
                }
            }
            else
            {
                // En PRODUCTION, validar que se use la tabla correcta (opcional, pero útil para detectar bugs)
                if (tableNameUsed.Equals("AcreditacionDepositoDiegoTest_Replica", StringComparison.OrdinalIgnoreCase) ||
                    tableNameUsed.Equals("AcreditacionDepositoDiegoTest_replica", StringComparison.OrdinalIgnoreCase))
                {
                    ServicioLog.instancia.WriteWarning(
                        $"ADVERTENCIA: En modo PRODUCTION se está usando tabla de TEST 'AcreditacionDepositoDiegoTest_Replica'. " +
                        $"Contexto: {context}. " +
                        $"Verificar que se use TableNameResolver.AcreditacionDeposito.",
                        "TableNameResolver | ValidateTableName");
                }
            }
        }

        /// <summary>
        /// Asegura que TableNameResolver esté inicializado
        /// Puede ser llamado externamente para forzar inicialización temprana
        /// NOTA: No llama a AppRuntime.Initialize() para evitar dependencia circular.
        /// AppRuntime.Initialize() debe llamarse primero desde App.xaml.cs
        /// </summary>
        public static void EnsureInitialized()
        {
            if (!_initialized)
            {
                lock (_lock)
                {
                    if (!_initialized)
                    {
                        // Solo verificar que AppRuntime esté inicializado, no inicializarlo aquí
                        // para evitar dependencia circular
                        if (!AppRuntime.IsInitialized)
                        {
                            // Si AppRuntime no está inicializado, intentar inicializarlo
                            // pero solo si no hay riesgo de dependencia circular
                            try
                            {
                                AppRuntime.Initialize();
                            }
                            catch
                            {
                                // Si falla, continuar de todas formas (se inicializará lazy)
                            }
                        }
                        
                        var tableName = AppRuntime.IsTest 
                            ? "AcreditacionDepositoDiegoTest_Replica"
                            : "AcreditacionDepositoDiegoTest";
                        
                        ServicioLog.instancia.WriteInfo(
                            $"TableNameResolver inicializado | Modo: {AppRuntime.Mode} | " +
                            $"Tabla de acreditaciones: {tableName}",
                            "TableNameResolver | EnsureInitialized");
                        
                        _initialized = true;
                    }
                }
            }
        }
    }
}
