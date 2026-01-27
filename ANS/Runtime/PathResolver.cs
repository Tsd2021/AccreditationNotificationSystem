using ANS.Model.Services;
using System;
using System.IO;

namespace ANS.Runtime
{
    /// <summary>
    /// Resuelve rutas manteniendo coherencia entre PROD y TEST
    /// En TEST replica la estructura de PROD bajo TestRoot
    /// </summary>
    public static class PathResolver
    {
        private static string _effectiveTestRoot;
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// TestRoot efectivo (del JSON o default)
        /// </summary>
        public static string EffectiveTestRoot
        {
            get
            {
                EnsureInitialized();
                return _effectiveTestRoot;
            }
        }

        /// <summary>
        /// Inicializa el resolver
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            lock (_lock)
            {
                if (_initialized)
                    return;

                _effectiveTestRoot = LocalConfigLoader.GetEffectiveTestRoot();
                Directory.CreateDirectory(_effectiveTestRoot);
                _initialized = true;
            }
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Resuelve una ruta de producción a su equivalente en TEST
        /// Replica la estructura relativa bajo TestRoot
        /// </summary>
        /// <param name="prodPath">Ruta de producción (puede ser UNC, absoluta, etc.)</param>
        /// <param name="defaultSubPath">Subcarpeta por defecto si no se puede extraer estructura (ej: "Bancos", "Excel", "Logs")</param>
        /// <returns>Ruta equivalente en TEST bajo TestRoot</returns>
        public static string ResolveTestPath(string prodPath, string defaultSubPath = null)
        {
            EnsureInitialized();

            if (AppRuntime.IsProduction)
            {
                // PROD: devolver la ruta original
                return prodPath;
            }

            // TEST: replicar estructura bajo TestRoot
            string relativePath = ExtractRelativeStructure(prodPath);

            // Si no se pudo extraer estructura, usar defaultSubPath
            if (string.IsNullOrWhiteSpace(relativePath) && !string.IsNullOrWhiteSpace(defaultSubPath))
            {
                relativePath = defaultSubPath;
            }

            // Si aún no hay estructura, usar el nombre del archivo o carpeta base
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                // Intentar extraer el último componente significativo
                var fileName = Path.GetFileName(prodPath);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    relativePath = fileName;
                }
                else
                {
                    // Fallback: usar "Output"
                    relativePath = "Output";
                }
            }

            // Construir ruta completa bajo TestRoot
            var testPath = Path.Combine(_effectiveTestRoot, relativePath);
            
            // Normalizar (eliminar .., etc.)
            testPath = Path.GetFullPath(testPath);

            // Asegurar que esté bajo TestRoot (seguridad)
            var testRootFull = Path.GetFullPath(_effectiveTestRoot);
            if (!testPath.StartsWith(testRootFull, StringComparison.OrdinalIgnoreCase))
            {
                // Si por alguna razón salió del root, usar solo el nombre del archivo
                var safeFileName = Path.GetFileName(prodPath) ?? "output";
                testPath = Path.Combine(testRootFull, safeFileName);
                testPath = Path.GetFullPath(testPath);
            }

            // Crear directorio si no existe
            var directory = Path.GetDirectoryName(testPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return testPath;
        }

        /// <summary>
        /// Extrae la estructura relativa de una ruta de producción
        /// Ejemplo: "\\server\bbva\SALIDA\archivo.txt" => "bbva\SALIDA"
        /// </summary>
        private static string ExtractRelativeStructure(string prodPath)
        {
            if (string.IsNullOrWhiteSpace(prodPath))
                return null;

            try
            {
                // Normalizar la ruta
                var normalized = prodPath.Replace('/', '\\').Trim('\\');

                // Si es UNC (\\server\share\...), extraer desde el share
                if (normalized.StartsWith(@"\\"))
                {
                    var parts = normalized.Substring(2).Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        // Omitir el primer componente (servidor) y tomar el resto
                        return string.Join(Path.DirectorySeparatorChar.ToString(), parts, 1, parts.Length - 1);
                    }
                    return parts.Length > 0 ? parts[0] : null;
                }

                // Si es ruta absoluta (C:\...), extraer desde la unidad
                if (normalized.Length > 2 && normalized[1] == ':')
                {
                    var parts = normalized.Substring(3).Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
                    }
                }

                // Si es relativa, usar tal cual
                return normalized;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Resuelve ruta de banco manteniendo estructura
        /// </summary>
        public static string ResolveBankPath(string prodBankPath, string bankName)
        {
            EnsureInitialized();

            if (AppRuntime.IsProduction)
                return prodBankPath;

            // En TEST, replicar estructura bajo TestRoot/Bancos/{bankName}
            var relative = ExtractRelativeStructure(prodBankPath);
            var testPath = string.IsNullOrWhiteSpace(relative)
                ? Path.Combine(_effectiveTestRoot, "Bancos", bankName)
                : Path.Combine(_effectiveTestRoot, "Bancos", bankName, relative);

            testPath = Path.GetFullPath(testPath);
            Directory.CreateDirectory(Path.GetDirectoryName(testPath));
            return testPath;
        }

        /// <summary>
        /// Resuelve ruta de Excel
        /// </summary>
        public static string ResolveExcelPath(string prodExcelPath)
        {
            return ResolveTestPath(prodExcelPath, "Excel");
        }

        /// <summary>
        /// Resuelve ruta de Logs
        /// </summary>
        public static string ResolveLogsPath(string prodLogsPath)
        {
            return ResolveTestPath(prodLogsPath, "Logs");
        }

        /// <summary>
        /// Resuelve ruta de SQLite
        /// </summary>
        public static string ResolveSqlitePath(string prodSqlitePath)
        {
            EnsureInitialized();

            if (AppRuntime.IsProduction)
                return prodSqlitePath;

            // En TEST, SQLite va a TestRoot/sqlite/
            var fileName = Path.GetFileName(prodSqlitePath) ?? "ans_test.db";
            var testPath = Path.Combine(_effectiveTestRoot, "sqlite", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(testPath));
            return testPath;
        }
    }
}
