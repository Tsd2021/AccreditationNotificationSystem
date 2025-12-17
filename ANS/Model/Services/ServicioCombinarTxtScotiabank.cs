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
        /// Estructura para retornar los archivos encontrados
        /// </summary>
        private class ResultadoBusquedaArchivos
        {
            public FileInfo ArchivoTSD { get; set; }
            public FileInfo ArchivoTanda2 { get; set; }
            public FileInfo ArchivoDiaADia { get; set; }
        }

        /// <summary>
        /// Busca los 3 archivos necesarios: TSD, Tanda2 y DiaADia
        /// </summary>
        private static ResultadoBusquedaArchivos BuscarArchivosNecesarios(string folderPath, string divisaCodigo, string ciudad)
        {
            var resultado = new ResultadoBusquedaArchivos();

            // Buscar archivo TSD (AcreditacionTecnisegur sin "Buzones")
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

            resultado.ArchivoTSD = archivosTSD.FirstOrDefault();

            // Buscar archivo Tanda 2
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

            resultado.ArchivoTanda2 = archivosTanda2.FirstOrDefault();

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
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            resultado.ArchivoDiaADia = archivosDiaADia.FirstOrDefault();

            return resultado;
        }

        /// <summary>
        /// Combina archivos TXT de una divisa específica para una ciudad
        /// Con lógica de reintentos: intenta cada 3 minutos por 1 hora hasta encontrar los 3 archivos necesarios
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
                // LÓGICA DE REINTENTOS: Intentar cada 3 minutos por 1 hora
                // ============================================
                DateTime inicioIntento = DateTime.Now;
                TimeSpan tiempoMaximoEspera = TimeSpan.FromHours(1);
                TimeSpan intervaloReintento = TimeSpan.FromMinutes(3);
                int intento = 0;
                bool archivosCompletos = false;

                FileInfo archivoTSDInfo = null;
                FileInfo archivoTanda2Info = null;
                FileInfo archivoDiaADiaInfo = null;

                while (!archivosCompletos && (DateTime.Now - inicioIntento) < tiempoMaximoEspera)
                {
                    intento++;
                    ServicioLog.instancia.WriteInfo(
                        $"Intento {intento} - Buscando archivos necesarios para {divisaCodigo} | " +
                        $"Tiempo transcurrido: {(DateTime.Now - inicioIntento).TotalMinutes:F1} minutos",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Reintento {intento}");

                    // Buscar los 3 archivos necesarios
                    var resultadoBusqueda = BuscarArchivosNecesarios(folderPath, divisaCodigo, ciudad);
                    archivoTSDInfo = resultadoBusqueda.ArchivoTSD;
                    archivoTanda2Info = resultadoBusqueda.ArchivoTanda2;
                    archivoDiaADiaInfo = resultadoBusqueda.ArchivoDiaADia;

                    // ✅ Verificar si se encontró el archivo TSD (OBLIGATORIO)
                    // Tanda2 y DiaADia son opcionales si TSD existe
                    archivosCompletos = archivoTSDInfo != null;

                    if (archivosCompletos)
                    {
                        var archivosEncontrados = new List<string> { $"TSD: {archivoTSDInfo.Name}" };
                        if (archivoTanda2Info != null) archivosEncontrados.Add($"Tanda2: {archivoTanda2Info.Name}");
                        if (archivoDiaADiaInfo != null) archivosEncontrados.Add($"DiaADia: {archivoDiaADiaInfo.Name}");
                        
                        var archivosOpcionalesFaltantes = new List<string>();
                        if (archivoTanda2Info == null) archivosOpcionalesFaltantes.Add("Tanda2");
                        if (archivoDiaADiaInfo == null) archivosOpcionalesFaltantes.Add("DiaADia");

                        if (archivosOpcionalesFaltantes.Any())
                        {
                            ServicioLog.instancia.WriteInfo(
                                $"✅ Archivo TSD encontrado en intento {intento} | " +
                                $"Archivos encontrados: {string.Join(" | ", archivosEncontrados)} | " +
                                $"Archivos opcionales faltantes (se combinará sin ellos): {string.Join(", ", archivosOpcionalesFaltantes)}",
                                $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Archivos Suficientes");
                        }
                        else
                        {
                            ServicioLog.instancia.WriteInfo(
                                $"✅ TODOS LOS ARCHIVOS ENCONTRADOS en intento {intento} | " +
                                $"TSD: {archivoTSDInfo.Name} | " +
                                $"Tanda2: {archivoTanda2Info.Name} | " +
                                $"DiaADia: {archivoDiaADiaInfo.Name}",
                                $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Archivos Completos");
                        }
                        break; // Salir del loop, proceder a combinar
                    }
                    else
                    {
                        var archivosFaltantes = new List<string>();
                        if (archivoTSDInfo == null) archivosFaltantes.Add("TSD (OBLIGATORIO)");
                        if (archivoTanda2Info == null) archivosFaltantes.Add("Tanda2 (opcional)");
                        if (archivoDiaADiaInfo == null) archivosFaltantes.Add("DiaADia (opcional)");

                        ServicioLog.instancia.WriteInfo(
                            $"⚠️ Archivo TSD faltante en intento {intento} | " +
                            $"Archivos faltantes: {string.Join(", ", archivosFaltantes)} | " +
                            $"Esperando {intervaloReintento.TotalMinutes} minutos antes del siguiente intento...",
                            $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Archivos Faltantes");

                        // Esperar antes del siguiente intento (solo si no se alcanzó el tiempo máximo)
                        if ((DateTime.Now - inicioIntento) < tiempoMaximoEspera)
                        {
                            await Task.Delay(intervaloReintento);
                        }
                    }
                }

                // ✅ Validación final: TSD es obligatorio, Tanda2 y DiaADia son opcionales
                if (archivoTSDInfo == null)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"❌ NO SE ENCONTRÓ EL ARCHIVO TSD (OBLIGATORIO) después de {intento} intentos y {tiempoMaximoEspera.TotalMinutes} minutos | " +
                        $"NO se generará archivo combinado",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Error - TSD Faltante");
                    return; // Salir sin generar archivo combinado
                }

                // ✅ Si TSD existe, proceder incluso si faltan Tanda2 o DiaADia
                var archivosOpcionalesFinales = new List<string>();
                if (archivoTanda2Info == null) archivosOpcionalesFinales.Add("Tanda2");
                if (archivoDiaADiaInfo == null) archivosOpcionalesFinales.Add("DiaADia");
                
                if (archivosOpcionalesFinales.Any())
                {
                    ServicioLog.instancia.WriteInfo(
                        $"✅ Archivo TSD encontrado. Procediendo con combinación aunque falten archivos opcionales: {string.Join(", ", archivosOpcionalesFinales)}",
                        $"SCOTIABANK | ServicioCombinarTxtScotiabank | {ciudad} | Continuando sin Archivos Opcionales");
                }

                // Los archivos ya fueron encontrados en el loop de reintentos
                // Continuar con la combinación usando los archivos encontrados
                string archivoTSD = archivoTSDInfo?.FullName;
                string archivoTanda2 = archivoTanda2Info?.FullName;
                string archivoDiaADia = archivoDiaADiaInfo?.FullName;

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

