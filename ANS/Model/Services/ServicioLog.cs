
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
    }
}