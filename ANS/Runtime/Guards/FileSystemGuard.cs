using ANS.Model.Services;
using System;
using System.IO;

namespace ANS.Runtime.Guards
{
    /// <summary>
    /// Guardia para operaciones de sistema de archivos
    /// Bloquea escrituras en rutas de producción cuando está en modo TEST
    /// </summary>
    public static class FileSystemGuard
    {
        /// <summary>
        /// Valida que la ruta de escritura esté permitida según el modo actual
        /// En modo TEST: solo permite rutas bajo whitelist estricta (%LocalAppData%\ANS\Test\)
        /// </summary>
        /// <param name="filePath">Ruta completa del archivo a escribir</param>
        /// <param name="context">Contexto de la operación (para logging)</param>
        /// <exception cref="InvalidOperationException">Si está en TEST y la ruta no está en whitelist</exception>
        public static void EnsureWriteAllowed(string filePath, string context = "FileSystem")
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            if (AppRuntime.IsTest)
            {
                // ✅ WHITELIST ESTRICTA: Solo permitir rutas bajo EffectiveTestRoot
                PathResolver.Initialize();
                var effectiveTestRoot = Path.GetFullPath(PathResolver.EffectiveTestRoot);
                var fullFilePath = Path.GetFullPath(filePath);

                // Verificar que la ruta esté dentro de la whitelist
                if (!fullFilePath.StartsWith(effectiveTestRoot, StringComparison.OrdinalIgnoreCase))
                {
                    var msg = $"BLOQUEO EN TEST: Intento de escribir fuera de whitelist permitida. " +
                             $"Ruta intentada: {filePath} | " +
                             $"Ruta normalizada: {fullFilePath} | " +
                             $"Whitelist permitida (EffectiveTestRoot): {effectiveTestRoot} | " +
                             $"Contexto: {context}";
                    ServicioLog.instancia.WriteError(msg, "FileSystemGuard | EnsureWriteAllowed");
                    throw new InvalidOperationException(msg);
                }

                // Bloquear explícitamente rutas UNC (aunque ya están fuera de whitelist, doble verificación)
                if (filePath.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
                {
                    var msg = $"BLOQUEO EN TEST: Intento de escribir en ruta UNC (share de red): {filePath} | Contexto: {context}";
                    ServicioLog.instancia.WriteError(msg, "FileSystemGuard | EnsureWriteAllowed");
                    throw new InvalidOperationException(msg);
                }
            }
        }

        /// <summary>
        /// Obtiene el nombre de archivo con prefijo TEST_ si está en modo TEST
        /// </summary>
        public static string GetFileNameWithTestPrefix(string originalFileName)
        {
            if (AppRuntime.IsTest && !originalFileName.StartsWith("TEST_", StringComparison.OrdinalIgnoreCase))
            {
                return $"TEST_{originalFileName}";
            }
            return originalFileName;
        }
    }
}
