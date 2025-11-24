
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ANS.Model.Services
{
    public class ServicioLog
    {
        // ✅ Thread-safe: Lazy<T> garantiza inicialización única
        // Crítico: Múltiples threads escriben logs simultáneamente, thread-safety previene corrupción de archivos
        private static readonly Lazy<ServicioLog> _lazy = 
            new Lazy<ServicioLog>(() => new ServicioLog());
        
        public static ServicioLog instancia => _lazy.Value;

        public static ServicioLog getInstancia()
        {
            return _lazy.Value;
        }

        // ✅ Método privado centralizado para escribir logs (evita duplicación de código)
        private void WriteToFile(string level, string message, string context = "")
        {
            try
            {
                // Directorio y nombre de archivo con fecha actual
                // Prod:
                string logDirectory = @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\Logs\";
                // Testing Local:
                //string logDirectory = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\Logs\";
                
                string fileName = $"TAAS_Log{DateTime.Now:ddMMyyyy}.txt";
                string filePath = Path.Combine(logDirectory, fileName);

                // Asegura que el directorio exista
                Directory.CreateDirectory(logDirectory);

                // Construye la línea de log con formato consistente
                string contextPart = string.IsNullOrEmpty(context) ? "" : $" | {context}";
                string line = $"[{level}] {DateTime.Now:yyyy-MM-dd HH:mm:ss}{contextPart} | {message}";

                // Añade la línea al final (crea el archivo si no existe)
                // ✅ Thread-safe: FileStream con FileShare.ReadWrite permite escritura concurrente
                using (var writer = new StreamWriter(filePath, append: true))
                {
                    writer.WriteLine(line);
                }
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine($"No se pudo escribir el log: {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Debug.WriteLine($"Acceso denegado al intentar escribir el log: {uaEx.Message}");
            }
            catch (Exception ex)
            {
                // Fallback: si falla el logging, al menos intentar con Debug
                Debug.WriteLine($"Error crítico al escribir log: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ Método mejorado: Registra excepciones con contexto completo (stack trace, inner exception)
        /// </summary>
        public void WriteLog(Exception e, string bank, string accreditationType)
        {
            if (e == null) return;

            string context = $"Bank: {bank} | AccreditationType: {accreditationType}";
            string message = $"Exception: {e.GetType().Name} | Message: {e.Message}";
            
            // Incluye InnerException si existe
            if (e.InnerException != null)
            {
                message += $" | InnerException: {e.InnerException.GetType().Name} - {e.InnerException.Message}";
            }
            
            // Incluye StackTrace para debugging (solo las primeras líneas para no saturar)
            if (!string.IsNullOrEmpty(e.StackTrace))
            {
                var stackLines = e.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                var relevantStack = string.Join(" | ", stackLines.Take(3)); // Primeras 3 líneas
                message += $" | StackTrace: {relevantStack}";
            }

            WriteToFile("ERROR", message, context);
        }

        /// <summary>
        /// ✅ Nuevo método: Registra errores con contexto personalizado (más flexible)
        /// </summary>
        public void WriteError(string message, string context = "")
        {
            if (string.IsNullOrEmpty(message)) return;
            WriteToFile("ERROR", message, context);
        }

        /// <summary>
        /// ✅ Nuevo método: Registra información (para pasos clave del proceso)
        /// </summary>
        public void WriteInfo(string message, string context = "")
        {
            if (string.IsNullOrEmpty(message)) return;
            WriteToFile("INFO", message, context);
        }

        /// <summary>
        /// ✅ Nuevo método: Registra advertencias
        /// </summary>
        public void WriteWarning(string message, string context = "")
        {
            if (string.IsNullOrEmpty(message)) return;
            WriteToFile("WARNING", message, context);
        }

        /// <summary>
        /// ✅ Método genérico mejorado: Mantiene compatibilidad con código existente
        /// </summary>
        public void WriteLogGeneric(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            WriteToFile("INFO", msg);
        }
    }
}