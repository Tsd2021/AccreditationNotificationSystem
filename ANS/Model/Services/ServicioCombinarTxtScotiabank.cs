using ANS.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Services
{
    /// <summary>
    /// Servicio para combinar archivos TXT de Scotiabank por divisa
    /// Reutilizable tanto para jobs como para ejecución manual
    /// </summary>
    public class ServicioCombinarTxtScotiabank
    {
        /// <summary>
        /// Combina archivos TXT de Scotiabank para una ciudad específica
        /// </summary>
        public static async Task CombinarArchivosPorCiudad(string ciudad)
        {
            // Procesar UYU y USD
            await CombinarArchivosPorDivisa(ciudad, "UYU", "00");
            await CombinarArchivosPorDivisa(ciudad, "USD", "01");
        }

        /// <summary>
        /// Combina archivos TXT de una divisa específica para una ciudad
        /// </summary>
        public static async Task CombinarArchivosPorDivisa(string ciudad, string divisaCodigo, string monedaCodigo)
        {
            try
            {
                string basePath = ciudad == "MONTEVIDEO" 
                    ? ConfiguracionGlobal.Rutas.ScotiabankMontevideo 
                    : ConfiguracionGlobal.Rutas.ScotiabankMaldonado;
                
                string folderName = DateTime.Now.ToString("yyyy-MM-dd");
                string folderPath = Path.Combine(basePath, folderName);

                ServicioLog.instancia.WriteInfo(
                    $"Iniciando combinación de archivos para {divisaCodigo} | Ciudad: {ciudad} | Carpeta: {folderPath}",
                    $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Inicio");

                // Crear carpeta si no existe
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    ServicioLog.instancia.WriteInfo(
                        $"Carpeta creada: {folderPath}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Creación Carpeta");
                }

                // ============================================
                // PASO 1: Buscar archivo TSD más reciente
                // ============================================
                ServicioLog.instancia.WriteInfo(
                    $"PASO 1: Buscando archivo TSD más reciente para {divisaCodigo}",
                    $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 1");

                var archivosTSD = Directory.GetFiles(folderPath, $"*{divisaCodigo}*.txt", SearchOption.TopDirectoryOnly)
                    .Where(f => 
                    {
                        string fileName = Path.GetFileName(f).ToUpper();
                        return fileName.Contains("ACREDITACIONTECNISEGUR") &&
                               !fileName.Contains("BUZONES") &&
                               !fileName.Contains("ACREDITACIONBUZONESTECNISEGUR") &&
                               !fileName.Contains("_TANDA") &&
                               !fileName.Contains("_DIAADIA") &&
                               !fileName.Contains("COMBINADO");
                    })
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                FileInfo archivoTSDInfo = archivosTSD.FirstOrDefault();
                string archivoTSD = archivoTSDInfo?.FullName;

                if (archivoTSDInfo != null)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 1 - Archivo TSD encontrado: {archivoTSDInfo.Name} | " +
                        $"Hora creación: {archivoTSDInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss} | " +
                        $"Tamaño: {archivoTSDInfo.Length} bytes",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 1");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 1 - No se encontró archivo TSD para {divisaCodigo}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 1");
                }

                // ============================================
                // PASO 2: Buscar archivos Tanda 2
                // ============================================
                ServicioLog.instancia.WriteInfo(
                    $"PASO 2: Buscando archivos Tanda 2 (entre 7:00 y 15:00) para {divisaCodigo}",
                    $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 2");

                var archivosTanda2 = Directory.GetFiles(folderPath, $"*{divisaCodigo}*.txt", SearchOption.TopDirectoryOnly)
                    .Where(f =>
                    {
                        string fileName = Path.GetFileName(f).ToUpper();
                        if (!fileName.Contains("ACREDITACIONBUZONESTECNISEGUR") || 
                            fileName.Contains("COMBINADO") ||
                            fileName.Contains("CASHOFFICE") ||
                            fileName.Contains("_TANDA1") ||
                            fileName.Contains("_DIAADIA"))
                            return false;

                        bool tieneSufijoTanda2 = fileName.Contains("_TANDA2");
                        
                        if (tieneSufijoTanda2)
                            return true;
                        
                        var fileInfo = new FileInfo(f);
                        var horaModificacion = fileInfo.LastWriteTime;
                        bool estaEnRangoHorario = horaModificacion.Hour >= 7 && 
                                                   (horaModificacion.Hour < 15 || (horaModificacion.Hour == 15 && horaModificacion.Minute == 0));
                        
                        return estaEnRangoHorario;
                    })
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                FileInfo archivoTanda2Info = archivosTanda2.FirstOrDefault();
                string archivoTanda2 = archivoTanda2Info?.FullName;

                if (archivoTanda2Info != null)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 2 - Archivo Tanda 2 encontrado: {archivoTanda2Info.Name} | " +
                        $"Hora creación: {archivoTanda2Info.LastWriteTime:yyyy-MM-dd HH:mm:ss} | " +
                        $"Tamaño: {archivoTanda2Info.Length} bytes",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 2");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 2 - No se encontró archivo Tanda 2 para {divisaCodigo}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 2");
                }

                // ============================================
                // PASO 3: Buscar archivos Día a Día
                // ============================================
                ServicioLog.instancia.WriteInfo(
                    $"PASO 3: Buscando archivos Día a Día para {divisaCodigo}",
                    $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 3");

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
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                FileInfo archivoDiaADiaInfo = archivosDiaADia.FirstOrDefault();
                string archivoDiaADia = archivoDiaADiaInfo?.FullName;

                if (archivoDiaADiaInfo != null)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 3 - Archivo Día a Día encontrado: {archivoDiaADiaInfo.Name} | " +
                        $"Hora creación: {archivoDiaADiaInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss} | " +
                        $"Tamaño: {archivoDiaADiaInfo.Length} bytes",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 3");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 3 - No se encontró archivo Día a Día para {divisaCodigo}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 3");
                }

                // ============================================
                // COMBINAR ARCHIVOS EN ORDEN ESPECÍFICO
                // ============================================
                ServicioLog.instancia.WriteInfo(
                    $"Iniciando combinación de archivos en orden específico para {divisaCodigo}",
                    $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Combinación");

                var lineasCombinadas = new List<string>();
                long totalImporte = 0;

                // PASO 1: Leer archivo TSD (BASE)
                if (archivoTSD != null && File.Exists(archivoTSD))
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Leyendo archivo TSD (BASE): {Path.GetFileName(archivoTSD)}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Lectura TSD");

                    var lineasTSD = File.ReadAllLines(archivoTSD, Encoding.UTF8)
                        .Select(l => l.TrimEnd('\r', '\n'))
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                    
                    int lineasValidasTSD = 0;
                    int lineasInvalidasTSD = 0;
                    long totalImporteTSD = 0;

                    foreach (var linea in lineasTSD)
                    {
                        if (linea.Length == 875)
                        {
                            lineasCombinadas.Add(linea);
                            lineasValidasTSD++;

                            try
                            {
                                if (linea.Length >= 67)
                                {
                                    string importeStr = linea.Substring(52, 15);
                                    if (long.TryParse(importeStr, out long importe))
                                    {
                                        totalImporteTSD += importe;
                                        totalImporte += importe;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                ServicioLog.instancia.WriteInfo(
                                    $"Error al extraer importe de línea TSD: {ex.Message} | Línea: {linea.Substring(0, Math.Min(50, linea.Length))}...",
                                    $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Error");
                            }
                        }
                        else
                        {
                            lineasInvalidasTSD++;
                        }
                    }

                    ServicioLog.instancia.WriteInfo(
                        $"PASO 1 COMPLETADO - Archivo TSD procesado | " +
                        $"Total líneas leídas: {lineasTSD.Count} | " +
                        $"Líneas válidas agregadas: {lineasValidasTSD} | " +
                        $"Líneas inválidas: {lineasInvalidasTSD} | " +
                        $"Total importe TSD: {totalImporteTSD:N0} | " +
                        $"Total acumulado: {totalImporte:N0}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 1 Completado");
                }

                // PASO 2: Leer archivo Día a Día (ANTES QUE TANDA 2)
                if (archivoDiaADia != null && File.Exists(archivoDiaADia))
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Leyendo archivo Día a Día (AGREGAR ABAJO): {Path.GetFileName(archivoDiaADia)}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Lectura Día a Día");

                    var lineasDiaADia = File.ReadAllLines(archivoDiaADia, Encoding.UTF8)
                        .Select(l => l.TrimEnd('\r', '\n'))
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                    
                    ServicioLog.instancia.WriteInfo(
                        $"Total líneas leídas del archivo Día a Día: {lineasDiaADia.Count}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Lectura Día a Día");
                    
                    int lineasValidasDiaADia = 0;
                    int lineasInvalidasDiaADia = 0;
                    long totalImporteDiaADia = 0;

                    foreach (var linea in lineasDiaADia)
                    {
                        // Normalizar la línea a 875 caracteres
                        string lineaNormalizada = linea;
                        
                        // Si tiene más de 875, recortar espacios finales
                        if (linea.Length > 875)
                        {
                            lineaNormalizada = linea.Substring(0, 875);
                        }
                        // Si tiene menos de 875, rellenar con espacios hasta 875
                        else if (linea.Length < 875)
                        {
                            lineaNormalizada = linea.PadRight(875);
                        }
                        
                        // Aceptar líneas que tengan al menos 100 caracteres (formato válido)
                        if (linea.Length >= 100)
                        {
                            lineasCombinadas.Add(lineaNormalizada);
                            lineasValidasDiaADia++;

                            try
                            {
                                if (lineaNormalizada.Length >= 67)
                                {
                                    string importeStr = lineaNormalizada.Substring(52, 15);
                                    if (long.TryParse(importeStr, out long importe))
                                    {
                                        totalImporteDiaADia += importe;
                                        totalImporte += importe;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                ServicioLog.instancia.WriteInfo(
                                    $"Error al extraer importe de línea Día a Día: {ex.Message} | Línea: {lineaNormalizada.Substring(0, Math.Min(50, lineaNormalizada.Length))}...",
                                    $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Error");
                            }
                        }
                        else
                        {
                            lineasInvalidasDiaADia++;
                            ServicioLog.instancia.WriteInfo(
                                $"Línea Día a Día demasiado corta (longitud: {linea.Length}, mínimo: 100) | Primeros 50: {linea.Substring(0, Math.Min(50, linea.Length))}...",
                                $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Advertencia");
                        }
                    }

                    ServicioLog.instancia.WriteInfo(
                        $"PASO 2 COMPLETADO - Archivo Día a Día procesado | " +
                        $"Total líneas leídas: {lineasDiaADia.Count} | " +
                        $"Líneas válidas agregadas: {lineasValidasDiaADia} | " +
                        $"Líneas inválidas: {lineasInvalidasDiaADia} | " +
                        $"Total importe Día a Día: {totalImporteDiaADia:N0} | " +
                        $"Total acumulado: {totalImporte:N0}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 2 Completado");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 2 - Archivo Día a Día no encontrado o no existe, continuando sin él",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 2");
                }

                // PASO 3: Leer archivo Tanda 2 (DESPUÉS DE DÍA A DÍA)
                if (archivoTanda2 != null && File.Exists(archivoTanda2))
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Leyendo archivo Tanda 2 (AGREGAR ABAJO): {Path.GetFileName(archivoTanda2)}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Lectura Tanda 2");

                    var lineasTanda2 = File.ReadAllLines(archivoTanda2, Encoding.UTF8)
                        .Select(l => l.TrimEnd('\r', '\n'))
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                    
                    ServicioLog.instancia.WriteInfo(
                        $"Total líneas leídas del archivo Tanda 2: {lineasTanda2.Count}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Lectura Tanda 2");
                    
                    int lineasValidasTanda2 = 0;
                    int lineasInvalidasTanda2 = 0;
                    long totalImporteTanda2 = 0;

                    foreach (var linea in lineasTanda2)
                    {
                        // Normalizar la línea a 875 caracteres
                        string lineaNormalizada = linea;
                        
                        // Si tiene más de 875, recortar espacios finales
                        if (linea.Length > 875)
                        {
                            lineaNormalizada = linea.Substring(0, 875);
                        }
                        // Si tiene menos de 875, rellenar con espacios hasta 875
                        else if (linea.Length < 875)
                        {
                            lineaNormalizada = linea.PadRight(875);
                        }
                        
                        // Aceptar líneas que tengan al menos 100 caracteres (formato válido)
                        if (linea.Length >= 100)
                        {
                            lineasCombinadas.Add(lineaNormalizada);
                            lineasValidasTanda2++;

                            try
                            {
                                if (lineaNormalizada.Length >= 67)
                                {
                                    string importeStr = lineaNormalizada.Substring(52, 15);
                                    if (long.TryParse(importeStr, out long importe))
                                    {
                                        totalImporteTanda2 += importe;
                                        totalImporte += importe;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                ServicioLog.instancia.WriteInfo(
                                    $"Error al extraer importe de línea Tanda 2: {ex.Message} | Línea: {lineaNormalizada.Substring(0, Math.Min(50, lineaNormalizada.Length))}...",
                                    $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Error");
                            }
                        }
                        else
                        {
                            lineasInvalidasTanda2++;
                            ServicioLog.instancia.WriteInfo(
                                $"Línea Tanda 2 demasiado corta (longitud: {linea.Length}, mínimo: 100) | Primeros 50: {linea.Substring(0, Math.Min(50, linea.Length))}...",
                                $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Advertencia");
                        }
                    }

                    ServicioLog.instancia.WriteInfo(
                        $"PASO 3 COMPLETADO - Archivo Tanda 2 procesado | " +
                        $"Total líneas leídas: {lineasTanda2.Count} | " +
                        $"Líneas válidas agregadas: {lineasValidasTanda2} | " +
                        $"Líneas inválidas: {lineasInvalidasTanda2} | " +
                        $"Total importe Tanda 2: {totalImporteTanda2:N0} | " +
                        $"Total acumulado: {totalImporte:N0}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 3 Completado");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 3 - Archivo Tanda 2 no encontrado o no existe, continuando sin él",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Paso 3");
                }

                // Verificar si hay archivos para combinar
                if (lineasCombinadas.Count == 0)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"No se encontraron archivos para combinar para {divisaCodigo} | No hay líneas válidas",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Error");
                    return;
                }

                // ============================================
                // GENERAR ARCHIVO COMBINADO
                // ============================================
                long totalDelNombre = 0;
                
                if (archivoTSDInfo != null)
                {
                    long numeroTSD = ExtraerNumeroDelNombreArchivo(archivoTSDInfo.Name, divisaCodigo);
                    totalDelNombre += numeroTSD;
                }
                
                if (archivoTanda2Info != null)
                {
                    long numeroTanda2 = ExtraerNumeroDelNombreArchivo(archivoTanda2Info.Name, divisaCodigo);
                    totalDelNombre += numeroTanda2;
                }
                
                if (archivoDiaADiaInfo != null)
                {
                    long numeroDiaADia = ExtraerNumeroDelNombreArchivo(archivoDiaADiaInfo.Name, divisaCodigo);
                    totalDelNombre += numeroDiaADia;
                }

                ServicioLog.instancia.WriteInfo(
                    $"Generando archivo combinado para {divisaCodigo} | " +
                    $"Total líneas: {lineasCombinadas.Count} | " +
                    $"Total del nombre (suma de archivos): {totalDelNombre:N0}",
                    $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Generación");

                string timestamp = DateTime.Now.ToString("d - M-yyyy");
                string suctecni = ciudad == "MONTEVIDEO" ? "Mont" : "Mald";
                string fileName = $"{timestamp}-{divisaCodigo}{totalDelNombre}-" +
                                  $"AcreditacionTecnisegur{suctecni}.txt";

                string rutaDestino = Path.Combine(folderPath, fileName);

                // Escribir archivo combinado en UTF-8 sin BOM
                var utf8SinBom = new System.Text.UTF8Encoding(false);
                File.WriteAllLines(rutaDestino, lineasCombinadas, utf8SinBom);

                ServicioLog.instancia.WriteInfo(
                    $"ARCHIVO COMBINADO GENERADO EXITOSAMENTE | " +
                    $"Divisa: {divisaCodigo} | " +
                    $"Ruta: {rutaDestino} | " +
                    $"Total líneas escritas: {lineasCombinadas.Count} | " +
                    $"Total en nombre (suma de archivos): {totalDelNombre:N0} | " +
                    $"Tamaño archivo: {new FileInfo(rutaDestino).Length} bytes",
                    $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Éxito");
            }
            catch (Exception ex)
            {
                ServicioLog.instancia.WriteLog(ex, "Scotiabank", $"Combinar TXT {ciudad} {divisaCodigo}");
                throw;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Extrae el número del nombre del archivo
        /// Formato esperado: {timestamp}-{divisa}{numero}-Acreditacion...
        /// </summary>
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
            catch (Exception ex)
            {
                ServicioLog.instancia.WriteInfo(
                    $"Error al extraer número del nombre de archivo: {nombreArchivo} | Error: {ex.Message}",
                    "SCOTIABANK | ServicioCombinarTxtScotiabank | Extracción");
            }
            
            return 0;
        }
    }
}

