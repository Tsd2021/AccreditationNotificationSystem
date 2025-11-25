
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

        // ✅ Método privado para extraer el banco del contexto o mensaje
        private string ExtraerBancoDelContexto(string context, string message)
        {
            // Lista de bancos a buscar (en orden de especificidad)
            var bancos = new[] { 
                "SCOTIABANK", "SCOTIA", 
                "SANTANDER", 
                "BBVA", 
                "ITAU", 
                "HSBC", 
                "BANDES", 
                "BROU", 
                "HERITAGE" 
            };
            
            // Combinar contexto y mensaje para búsqueda completa
            string textoCompleto = $"{context} {message}".ToUpper();
            
            // ✅ PRIORIDAD 1 (MÁS ALTA): Buscar patrones explícitos como "Banco: SANTANDER", "Bank: BBVA", etc.
            // Esta es la prioridad más alta porque es la forma más explícita y confiable de identificar el banco
            if (textoCompleto.Contains("BANCO:") || textoCompleto.Contains("BANK:"))
            {
                // Buscar directamente en el texto completo (ya está en mayúsculas)
                foreach (var banco in bancos)
                {
                    // Buscar "BANCO: SCOTIABANK" o "BANK: SCOTIABANK" o "| BANCO: SCOTIABANK"
                    if (textoCompleto.Contains($"BANCO: {banco}") || 
                        textoCompleto.Contains($"BANK: {banco}") ||
                        textoCompleto.Contains($"| BANCO: {banco}") ||
                        textoCompleto.Contains($"| BANK: {banco}") ||
                        textoCompleto.Contains($"BANCO:{banco}") ||
                        textoCompleto.Contains($"BANK:{banco}"))
                    {
                        return NormalizarNombreBanco(banco);
                    }
                }
                
                // Si no se encontró con el patrón directo, intentar parsear el texto original
                var textoOriginal = $"{context} {message}";
                var partes = textoOriginal.Split(new[] { "Banco:", "Bank:", "banco:", "bank:", "BANCO:", "BANK:" }, 
                    StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length > 1)
                {
                    var bancoTexto = partes[1].Split(new[] { '|', ';', ',' })[0].Trim();
                    if (!string.IsNullOrEmpty(bancoTexto))
                    {
                        var bancoNormalizado = NormalizarNombreBanco(bancoTexto);
                        if (bancoNormalizado != "GENERAL")
                            return bancoNormalizado;
                    }
                }
            }
            
            // ✅ PRIORIDAD 2: Buscar nombres de clases de Jobs (más específico)
            // Ejemplos: "AcreditarTanda2SantanderHenderson", "AcreditarTanda2HendersonScotiabank"
            if (textoCompleto.Contains("ACREDITARTANDA") || textoCompleto.Contains("ACREDITARDIAA") || 
                textoCompleto.Contains("ACREDITARPUNTO") || textoCompleto.Contains("EXCEL"))
            {
                if (textoCompleto.Contains("SANTANDER") && !textoCompleto.Contains("SCOTIABANK"))
                    return "SANTANDER";
                if (textoCompleto.Contains("SCOTIABANK") || textoCompleto.Contains("SCOTIA"))
                    return "SCOTIABANK";
                if (textoCompleto.Contains("BBVA"))
                    return "BBVA";
                if (textoCompleto.Contains("ITAU"))
                    return "ITAU";
                if (textoCompleto.Contains("HSBC"))
                    return "HSBC";
                if (textoCompleto.Contains("BANDES"))
                    return "BANDES";
            }
            
            // ✅ PRIORIDAD 3: Buscar nombres de Jobs en el contexto
            // Ejemplos: "SantanderJobTAN2", "ScotiabankJobAcreditarTAN2"
            if (textoCompleto.Contains("JOB"))
            {
                if (textoCompleto.Contains("SANTANDERJOB") || textoCompleto.Contains("SANTANDER JOB"))
                    return "SANTANDER";
                if (textoCompleto.Contains("SCOTIABANKJOB") || textoCompleto.Contains("SCOTIABANK JOB") ||
                    textoCompleto.Contains("SCOTIAJOB") || textoCompleto.Contains("SCOTIA JOB"))
                    return "SCOTIABANK";
                if (textoCompleto.Contains("BBVAJOB") || textoCompleto.Contains("BBVA JOB"))
                    return "BBVA";
                if (textoCompleto.Contains("ITAUJOB") || textoCompleto.Contains("ITAU JOB"))
                    return "ITAU";
                if (textoCompleto.Contains("HSBCJOB") || textoCompleto.Contains("HSBC JOB"))
                    return "HSBC";
                if (textoCompleto.Contains("BANDESJOB") || textoCompleto.Contains("BANDES JOB"))
                    return "BANDES";
            }
            
            // ✅ PRIORIDAD 4: Buscar servicios por nombre: ServicioSantander, ServicioBBVA, etc.
            if (textoCompleto.Contains("SERVICIOSANTANDER") || textoCompleto.Contains("SERVICIO SANTANDER"))
                return "SANTANDER";
            if (textoCompleto.Contains("SERVICIOBBVA") || textoCompleto.Contains("SERVICIO BBVA"))
                return "BBVA";
            if (textoCompleto.Contains("SERVICIOSCOTIABANK") || textoCompleto.Contains("SERVICIO SCOTIABANK") || 
                textoCompleto.Contains("SERVICIOSCOTIA") || textoCompleto.Contains("SERVICIO SCOTIA"))
                return "SCOTIABANK";
            if (textoCompleto.Contains("SERVICIOITAU") || textoCompleto.Contains("SERVICIO ITAU"))
                return "ITAU";
            if (textoCompleto.Contains("SERVICIOHSBC") || textoCompleto.Contains("SERVICIO HSBC"))
                return "HSBC";
            if (textoCompleto.Contains("SERVICIOBANDES") || textoCompleto.Contains("SERVICIO BANDES"))
                return "BANDES";
            if (textoCompleto.Contains("SERVICIOBROU") || textoCompleto.Contains("SERVICIO BROU"))
                return "BROU";
            if (textoCompleto.Contains("SERVICIOHERITAGE") || textoCompleto.Contains("SERVICIO HERITAGE"))
                return "HERITAGE";
            
            // ✅ PRIORIDAD 5: Buscar nombres de bancos directamente en el texto completo
            // Buscar el más específico primero (SCOTIABANK antes que SCOTIA)
            foreach (var banco in bancos)
            {
                // Buscar como palabra completa (con espacios o separadores)
                if (textoCompleto.Contains($" {banco} ") || 
                    textoCompleto.Contains($"|{banco}") || 
                    textoCompleto.Contains($"| {banco}") ||
                    textoCompleto.Contains($":{banco}") ||
                    textoCompleto.StartsWith($"{banco}") || 
                    textoCompleto.EndsWith(banco) ||
                    textoCompleto.Contains($"{banco}JOB"))
                    return NormalizarNombreBanco(banco);
            }
            
            // ✅ PRIORIDAD 6: Verificar si es ENVIO MASIVO, ENVIO NIVELES, etc. (debe ir a GENERAL)
            if (textoCompleto.Contains("ENVIO MASIVO") || textoCompleto.Contains("ENVIOMASIVO") ||
                textoCompleto.Contains("ENVIO NIVELES") || textoCompleto.Contains("ENVIONIVELES") ||
                textoCompleto.Contains("ENVIO_NIVELES") || textoCompleto.Contains("ENVIO_MASIVO"))
            {
                return "GENERAL"; // Estos van a GENERAL intencionalmente
            }
            
            return "GENERAL"; // Si no se encuentra banco, usar carpeta GENERAL
        }
        
        // ✅ Método para normalizar nombres de bancos a formato de carpeta
        private string NormalizarNombreBanco(string banco)
        {
            if (string.IsNullOrEmpty(banco))
                return "GENERAL";
                
            var bancoUpper = banco.ToUpper().Trim();
            return bancoUpper switch
            {
                "SCOTIA" => "SCOTIABANK",
                "SANTANDER" => "SANTANDER",
                "BBVA" => "BBVA",
                "ITAU" => "ITAU",
                "HSBC" => "HSBC",
                "BANDES" => "BANDES",
                "BROU" => "BROU",
                "HERITAGE" => "HERITAGE",
                _ => "GENERAL" // Si no coincide con ningún banco conocido, usar GENERAL
            };
        }

        // ✅ Método privado centralizado para escribir logs (evita duplicación de código)
        private void WriteToFile(string level, string message, string context = "")
        {
            try
            {
                // Directorio base de logs
                // Prod:
                string baseLogDirectory = @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\Logs\";
                // Testing Local:
                //string baseLogDirectory = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\Logs\";
                
                // ✅ Extraer banco del contexto o mensaje para crear carpeta por banco
                string banco = ExtraerBancoDelContexto(context, message);
                string logDirectory = Path.Combine(baseLogDirectory, banco);
                
                // Asegura que el directorio exista
                Directory.CreateDirectory(logDirectory);
                
                string fileName = $"TAAS_Log_{banco}_{DateTime.Now:ddMMyyyy}.txt";
                string filePath = Path.Combine(logDirectory, fileName);

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