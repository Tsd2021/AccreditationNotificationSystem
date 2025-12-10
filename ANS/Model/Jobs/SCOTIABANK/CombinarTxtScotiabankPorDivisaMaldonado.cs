using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.SCOTIABANK
{
    [DisallowConcurrentExecution]
    public class CombinarTxtScotiabankPorDivisaMaldonado : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            string jobName = context.JobDetail.Key.Name;
            string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
            DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;

            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC",
                $"SCOTIABANK | Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");

            Exception e = null;

            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MainWindow main = (MainWindow)Application.Current.MainWindow;
                    main.MostrarAviso("Combinando TXT Scotiabank Maldonado por divisa", Color.FromRgb(255, 102, 102));
                });

                // Procesar UYU y USD
                await CombinarArchivosPorDivisa("UYU", "00");
                await CombinarArchivosPorDivisa("USD", "01");

                ServicioLog.instancia.WriteInfo(
                    "Job completado exitosamente | Archivos combinados para UYU y USD",
                    $"SCOTIABANK | Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");
            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al combinar TXT de Scotiabank Maldonado: {ex.Message}");
                ServicioLog.instancia.WriteLog(ex, "Scotiabank", "Combinar TXT Maldonado por divisa");
            }
            finally
            {
                Mensaje mensaje = new Mensaje
                {
                    Color = Color.FromRgb(255, 102, 102),
                    Banco = "Scotiabank",
                    Tipo = "Combinar TXT Maldonado por divisa",
                    Icon = PackIconKind.Bank,
                    Estado = e != null ? "Error" : "Success"
                };

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MainWindow main = (MainWindow)Application.Current.MainWindow;
                    VMmainWindow vm = main.DataContext as VMmainWindow;

                    if (vm == null)
                    {
                        vm = new VMmainWindow();
                        main.DataContext = vm;
                    }

                    if (e != null)
                    {
                        main.MostrarAviso("Error Job Combinar TXT Scotiabank Maldonado", Colors.Red);
                    }
                    else
                    {
                        main.MostrarAviso("Success Job Combinar TXT Scotiabank Maldonado", Colors.Green);
                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);
                    vm.CargarMensajes();
                });
            }

            await Task.CompletedTask;
        }

        private async Task CombinarArchivosPorDivisa(string divisaCodigo, string monedaCodigo)
        {
            try
            {
                // En PRODUCCIÓN: Leer de la carpeta donde ANS genera los archivos (MALDONADO)
                // Busca: TSD (archivos sin "BuzonesTecnisegur"), Tanda2 (con sufijo _Tanda2), DiaADia (con sufijo _DiaADia)
                // NO incluye Tanda1 en la combinación
                // En TEST: El test manual usa COMBINATION FILES TEST
                string ciudad = "MALDONADO";
                string basePath = ConfiguracionGlobal.Rutas.ScotiabankMaldonado;
                string folderName = DateTime.Now.ToString("yyyy-MM-dd");
                string folderPath = Path.Combine(basePath, folderName);

                ServicioLog.instancia.WriteInfo(
                    $"Iniciando combinación de archivos para {divisaCodigo} | Carpeta: {folderPath}",
                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Inicio");

                // Crear carpeta si no existe
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    ServicioLog.instancia.WriteInfo(
                        $"Carpeta creada: {folderPath}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Creación Carpeta");
                }

                // ============================================
                // PASO 1: Buscar archivo TSD más reciente (creado más tarde)
                // ============================================
                ServicioLog.instancia.WriteInfo(
                    $"PASO 1: Buscando archivo TSD más reciente para {divisaCodigo}",
                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 1");

                var archivosTSD = Directory.GetFiles(folderPath, $"*{divisaCodigo}*.txt", SearchOption.TopDirectoryOnly)
                    .Where(f => 
                    {
                        string fileName = Path.GetFileName(f).ToUpper();
                        // Los archivos de TSD tienen "ACREDITACIONTECNISEGUR" pero NO "BUZONES"
                        // Excluir archivos de nuestra app y archivos combinados
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
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 1");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 1 - No se encontró archivo TSD para {divisaCodigo}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 1");
                }

                // ============================================
                // PASO 2: Buscar archivos creados después de 7am pero antes de 15:00 (Tanda 2)
                // ============================================
                ServicioLog.instancia.WriteInfo(
                    $"PASO 2: Buscando archivos Tanda 2 (entre 7:00 y 15:00) para {divisaCodigo}",
                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 2");

                var archivosTanda2 = Directory.GetFiles(folderPath, $"*{divisaCodigo}*.txt", SearchOption.TopDirectoryOnly)
                    .Where(f =>
                    {
                        string fileName = Path.GetFileName(f).ToUpper();
                        // Buscar SOLO archivos con sufijo _Tanda2 (excluir explícitamente Tanda1)
                        if (!fileName.Contains("ACREDITACIONBUZONESTECNISEGUR") || 
                            fileName.Contains("COMBINADO") ||
                            fileName.Contains("CASHOFFICE") ||
                            fileName.Contains("_TANDA1") ||  // Excluir explícitamente Tanda1
                            fileName.Contains("_DIAADIA"))    // Excluir DiaADia (se busca en otro paso)
                            return false;

                        // SOLO aceptar archivos con sufijo _Tanda2
                        // También aceptar archivos sin sufijo si están en el rango horario (7:00-15:00) 
                        // para compatibilidad con archivos antiguos, pero excluyendo Tanda1
                        bool tieneSufijoTanda2 = fileName.Contains("_TANDA2");
                        
                        if (tieneSufijoTanda2)
                            return true; // Si tiene _Tanda2, aceptarlo directamente
                        
                        // Si no tiene sufijo, verificar rango horario (compatibilidad con archivos antiguos)
                        var fileInfo = new FileInfo(f);
                        var horaModificacion = fileInfo.LastWriteTime;
                        bool estaEnRangoHorario = horaModificacion.Hour >= 7 && 
                                                   (horaModificacion.Hour < 15 || (horaModificacion.Hour == 15 && horaModificacion.Minute == 0));
                        
                        return estaEnRangoHorario; // Solo si está en el rango horario y no tiene sufijos
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
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 2");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 2 - No se encontró archivo Tanda 2 para {divisaCodigo}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 2");
                }

                // ============================================
                // PASO 3: Buscar archivos Día a Día
                // ============================================
                ServicioLog.instancia.WriteInfo(
                    $"PASO 3: Buscando archivos Día a Día para {divisaCodigo}",
                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 3");

                var archivosDiaADia = Directory.GetFiles(folderPath, $"*{divisaCodigo}*.txt", SearchOption.TopDirectoryOnly)
                    .Where(f =>
                    {
                        string fileName = Path.GetFileName(f).ToUpper();
                        // Buscar archivos que contengan AcreditacionBuzonesTecnisegur y _DiaADia en el nombre
                        if (!fileName.Contains("ACREDITACIONBUZONESTECNISEGUR") || 
                            fileName.Contains("COMBINADO") ||
                            fileName.Contains("CASHOFFICE"))
                            return false;

                        // Priorizar archivos con sufijo _DiaADia, pero también aceptar los sin sufijo si están después de las 16:00
                        bool tieneSufijoDiaADia = fileName.Contains("_DIAADIA");
                        
                        var fileInfo = new FileInfo(f);
                        var horaModificacion = fileInfo.LastWriteTime;
                        bool estaDespuesDe16 = horaModificacion.Hour >= 16;
                        
                        // Si tiene el sufijo _DiaADia, lo aceptamos directamente
                        // Si no tiene sufijo pero está después de las 16:00, también lo aceptamos (compatibilidad con archivos antiguos)
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
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 3");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 3 - No se encontró archivo Día a Día para {divisaCodigo}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 3");
                }

                // ============================================
                // COMBINAR ARCHIVOS EN ORDEN ESPECÍFICO
                // ============================================
                ServicioLog.instancia.WriteInfo(
                    $"Iniciando combinación de archivos en orden específico para {divisaCodigo}",
                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Combinación");

                var lineasCombinadas = new List<string>();
                long totalImporte = 0;

                // PASO 1: Leer archivo TSD (más reciente) - BASE
                if (archivoTSD != null && File.Exists(archivoTSD))
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Leyendo archivo TSD (BASE): {Path.GetFileName(archivoTSD)}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Lectura TSD");

                    var lineasTSD = File.ReadAllLines(archivoTSD, Encoding.UTF8)
                        .Select(l => l.TrimEnd('\r', '\n')) // Eliminar caracteres de fin de línea
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                    
                    int lineasValidasTSD = 0;
                    int lineasInvalidasTSD = 0;
                    long totalImporteTSD = 0;

                    foreach (var linea in lineasTSD)
                    {
                        // La línea debe tener exactamente 875 caracteres
                        if (linea.Length == 875)
                        {
                            lineasCombinadas.Add(linea);
                            lineasValidasTSD++;

                            // Extraer importe: posición 52-66 (15 dígitos)
                            // Formato: 0-17: rutOrdenante (18), 18-20: espacios (3), 21-22: tipoOperativa (2), 
                            // 23-30: fecha (8), 31-51: espacios (21), 52-66: importe (15)
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
                                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Error");
                            }
                        }
                        else
                        {
                            lineasInvalidasTSD++;
                            if (lineasInvalidasTSD <= 3) // Log solo las primeras 3 inválidas
                            {
                                ServicioLog.instancia.WriteInfo(
                                    $"Línea TSD inválida (longitud: {linea.Length}, esperado: 875) | Primeros 50 caracteres: {linea.Substring(0, Math.Min(50, linea.Length))}...",
                                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Advertencia");
                            }
                        }
                    }

                    ServicioLog.instancia.WriteInfo(
                        $"PASO 1 COMPLETADO - Archivo TSD procesado | " +
                        $"Total líneas leídas: {lineasTSD.Count} | " +
                        $"Líneas válidas agregadas: {lineasValidasTSD} | " +
                        $"Líneas inválidas: {lineasInvalidasTSD} | " +
                        $"Total importe TSD: {totalImporteTSD:N0} | " +
                        $"Total acumulado: {totalImporte:N0}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 1 Completado");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 1 - Archivo TSD no encontrado o no existe, continuando sin él",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 1");
                }

                // PASO 2: Leer archivo Tanda 2 (después de 7am, antes de 15:00) - AGREGAR ABAJO
                if (archivoTanda2 != null && File.Exists(archivoTanda2))
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Leyendo archivo Tanda 2 (AGREGAR ABAJO): {Path.GetFileName(archivoTanda2)}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Lectura Tanda 2");

                    var lineasTanda2 = File.ReadAllLines(archivoTanda2, Encoding.UTF8)
                        .Select(l => l.TrimEnd('\r', '\n')) // Eliminar caracteres de fin de línea
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                    
                    int lineasValidasTanda2 = 0;
                    int lineasInvalidasTanda2 = 0;
                    long totalImporteTanda2 = 0;

                    foreach (var linea in lineasTanda2)
                    {
                        if (linea.Length == 876)
                        {
                            lineasCombinadas.Add(linea);
                            lineasValidasTanda2++;

                            try
                            {
                                if (linea.Length >= 67)
                                {
                                    string importeStr = linea.Substring(52, 15);
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
                                    $"Error al extraer importe de línea Tanda 2: {ex.Message} | Línea: {linea.Substring(0, Math.Min(50, linea.Length))}...",
                                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Error");
                            }
                        }
                        else
                        {
                            lineasInvalidasTanda2++;
                            if (lineasInvalidasTanda2 <= 3)
                            {
                                ServicioLog.instancia.WriteInfo(
                                    $"Línea Tanda 2 inválida (longitud: {linea.Length}, esperado: 875) | Primeros 50 caracteres: {linea.Substring(0, Math.Min(50, linea.Length))}...",
                                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Advertencia");
                            }
                        }
                    }

                    ServicioLog.instancia.WriteInfo(
                        $"PASO 2 COMPLETADO - Archivo Tanda 2 procesado | " +
                        $"Total líneas leídas: {lineasTanda2.Count} | " +
                        $"Líneas válidas agregadas: {lineasValidasTanda2} | " +
                        $"Líneas inválidas: {lineasInvalidasTanda2} | " +
                        $"Total importe Tanda 2: {totalImporteTanda2:N0} | " +
                        $"Total acumulado: {totalImporte:N0}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 2 Completado");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 2 - Archivo Tanda 2 no encontrado o no existe, continuando sin él",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 2");
                }

                // PASO 3: Leer archivo Día a Día - AGREGAR ABAJO
                if (archivoDiaADia != null && File.Exists(archivoDiaADia))
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Leyendo archivo Día a Día (AGREGAR ABAJO): {Path.GetFileName(archivoDiaADia)}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Lectura Día a Día");

                    var lineasDiaADia = File.ReadAllLines(archivoDiaADia, Encoding.UTF8)
                        .Select(l => l.TrimEnd('\r', '\n')) // Eliminar caracteres de fin de línea
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                    
                    int lineasValidasDiaADia = 0;
                    int lineasInvalidasDiaADia = 0;
                    long totalImporteDiaADia = 0;

                    foreach (var linea in lineasDiaADia)
                    {
                        if (linea.Length == 876)
                        {
                            lineasCombinadas.Add(linea);
                            lineasValidasDiaADia++;

                            try
                            {
                                if (linea.Length >= 67)
                                {
                                    string importeStr = linea.Substring(52, 15);
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
                                    $"Error al extraer importe de línea Día a Día: {ex.Message} | Línea: {linea.Substring(0, Math.Min(50, linea.Length))}...",
                                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Error");
                            }
                        }
                        else
                        {
                            lineasInvalidasDiaADia++;
                            if (lineasInvalidasDiaADia <= 3)
                            {
                                ServicioLog.instancia.WriteInfo(
                                    $"Línea Día a Día inválida (longitud: {linea.Length}, esperado: 875) | Primeros 50 caracteres: {linea.Substring(0, Math.Min(50, linea.Length))}...",
                                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Advertencia");
                            }
                        }
                    }

                    ServicioLog.instancia.WriteInfo(
                        $"PASO 3 COMPLETADO - Archivo Día a Día procesado | " +
                        $"Total líneas leídas: {lineasDiaADia.Count} | " +
                        $"Líneas válidas agregadas: {lineasValidasDiaADia} | " +
                        $"Líneas inválidas: {lineasInvalidasDiaADia} | " +
                        $"Total importe Día a Día: {totalImporteDiaADia:N0} | " +
                        $"Total acumulado: {totalImporte:N0}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 3 Completado");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"PASO 3 - Archivo Día a Día no encontrado o no existe, continuando sin él",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Paso 3");
                }

                // Verificar si hay archivos para combinar
                if (lineasCombinadas.Count == 0)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"No se encontraron archivos para combinar para {divisaCodigo} | No hay líneas válidas",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Error");
                    return;
                }

                // ============================================
                // GENERAR ARCHIVO COMBINADO
                // ============================================
                // Extraer números de los nombres de los archivos originales y sumarlos
                long totalDelNombre = 0;
                
                if (archivoTSDInfo != null)
                {
                    long numeroTSD = ExtraerNumeroDelNombreArchivo(archivoTSDInfo.Name, divisaCodigo);
                    totalDelNombre += numeroTSD;
                    ServicioLog.instancia.WriteInfo(
                        $"Número extraído de TSD: {numeroTSD:N0} | Archivo: {archivoTSDInfo.Name}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Extracción");
                }
                
                if (archivoTanda2Info != null)
                {
                    long numeroTanda2 = ExtraerNumeroDelNombreArchivo(archivoTanda2Info.Name, divisaCodigo);
                    totalDelNombre += numeroTanda2;
                    ServicioLog.instancia.WriteInfo(
                        $"Número extraído de Tanda 2: {numeroTanda2:N0} | Archivo: {archivoTanda2Info.Name}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Extracción");
                }
                
                if (archivoDiaADiaInfo != null)
                {
                    long numeroDiaADia = ExtraerNumeroDelNombreArchivo(archivoDiaADiaInfo.Name, divisaCodigo);
                    totalDelNombre += numeroDiaADia;
                    ServicioLog.instancia.WriteInfo(
                        $"Número extraído de Día a Día: {numeroDiaADia:N0} | Archivo: {archivoDiaADiaInfo.Name}",
                        "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Extracción");
                }

                ServicioLog.instancia.WriteInfo(
                    $"Generando archivo combinado para {divisaCodigo} | " +
                    $"Total líneas: {lineasCombinadas.Count} | " +
                    $"Total del nombre (suma de archivos): {totalDelNombre:N0}",
                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Generación");

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
                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Éxito");
            }
            catch (Exception ex)
            {
                ServicioLog.instancia.WriteLog(ex, "Scotiabank", $"Combinar TXT Maldonado {divisaCodigo}");
                throw;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Extrae el número del nombre del archivo
        /// Formato esperado: {timestamp}-{divisa}{numero}-Acreditacion...
        /// Ejemplo: "26-11-2025-14-50-UYU3295000-AcreditacionTecnisegurMald.txt"
        /// </summary>
        private static long ExtraerNumeroDelNombreArchivo(string nombreArchivo, string divisaCodigo)
        {
            try
            {
                // Buscar el patrón: {divisa}{numero}-
                // Ejemplo: "UYU3295000-" o "USD1234567-"
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
                    "SCOTIABANK | CombinarTxtScotiabankPorDivisaMaldonado | Extracción");
            }
            
            return 0;
        }
    }
}

