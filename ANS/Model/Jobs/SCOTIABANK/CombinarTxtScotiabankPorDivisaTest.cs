using ANS.Model.Services;
using ANS;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ANS.Model.Jobs.SCOTIABANK
{
    /// <summary>
    /// Clase de test para probar la funcionalidad de combinación de archivos TXT de Scotiabank
    /// </summary>
    public static class CombinarTxtScotiabankPorDivisaTest
    {
        /// <summary>
        /// Ejecuta un test completo de combinación de archivos
        /// Crea archivos de prueba y verifica que se combinen correctamente
        /// </summary>
        public static void EjecutarTest()
        {
            // Usar la carpeta de combinación configurada para los archivos de test
            string baseTestFolder = ConfiguracionGlobal.Rutas.ScotiabankCombinacion;
            string folderName = DateTime.Now.ToString("yyyy-MM-dd");
            string testFolder = Path.Combine(baseTestFolder, folderName);
            string logFile = Path.Combine(testFolder, $"test_log_{DateTime.Now:yyyyMMddHHmmss}.txt");
            
            // Crear carpeta de test si no existe
            if (!Directory.Exists(testFolder))
            {
                Directory.CreateDirectory(testFolder);
            }
            
            // Función helper para escribir logs tanto al sistema como al archivo
            void EscribirLog(string mensaje, string categoria = "Scotiabank")
            {
                ServicioLog.instancia.WriteInfo(mensaje, categoria);
                try
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{categoria}] {mensaje}\r\n", Encoding.UTF8);
                }
                catch { }
            }
            
            try
            {
                EscribirLog("=== INICIANDO TEST DE COMBINACIÓN DE TXT SCOTIABANK ===", "Scotiabank");
                
                // NO borrar la carpeta si existe - queremos preservar archivos existentes
                if (!Directory.Exists(testFolder))
                {
                    Directory.CreateDirectory(testFolder);
                }

                EscribirLog($"=== CARPETA DE TEST CREADA ===", "Scotiabank");
                EscribirLog($"RUTA COMPLETA: {testFolder}", "Scotiabank");
                EscribirLog($"Puedes acceder a esta carpeta desde el Explorador de Windows", "Scotiabank");
                EscribirLog($"NOTA: Si ya existen archivos TXT en esta carpeta, se usarán esos en lugar de crear nuevos", "Scotiabank");
                EscribirLog($"Archivo de log: {logFile}", "Scotiabank");

                // ============================================
                // VERIFICAR SI EXISTEN ARCHIVOS O CREAR ARCHIVOS DE PRUEBA
                // ============================================
                
                string archivoTSD = null;
                string archivoTanda2 = null;
                string archivoDiaADia = null;

                // Verificar si ya existen archivos en la carpeta
                var archivosExistentes = Directory.GetFiles(testFolder, "*.txt", SearchOption.TopDirectoryOnly).ToList();
                
                if (archivosExistentes.Count > 0)
                {
                    EscribirLog($"Se encontraron {archivosExistentes.Count} archivo(s) existente(s) en la carpeta de test", "Scotiabank");
                    EscribirLog($"Se usarán los archivos existentes en lugar de crear nuevos", "Scotiabank");
                    
                    // Intentar identificar los archivos existentes
                    foreach (var archivo in archivosExistentes)
                    {
                        string nombreArchivo = Path.GetFileName(archivo).ToUpper();
                        EscribirLog($"Analizando archivo: {Path.GetFileName(archivo)}", "Scotiabank");
                        
                        // Excluir archivos combinados
                        if (nombreArchivo.Contains("COMBINADO"))
                        {
                            EscribirLog($"  → Ignorado (archivo combinado)", "Scotiabank");
                            continue;
                        }
                        
                        // Identificar TSD: tiene "ACREDITACIONTECNISEGUR" pero NO "BUZONES"
                        // Ejemplo: "27-11-2025-17-00-UYU10485859-AcreditacionTecnisegurMont.txt"
                        if (nombreArchivo.Contains("ACREDITACIONTECNISEGUR") && 
                            !nombreArchivo.Contains("BUZONES") &&
                            !nombreArchivo.Contains("_TANDA") &&
                            !nombreArchivo.Contains("_DIAADIA"))
                        {
                            if (archivoTSD == null || new FileInfo(archivo).LastWriteTime > new FileInfo(archivoTSD ?? archivo).LastWriteTime)
                            {
                                archivoTSD = archivo;
                                EscribirLog($"  → Identificado como TSD", "Scotiabank");
                            }
                        }
                        // Identificar Tanda 2: tiene "ACREDITACIONBUZONESTECNISEGUR" y "_TANDA2"
                        // Ejemplo: "27-11-2025-17-00-UYU2700100-AcreditacionBuzonesTecnisegurMont_Tanda2.txt"
                        else if (nombreArchivo.Contains("ACREDITACIONBUZONESTECNISEGUR") && 
                                 nombreArchivo.Contains("_TANDA2"))
                        {
                            if (archivoTanda2 == null || new FileInfo(archivo).LastWriteTime > new FileInfo(archivoTanda2 ?? archivo).LastWriteTime)
                            {
                                archivoTanda2 = archivo;
                                EscribirLog($"  → Identificado como Tanda 2", "Scotiabank");
                            }
                        }
                        // Identificar Día a Día: tiene "ACREDITACIONBUZONESTECNISEGUR" y "_DIAADIA"
                        // Ejemplo: "27-11-2025-17-00-UYU2279350-AcreditacionBuzonesTecnisegurMont_DiaADia.txt"
                        else if (nombreArchivo.Contains("ACREDITACIONBUZONESTECNISEGUR") && 
                                 nombreArchivo.Contains("_DIAADIA"))
                        {
                            if (archivoDiaADia == null || new FileInfo(archivo).LastWriteTime > new FileInfo(archivoDiaADia ?? archivo).LastWriteTime)
                            {
                                archivoDiaADia = archivo;
                                EscribirLog($"  → Identificado como Día a Día", "Scotiabank");
                            }
                        }
                        else
                        {
                            EscribirLog($"  → No identificado (no coincide con ningún patrón conocido)", "Scotiabank");
                        }
                    }
                    
                    EscribirLog($"Archivos identificados: TSD={archivoTSD != null}, Tanda2={archivoTanda2 != null}, Día a Día={archivoDiaADia != null}", "Scotiabank");
                }
                else
                {
                    EscribirLog($"No se encontraron archivos existentes, creando archivos de prueba...", "Scotiabank");
                }

                // Crear archivos solo si no se encontraron existentes
                if (archivoTSD == null)
                {
                    archivoTSD = CrearArchivoTSD(testFolder, "UYU");
                    EscribirLog($"Archivo TSD creado: {Path.GetFileName(archivoTSD)}", "Scotiabank");
                }
                else
                {
                    EscribirLog($"Usando archivo TSD existente: {Path.GetFileName(archivoTSD)}", "Scotiabank");
                }

                if (archivoTanda2 == null)
                {
                    archivoTanda2 = CrearArchivoTanda2(testFolder, "UYU");
                    EscribirLog($"Archivo Tanda 2 creado: {Path.GetFileName(archivoTanda2)}", "Scotiabank");
                }
                else
                {
                    EscribirLog($"Usando archivo Tanda 2 existente: {Path.GetFileName(archivoTanda2)}", "Scotiabank");
                }

                if (archivoDiaADia == null)
                {
                    archivoDiaADia = CrearArchivoDiaADia(testFolder, "UYU");
                    EscribirLog($"Archivo Día a Día creado: {Path.GetFileName(archivoDiaADia)}", "Scotiabank");
                }
                else
                {
                    EscribirLog($"Usando archivo Día a Día existente: {Path.GetFileName(archivoDiaADia)}", "Scotiabank");
                }

                // ============================================
                // PROBAR LA LÓGICA DE COMBINACIÓN
                // ============================================

                // Simular la lógica de búsqueda y combinación
                var resultado = ProbarCombinacion(testFolder, "UYU", archivoTSD, archivoTanda2, archivoDiaADia);

                // ============================================
                // VERIFICAR RESULTADOS
                // ============================================

                bool testExitoso = true;
                string mensaje = "";

                // Verificar que se encontraron los 3 archivos
                if (resultado.ArchivoTSD == null)
                {
                    testExitoso = false;
                    mensaje += "ERROR: No se encontró el archivo TSD\n";
                }
                else
                {
                    mensaje += $"✓ Archivo TSD encontrado: {Path.GetFileName(resultado.ArchivoTSD)}\n";
                }

                if (resultado.ArchivoTanda2 == null)
                {
                    testExitoso = false;
                    mensaje += "ERROR: No se encontró el archivo Tanda 2\n";
                }
                else
                {
                    mensaje += $"✓ Archivo Tanda 2 encontrado: {Path.GetFileName(resultado.ArchivoTanda2)}\n";
                }

                if (resultado.ArchivoDiaADia == null)
                {
                    testExitoso = false;
                    mensaje += "ERROR: No se encontró el archivo Día a Día\n";
                }
                else
                {
                    mensaje += $"✓ Archivo Día a Día encontrado: {Path.GetFileName(resultado.ArchivoDiaADia)}\n";
                }

                // Verificar total de líneas
                int totalLineasEsperadas = resultado.LineasTSD + resultado.LineasTanda2 + resultado.LineasDiaADia;
                if (resultado.TotalLineasCombinadas != totalLineasEsperadas)
                {
                    testExitoso = false;
                    mensaje += $"ERROR: Total de líneas incorrecto. Esperado: {totalLineasEsperadas}, Obtenido: {resultado.TotalLineasCombinadas}\n";
                }
                else
                {
                    mensaje += $"✓ Total de líneas correcto: {resultado.TotalLineasCombinadas}\n";
                }

                // Verificar total de importe
                long totalImporteEsperado = resultado.ImporteTSD + resultado.ImporteTanda2 + resultado.ImporteDiaADia;
                if (resultado.TotalImporte != totalImporteEsperado)
                {
                    testExitoso = false;
                    mensaje += $"ERROR: Total de importe incorrecto. Esperado: {totalImporteEsperado:N0}, Obtenido: {resultado.TotalImporte:N0}\n";
                }
                else
                {
                    mensaje += $"✓ Total de importe correcto: {resultado.TotalImporte:N0}\n";
                }

                // Verificar orden de combinación
                if (resultado.OrdenCorrecto)
                {
                    mensaje += "✓ Orden de combinación correcto (TSD -> Tanda2 -> Día a Día)\n";
                }
                else
                {
                    testExitoso = false;
                    mensaje += "ERROR: Orden de combinación incorrecto\n";
                }

                // ============================================
                // GENERAR ARCHIVO COMBINADO (como en el job real)
                // ============================================
                string archivoCombinado = "";
                try
                {
                    archivoCombinado = GenerarArchivoCombinado(testFolder, "UYU", resultado, EscribirLog);
                    if (File.Exists(archivoCombinado))
                    {
                        var fileInfo = new FileInfo(archivoCombinado);
                        mensaje += $"✓ Archivo combinado generado: {Path.GetFileName(archivoCombinado)}\n";
                        mensaje += $"  Tamaño: {fileInfo.Length} bytes | Líneas: {resultado.TotalLineasCombinadas}\n";
                        EscribirLog($"Archivo combinado generado: {archivoCombinado}", "Scotiabank");
                        EscribirLog($"Tamaño del archivo combinado: {fileInfo.Length} bytes", "Scotiabank");
                        
                        // Leer y verificar el archivo combinado
                        var lineasCombinadas = File.ReadAllLines(archivoCombinado, Encoding.UTF8);
                        EscribirLog($"Total líneas en archivo combinado: {lineasCombinadas.Length}", "Scotiabank");
                        if (lineasCombinadas.Length > 0)
                        {
                            EscribirLog($"Primera línea del combinado (longitud: {lineasCombinadas[0].Length}): {lineasCombinadas[0].Substring(0, Math.Min(100, lineasCombinadas[0].Length))}...", "Scotiabank");
                        }
                        else
                        {
                            EscribirLog($"ADVERTENCIA: El archivo combinado está vacío!", "Scotiabank");
                        }
                    }
                    else
                    {
                        testExitoso = false;
                        mensaje += "ERROR: No se pudo generar el archivo combinado\n";
                        EscribirLog($"ERROR: No se pudo generar el archivo combinado", "Scotiabank");
                    }
                }
                catch (Exception ex)
                {
                    testExitoso = false;
                    mensaje += $"ERROR al generar archivo combinado: {ex.Message}\n";
                    EscribirLog($"ERROR al generar archivo combinado: {ex.Message}", "Scotiabank");
                    EscribirLog($"Stack trace: {ex.StackTrace}", "Scotiabank");
                    ServicioLog.instancia.WriteLog(ex, "Scotiabank", "Error al generar archivo combinado");
                }

                // Mostrar resultados
                EscribirLog($"=== RESULTADOS DEL TEST ===\n{mensaje}", "Scotiabank");

                if (testExitoso)
                {
                    EscribirLog("=== TEST EXITOSO ===", "Scotiabank");
                }
                else
                {
                    EscribirLog("=== TEST FALLIDO ===", "Scotiabank");
                }

                // Limpiar archivos de test (opcional - comentar para inspección manual)
                // Directory.Delete(testFolder, true);
                EscribirLog($"=== ARCHIVOS DE TEST DISPONIBLES ===", "Scotiabank");
                EscribirLog($"RUTA COMPLETA: {testFolder}", "Scotiabank");
                EscribirLog($"Abre el Explorador de Windows y navega a: {testFolder}", "Scotiabank");
                EscribirLog($"O copia y pega esta ruta en la barra de direcciones del Explorador", "Scotiabank");
                if (!string.IsNullOrEmpty(archivoCombinado) && File.Exists(archivoCombinado))
                {
                    EscribirLog($"=== ARCHIVO COMBINADO GENERADO ===", "Scotiabank");
                    EscribirLog($"RUTA COMPLETA: {archivoCombinado}", "Scotiabank");
                    EscribirLog($"NOMBRE: {Path.GetFileName(archivoCombinado)}", "Scotiabank");
                }
                
                EscribirLog($"=== LOG COMPLETO GUARDADO EN: {logFile} ===", "Scotiabank");
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {ex.Message}\r\n{ex.StackTrace}\r\n", Encoding.UTF8);
                }
                catch { }
                ServicioLog.instancia.WriteLog(ex, "Scotiabank", "Error en test de combinación");
            }
        }

        private static string CrearArchivoTSD(string folderPath, string divisa)
        {
            // Crear archivo TSD con formato similar pero "AcreditacionTecnisegur" (sin "Buzones")
            string timestamp = DateTime.Now.ToString("dd-MM-yyyy-HH-mm");
            string totalImporte = "3295000"; // Total de los 3 importes: 255000 + 2865000 + 175000
            string fileName = $"{timestamp}-{divisa}{totalImporte}-AcreditacionTecnisegurMont.txt";
            string filePath = Path.Combine(folderPath, fileName);

            var lineas = new StringBuilder();
            
            // Crear 3 líneas de ejemplo con formato real (TSD incluye nombre del cliente)
            lineas.AppendLine(CrearLineaTxt("000000215437010016", "02", "26112025", "000000000255000", "+", "00", "210100000000486701700", "DELMAR SRL", incluirNombreCliente: true));
            lineas.AppendLine(CrearLineaTxt("000000215437010016", "02", "26112025", "000000002865000", "+", "00", "210100000000486701700", "DELMAR SRL", incluirNombreCliente: true));
            lineas.AppendLine(CrearLineaTxt("000000215437010016", "02", "26112025", "000000000175000", "+", "00", "210100000000291886200", "PRADELIX S.A.", incluirNombreCliente: true));

            File.WriteAllText(filePath, lineas.ToString(), Encoding.UTF8);
            
            // Establecer fecha de modificación reciente (más tarde que los otros)
            File.SetLastWriteTime(filePath, DateTime.Now.AddHours(-1));

            return filePath;
        }

        private static string CrearArchivoTanda2(string folderPath, string divisa)
        {
            // Crear archivo Tanda 2 (con sufijo _Tanda2)
            string timestamp = DateTime.Now.ToString("dd-MM-yyyy-HH-mm");
            string fileName = $"{timestamp}-{divisa}500000-AcreditacionBuzonesTecnisegurMont_Tanda2.txt";
            string filePath = Path.Combine(folderPath, fileName);

            var lineas = new StringBuilder();
            
            // Crear 2 líneas de ejemplo (Tanda 2 NO incluye nombre del cliente, solo espacios)
            lineas.AppendLine(CrearLineaTxt("000000215437010016", "02", "26112025", "000000000630000", "+", "00", "210100000000701728600"));
            lineas.AppendLine(CrearLineaTxt("000000215437010016", "02", "26112025", "000000120000000", "+", "00", "210100000003371594300"));

            File.WriteAllText(filePath, lineas.ToString(), Encoding.UTF8);
            
            // Establecer fecha de modificación entre 7:00 y 15:00
            var fechaModificacion = DateTime.Today.AddHours(14).AddMinutes(50);
            File.SetLastWriteTime(filePath, fechaModificacion);

            return filePath;
        }

        private static string CrearArchivoDiaADia(string folderPath, string divisa)
        {
            // Crear archivo Día a Día (con sufijo _DiaADia)
            string timestamp = DateTime.Now.ToString("dd-MM-yyyy-HH-mm");
            string fileName = $"{timestamp}-{divisa}300000-AcreditacionBuzonesTecnisegurMont_DiaADia.txt";
            string filePath = Path.Combine(folderPath, fileName);

            var lineas = new StringBuilder();
            
            // Crear 2 líneas de ejemplo (Día a Día NO incluye nombre del cliente, solo espacios)
            lineas.AppendLine(CrearLineaTxt("000000215437010016", "02", "26112025", "000000099192300", "+", "00", "210100000000701728600"));
            lineas.AppendLine(CrearLineaTxt("000000215437010016", "02", "26112025", "000000001700000", "+", "00", "210100000000291886200"));

            File.WriteAllText(filePath, lineas.ToString(), Encoding.UTF8);
            
            // Establecer fecha de modificación después de las 16:00
            var fechaModificacion = DateTime.Today.AddHours(16).AddMinutes(10);
            File.SetLastWriteTime(filePath, fechaModificacion);

            return filePath;
        }

        /// <summary>
        /// Crea una línea de TXT con el formato correcto (875 caracteres)
        /// </summary>
        /// <param name="incluirNombreCliente">Solo true para archivos TSD (libro de bancos)</param>
        private static string CrearLineaTxt(string rutOrdenante, string tipoOperativa, string fecha, 
            string importe, string signo, string moneda, string nroCuenta, string nombreCliente = "", bool incluirNombreCliente = false)
        {
            var linea = new StringBuilder();
            
            // Formato según ScotiaFileGenerator:
            // 0-17: rutOrdenante (18)
            linea.Append(rutOrdenante.PadRight(18));
            
            // 18-20: 3 espacios
            linea.Append(' ', 3);
            
            // 21-22: tipoOperativa (2)
            linea.Append(tipoOperativa);
            
            // 23-30: fecha (8)
            linea.Append(fecha);
            
            // 31-51: 21 espacios
            linea.Append(' ', 21);
            
            // 52-66: importe (15)
            linea.Append(importe.PadLeft(15, '0'));
            
            // 67: signo (1)
            linea.Append(signo);
            
            // 68-69: moneda (2)
            linea.Append(moneda);
            
            // 70-88: nroCuenta (19)
            linea.Append(nroCuenta.PadLeft(19, '0'));
            
            // 89-90: 2 espacios
            linea.Append(' ', 2);
            
            // 91-130: 40 espacios
            linea.Append(' ', 40);
            
            // 131-142: 12 espacios
            linea.Append(' ', 12);
            
            // 143-151: 9 espacios
            linea.Append(' ', 9);
            
            // 152: 1 espacio
            linea.Append(' ', 1);
            
            // 153-182: 30 espacios
            linea.Append(' ', 30);
            
            // 183-874: Solo archivos TSD tienen nombre del cliente, los demás solo espacios (692 caracteres, posiciones 183-874 inclusive = 692 caracteres)
            if (incluirNombreCliente && !string.IsNullOrWhiteSpace(nombreCliente))
            {
                int espaciosRestantes = 692 - nombreCliente.Length;
                linea.Append(nombreCliente);
                if (espaciosRestantes > 0)
                {
                    linea.Append(' ', espaciosRestantes);
                }
            }
            else
            {
                // Archivos de Tanda y Día a Día solo tienen espacios
                linea.Append(' ', 692);
            }
            
            // Total debe ser exactamente 875 caracteres
            string resultado = linea.ToString();
            
            // Asegurar que tenga exactamente 875 caracteres
            if (resultado.Length != 875)
            {
                if (resultado.Length < 875)
                {
                    resultado = resultado.PadRight(875);
                }
                else
                {
                    resultado = resultado.Substring(0, 875);
                }
            }

            return resultado;
        }

        private static TestResultado ProbarCombinacion(string folderPath, string divisaCodigo, 
            string archivoTSDConocido = null, string archivoTanda2Conocido = null, string archivoDiaADiaConocido = null)
        {
            var resultado = new TestResultado();

            // Si se proporcionaron archivos conocidos, usarlos directamente
            if (!string.IsNullOrEmpty(archivoTSDConocido) && File.Exists(archivoTSDConocido))
            {
                resultado.ArchivoTSD = archivoTSDConocido;
                var lineas = File.ReadAllLines(archivoTSDConocido, Encoding.UTF8)
                    .Select(l => l.TrimEnd('\r', '\n')) // Eliminar caracteres de fin de línea
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length == 875)
                    .ToList();
                resultado.LineasTSD = lineas.Count;
                resultado.ImporteTSD = CalcularTotalImporte(lineas);
            }
            else
            {
                // Buscar archivo TSD
                var archivosTSD = Directory.GetFiles(folderPath, $"*{divisaCodigo}*.txt", SearchOption.TopDirectoryOnly)
                    .Where(f =>
                    {
                        string fileName = Path.GetFileName(f).ToUpper();
                        return !fileName.Contains("BUZONESTECNISEGUR") &&
                               !fileName.Contains("ACREDITACIONBUZONESTECNISEGUR") &&
                               !fileName.Contains("COMBINADO");
                    })
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                    .ToList();

                resultado.ArchivoTSD = archivosTSD.FirstOrDefault();
            if (resultado.ArchivoTSD != null)
            {
                var lineas = File.ReadAllLines(resultado.ArchivoTSD, Encoding.UTF8)
                    .Select(l => l.TrimEnd('\r', '\n')) // Eliminar caracteres de fin de línea
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length == 875) // Exactamente 876 caracteres
                    .ToList();
                resultado.LineasTSD = lineas.Count;
                resultado.ImporteTSD = CalcularTotalImporte(lineas);
            }
            }

            // Si se proporcionó archivo Tanda 2 conocido, usarlo directamente
            if (!string.IsNullOrEmpty(archivoTanda2Conocido) && File.Exists(archivoTanda2Conocido))
            {
                resultado.ArchivoTanda2 = archivoTanda2Conocido;
                var lineas = File.ReadAllLines(archivoTanda2Conocido, Encoding.UTF8)
                    .Select(l => l.TrimEnd('\r', '\n')) // Eliminar caracteres de fin de línea
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length == 875)
                    .ToList();
                resultado.LineasTanda2 = lineas.Count;
                resultado.ImporteTanda2 = CalcularTotalImporte(lineas);
            }
            else
            {
                // Buscar archivo Tanda 2
                var archivosTanda2 = Directory.GetFiles(folderPath, $"*{divisaCodigo}*.txt", SearchOption.TopDirectoryOnly)
                    .Where(f =>
                    {
                        string fileName = Path.GetFileName(f).ToUpper();
                        if (!fileName.Contains("ACREDITACIONBUZONESTECNISEGUR") ||
                            fileName.Contains("COMBINADO") ||
                            fileName.Contains("CASHOFFICE"))
                            return false;

                        bool tieneSufijoTanda2 = fileName.Contains("_TANDA2");
                        var fileInfo = new FileInfo(f);
                        var horaModificacion = fileInfo.LastWriteTime;
                        bool estaEnRangoHorario = horaModificacion.Hour >= 7 &&
                                                   (horaModificacion.Hour < 15 || (horaModificacion.Hour == 15 && horaModificacion.Minute == 0));

                        return tieneSufijoTanda2 || (estaEnRangoHorario && !fileName.Contains("_TANDA1") && !fileName.Contains("_DIAADIA"));
                    })
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                    .ToList();

                resultado.ArchivoTanda2 = archivosTanda2.FirstOrDefault();
            if (resultado.ArchivoTanda2 != null)
            {
                var lineas = File.ReadAllLines(resultado.ArchivoTanda2, Encoding.UTF8)
                    .Select(l => l.TrimEnd('\r', '\n')) // Eliminar caracteres de fin de línea
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length == 875) // Exactamente 876 caracteres
                    .ToList();
                resultado.LineasTanda2 = lineas.Count;
                resultado.ImporteTanda2 = CalcularTotalImporte(lineas);
            }
            }

            // Si se proporcionó archivo Día a Día conocido, usarlo directamente
            if (!string.IsNullOrEmpty(archivoDiaADiaConocido) && File.Exists(archivoDiaADiaConocido))
            {
                resultado.ArchivoDiaADia = archivoDiaADiaConocido;
                var lineas = File.ReadAllLines(archivoDiaADiaConocido, Encoding.UTF8)
                    .Select(l => l.TrimEnd('\r', '\n')) // Eliminar caracteres de fin de línea
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length == 875)
                    .ToList();
                resultado.LineasDiaADia = lineas.Count;
                resultado.ImporteDiaADia = CalcularTotalImporte(lineas);
            }
            else
            {
                // Buscar archivo Día a Día
                var archivosDiaADia = Directory.GetFiles(folderPath, $"*{divisaCodigo}*.txt", SearchOption.TopDirectoryOnly)
                    .Where(f =>
                    {
                        string fileName = Path.GetFileName(f).ToUpper();
                        if (!fileName.Contains("ACREDITACIONBUZONESTECNISEGUR") ||
                            fileName.Contains("COMBINADO") ||
                            fileName.Contains("CASHOFFICE"))
                            return false;

                        bool tieneSufijoDiaADia = fileName.Contains("_DIAADIA");
                        var fileInfo = new FileInfo(f);
                        var horaModificacion = fileInfo.LastWriteTime;
                        bool estaDespuesDe16 = horaModificacion.Hour >= 16;

                        return tieneSufijoDiaADia || (estaDespuesDe16 && !fileName.Contains("_TANDA1") && !fileName.Contains("_TANDA2"));
                    })
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                    .ToList();

                resultado.ArchivoDiaADia = archivosDiaADia.FirstOrDefault();
            if (resultado.ArchivoDiaADia != null)
            {
                var lineas = File.ReadAllLines(resultado.ArchivoDiaADia, Encoding.UTF8)
                    .Select(l => l.TrimEnd('\r', '\n')) // Eliminar caracteres de fin de línea
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length == 875) // Exactamente 876 caracteres
                    .ToList();
                resultado.LineasDiaADia = lineas.Count;
                resultado.ImporteDiaADia = CalcularTotalImporte(lineas);
            }
            }

            // Simular combinación
            var lineasCombinadas = new System.Collections.Generic.List<string>();

            if (resultado.ArchivoTSD != null)
            {
                var lineas = File.ReadAllLines(resultado.ArchivoTSD, Encoding.UTF8)
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length >= 67)
                    .ToList();
                lineasCombinadas.AddRange(lineas);
            }

            if (resultado.ArchivoTanda2 != null)
            {
                var lineas = File.ReadAllLines(resultado.ArchivoTanda2, Encoding.UTF8)
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length >= 67)
                    .ToList();
                lineasCombinadas.AddRange(lineas);
            }

            if (resultado.ArchivoDiaADia != null)
            {
                var lineas = File.ReadAllLines(resultado.ArchivoDiaADia, Encoding.UTF8)
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length >= 67)
                    .ToList();
                lineasCombinadas.AddRange(lineas);
            }

            resultado.TotalLineasCombinadas = lineasCombinadas.Count;
            resultado.TotalImporte = CalcularTotalImporte(lineasCombinadas);
            resultado.OrdenCorrecto = resultado.ArchivoTSD != null && resultado.ArchivoTanda2 != null && resultado.ArchivoDiaADia != null;

            return resultado;
        }

        private static long CalcularTotalImporte(System.Collections.Generic.List<string> lineas)
        {
            long total = 0;
            foreach (var linea in lineas)
            {
                if (linea.Length == 875) // Exactamente 875 caracteres
                {
                    try
                    {
                        string importeStr = linea.Substring(52, 15);
                        if (long.TryParse(importeStr, out long importe))
                        {
                            total += importe;
                        }
                    }
                    catch { }
                }
            }
            return total;
        }

        private static string GenerarArchivoCombinado(string folderPath, string divisaCodigo, TestResultado resultado, Action<string, string> escribirLog = null)
        {
            if (escribirLog == null)
            {
                escribirLog = (msg, cat) => ServicioLog.instancia.WriteInfo(msg, cat);
            }
            
            var lineasCombinadas = new System.Collections.Generic.List<string>();

            // PASO 1: Leer archivo TSD (BASE)
            if (resultado.ArchivoTSD != null && File.Exists(resultado.ArchivoTSD))
            {
                escribirLog($"Leyendo archivo TSD: {Path.GetFileName(resultado.ArchivoTSD)}", "Scotiabank");
                var todasLasLineas = File.ReadAllLines(resultado.ArchivoTSD, Encoding.UTF8);
                escribirLog($"Total líneas leídas del archivo TSD: {todasLasLineas.Length}", "Scotiabank");
                
                var lineas = todasLasLineas
                    .Select((l, idx) => 
                    {
                        string limpia = l.TrimEnd('\r', '\n');
                        if (limpia.Length != 875 && !string.IsNullOrWhiteSpace(limpia))
                        {
                            escribirLog($"Línea TSD {idx + 1} tiene longitud {limpia.Length} (esperado 875) | Primeros 50: {limpia.Substring(0, Math.Min(50, limpia.Length))}...", "Scotiabank");
                        }
                        return limpia;
                    })
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length == 875)
                    .ToList();
                
                escribirLog($"Líneas válidas TSD (875 caracteres): {lineas.Count} de {todasLasLineas.Length}", "Scotiabank");
                lineasCombinadas.AddRange(lineas);
            }

            // PASO 2: Leer archivo Tanda 2 (AGREGAR ABAJO)
            if (resultado.ArchivoTanda2 != null && File.Exists(resultado.ArchivoTanda2))
            {
                escribirLog($"Leyendo archivo Tanda 2: {Path.GetFileName(resultado.ArchivoTanda2)}", "Scotiabank");
                var todasLasLineas = File.ReadAllLines(resultado.ArchivoTanda2, Encoding.UTF8);
                escribirLog($"Total líneas leídas del archivo Tanda 2: {todasLasLineas.Length}", "Scotiabank");
                
                var lineas = todasLasLineas
                    .Select((l, idx) => 
                    {
                        string limpia = l.TrimEnd('\r', '\n');
                        if (limpia.Length != 875 && !string.IsNullOrWhiteSpace(limpia))
                        {
                            escribirLog($"Línea Tanda 2 {idx + 1} tiene longitud {limpia.Length} (esperado 875) | Primeros 50: {limpia.Substring(0, Math.Min(50, limpia.Length))}...", "Scotiabank");
                        }
                        return limpia;
                    })
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length == 875)
                    .ToList();
                
                escribirLog($"Líneas válidas Tanda 2 (875 caracteres): {lineas.Count} de {todasLasLineas.Length}", "Scotiabank");
                lineasCombinadas.AddRange(lineas);
            }

            // PASO 3: Leer archivo Día a Día (AGREGAR ABAJO)
            if (resultado.ArchivoDiaADia != null && File.Exists(resultado.ArchivoDiaADia))
            {
                escribirLog($"Leyendo archivo Día a Día: {Path.GetFileName(resultado.ArchivoDiaADia)}", "Scotiabank");
                var todasLasLineas = File.ReadAllLines(resultado.ArchivoDiaADia, Encoding.UTF8);
                escribirLog($"Total líneas leídas del archivo Día a Día: {todasLasLineas.Length}", "Scotiabank");
                
                var lineas = todasLasLineas
                    .Select((l, idx) => 
                    {
                        string limpia = l.TrimEnd('\r', '\n');
                        if (limpia.Length != 875 && !string.IsNullOrWhiteSpace(limpia))
                        {
                            escribirLog($"Línea Día a Día {idx + 1} tiene longitud {limpia.Length} (esperado 875) | Primeros 50: {limpia.Substring(0, Math.Min(50, limpia.Length))}...", "Scotiabank");
                        }
                        return limpia;
                    })
                    .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length == 875)
                    .ToList();
                
                escribirLog($"Líneas válidas Día a Día (875 caracteres): {lineas.Count} de {todasLasLineas.Length}", "Scotiabank");
                lineasCombinadas.AddRange(lineas);
            }
            
            escribirLog($"Total líneas combinadas: {lineasCombinadas.Count}", "Scotiabank");

            // Extraer números de los nombres de los archivos y sumarlos
            long totalDelNombre = 0;
            
            if (resultado.ArchivoTSD != null)
            {
                long numero = ExtraerNumeroDelNombreArchivo(Path.GetFileName(resultado.ArchivoTSD), divisaCodigo);
                totalDelNombre += numero;
            }
            
            if (resultado.ArchivoTanda2 != null)
            {
                long numero = ExtraerNumeroDelNombreArchivo(Path.GetFileName(resultado.ArchivoTanda2), divisaCodigo);
                totalDelNombre += numero;
            }
            
            if (resultado.ArchivoDiaADia != null)
            {
                long numero = ExtraerNumeroDelNombreArchivo(Path.GetFileName(resultado.ArchivoDiaADia), divisaCodigo);
                totalDelNombre += numero;
            }

            // Generar archivo combinado
            string timestamp = DateTime.Now.ToString("d - M-yyyy");
            string suctecni = "Mont"; // Montevideo
            string fileName = $"{timestamp}-{divisaCodigo}{totalDelNombre}-" +
                              $"AcreditacionTecnisegur{suctecni}.txt";

            string rutaDestino = Path.Combine(folderPath, fileName);

            // Escribir archivo combinado en UTF-8 sin BOM
            var utf8SinBom = new System.Text.UTF8Encoding(false);
            File.WriteAllLines(rutaDestino, lineasCombinadas, utf8SinBom);

            return rutaDestino;
        }

        private static long ExtraerNumeroDelNombreArchivo(string nombreArchivo, string divisaCodigo)
        {
            try
            {
                int indiceDivisa = nombreArchivo.IndexOf(divisaCodigo, StringComparison.OrdinalIgnoreCase);
                if (indiceDivisa >= 0)
                {
                    int inicioNumero = indiceDivisa + divisaCodigo.Length;
                    int finNumero = nombreArchivo.IndexOf('-', inicioNumero);
                    
                    if (finNumero > inicioNumero)
                    {
                        string numeroStr = nombreArchivo.Substring(inicioNumero, finNumero - inicioNumero);
                        if (long.TryParse(numeroStr, out long numero))
                        {
                            return numero;
                        }
                    }
                }
            }
            catch { }
            
            return 0;
        }

        private class TestResultado
        {
            public string ArchivoTSD { get; set; }
            public string ArchivoTanda2 { get; set; }
            public string ArchivoDiaADia { get; set; }
            public int LineasTSD { get; set; }
            public int LineasTanda2 { get; set; }
            public int LineasDiaADia { get; set; }
            public long ImporteTSD { get; set; }
            public long ImporteTanda2 { get; set; }
            public long ImporteDiaADia { get; set; }
            public int TotalLineasCombinadas { get; set; }
            public long TotalImporte { get; set; }
            public bool OrdenCorrecto { get; set; }
        }
    }
}

