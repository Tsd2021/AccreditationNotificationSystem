
using System.Diagnostics;
using System.IO;

namespace ANS.Model.Services
{
    public class ServicioLog
    {
        public static ServicioLog instancia { get; set; }

        public static ServicioLog getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioLog();
            }
            return instancia;
        }

        public void WriteLog(Exception e, string bank, string accreditationType)
        {
            if (e == null) return;

            // Construye la línea de log
            string line = $"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Bank: {bank} | AccreditationType: {accreditationType} | Exception: {e.Message}";

            // Directorio y nombre de archivo con fecha actual

            
        
        Prod:
            string logDirectory = @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\Logs\";

            //Testing Local:
            //string logDirectory = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\Logs\";
            string fileName = $"TAAS_Log{DateTime.Now:ddMMyyyy}.txt";
            string filePath = Path.Combine(logDirectory, fileName);

            try
            {
                // Asegura que el directorio exista
                Directory.CreateDirectory(logDirectory);

                // Añade la línea al final (crea el archivo si no existe)
                using (var writer = new StreamWriter(filePath, append: true))
                {
                    writer.WriteLine(line);
                }

            }
            catch (IOException ioEx)
            {
                // Si hay un error de IO al escribir el log, puedes manejarlo aquí
                Debug.WriteLine($"No se pudo escribir el log: {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                // Manejo de permisos denegados
                Debug.WriteLine($"Acceso denegado al intentar escribir el log: {uaEx.Message}");
            }
        }

        public void WriteLogGeneric(string  msg)
        {
            if (string.IsNullOrEmpty(msg)) return;

            // Construye la línea de log
            string line = $"[GenericLog] {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Msg: {msg}";

            // Directorio y nombre de archivo con fecha actual

            //Testing Prod:
            string logDirectory = @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\Logs\";

            //Testing Local:
            //string logDirectory = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\Logs\";
            string fileName = $"TAAS_Log{DateTime.Now:ddMMyyyy}.txt";
            string filePath = Path.Combine(logDirectory, fileName);

            try
            {
                // Asegura que el directorio exista
                Directory.CreateDirectory(logDirectory);

                // Añade la línea al final (crea el archivo si no existe)
                using (var writer = new StreamWriter(filePath, append: true))
                {
                    writer.WriteLine(line);
                }

            }
            catch (IOException ioEx)
            {
                // Si hay un error de IO al escribir el log, puedes manejarlo aquí
                Debug.WriteLine($"No se pudo escribir el log: {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                // Manejo de permisos denegados
                Debug.WriteLine($"Acceso denegado al intentar escribir el log: {uaEx.Message}");
            }
        }


        /// <summary>
        /// Log específico para respuestas del WS TenS (Santander).
        /// Guarda en subcarpeta "Logs Tens".
        /// </summary>
        public void WriteTensResponse(
            string correlationId,
            bool ok,
            string code,
            string description,
            string tipoAcreditacion,
            string ciudad,
            string divisa,
            string nombreArchivo,
            int bytes,
            string sha256,
            long duracionMs)
        {
            string ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            // Línea amigable + trazable (clave=valor, 1 sola línea)
            string line =
                $"[Santander][TensWS] ts={ts} correlationId={correlationId} " +
                $"resultado={(ok ? "OK" : "FAIL")} code={(code ?? "NULL")} " +
                $"desc=\"{(description ?? "NULL").Replace("\"", "'")}\" " +
                $"tipo={(tipoAcreditacion ?? "NULL")} ciudad={(ciudad ?? "NULL")} divisa={(divisa ?? "NULL")} " +
                $"archivo={(nombreArchivo ?? "NULL")} bytes={bytes} sha256={(sha256 ?? "NULL")} " +
                $"duracionMs={duracionMs}";

            // --- Directorio y archivo ---
            // Prod:
            string baseDir = @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\Logs\";

            // Local:
            // string baseDir = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\Logs\";

            string tensDir = Path.Combine(baseDir, "Logs Tens");
            string fileName = $"Santander_TenS_{DateTime.Now:ddMMyyyy}.txt";
            string filePath = Path.Combine(tensDir, fileName);

            try
            {
                Directory.CreateDirectory(tensDir);
                using (var writer = new StreamWriter(filePath, append: true))
                {
                    writer.WriteLine(line);
                }
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine($"No se pudo escribir el log TenS: {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Debug.WriteLine($"Acceso denegado al escribir log TenS: {uaEx.Message}");
            }
        }

        /// <summary>
        /// Pista/ayuda inmediata para fallas TenS (misma carpeta "Logs Tens").
        /// </summary>
        public void WriteTensHint(string correlationId, string hintText)
        {
            if (string.IsNullOrWhiteSpace(hintText)) return;

            string ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            string line = $"[Santander][TensWS][Hint] ts={ts} correlationId={correlationId} hint=\"{hintText.Replace("\"", "'")}\"";

            // Prod:
            string baseDir = @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\Logs\";
            // Local:
            // string baseDir = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\Logs\";

            string tensDir = Path.Combine(baseDir, "Logs Tens");
            string fileName = $"Santander_TenS_{DateTime.Now:ddMMyyyy}.txt";
            string filePath = Path.Combine(tensDir, fileName);

            try
            {
                Directory.CreateDirectory(tensDir);
                using (var writer = new StreamWriter(filePath, append: true))
                {
                    writer.WriteLine(line);
                }
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine($"No se pudo escribir el log TenS (hint): {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Debug.WriteLine($"Acceso denegado al escribir log TenS (hint): {uaEx.Message}");
            }
        }
    }
}
