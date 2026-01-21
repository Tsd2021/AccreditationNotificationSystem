
using Microsoft.Data.SqlClient;
using SharedDTOs;
using System.Data;
using System.IO;

namespace ANS.Model.Services
{


    public class ServicioEnvioMasivo
    {
        // ✅ Thread-safe: Lazy<T> garantiza que solo se crea una instancia, incluso en entornos multi-threaded
        // LazyThreadSafetyMode.ExecutionAndPublication es el modo por defecto y es thread-safe
        private static readonly Lazy<ServicioEnvioMasivo> _lazy =
            new Lazy<ServicioEnvioMasivo>(() => new ServicioEnvioMasivo());

        public static ServicioEnvioMasivo instancia => _lazy.Value;

        public ServicioEmail _emailService { get; set; } = ServicioEmail.getInstancia();

        public static ServicioEnvioMasivo getInstancia()
        {
            return _lazy.Value;
        }
        public async Task procesarEnvioMasivo(int numEnvioMasivo)
        {
            // ✅ Logging informativo: Inicio del proceso
            ServicioLog.instancia.WriteInfo($"Iniciando procesamiento de envío masivo {numEnvioMasivo}",
                $"NumEnvioMasivo: {numEnvioMasivo}");

            try
            {
                List<BuzonDTO> buzones = await getBuzonesByNumeroEnvioMasivo(numEnvioMasivo);

                ServicioLog.instancia.WriteInfo($"Buzones encontrados: {buzones.Count}",
                    $"NumEnvioMasivo: {numEnvioMasivo}");

                await hidratarDTOconSusAcreditaciones(buzones, numEnvioMasivo);

                var buzonesConAcreditaciones = buzones
                    .Where(b => b.Acreditaciones != null && b.Acreditaciones.Count > 0)
                    .ToList();

                var buzonesSinAcreditaciones = buzones
                    .Where(b => b.Acreditaciones == null || b.Acreditaciones.Count == 0)
                    .ToList();

                if (buzonesConAcreditaciones.Count == 0 && buzonesSinAcreditaciones.Count == 0)
                {
                    ServicioLog.instancia.WriteInfo($"No hay buzones para procesar", $"NumEnvioMasivo: {numEnvioMasivo}");
                    return;
                }

                ServicioLog.instancia.WriteInfo($"Buzones con acreditaciones: {buzonesConAcreditaciones.Count}", $"NumEnvioMasivo: {numEnvioMasivo}");
                ServicioLog.instancia.WriteInfo($"Buzones sin acreditaciones: {buzonesSinAcreditaciones.Count}", $"NumEnvioMasivo: {numEnvioMasivo}");

                await obtenerUsuarioYFechaDelDeposito(buzones);

                await obtenerFechaUltimaConexionDelBuzon(buzones);

                var svcSuc = ServicioSucursalesClientes.getInstancia();

                // (opcional, recomendado) limpiar para evitar duplicados si ya se cargó antes
                svcSuc.listaSucursalCliente.Clear();

                // cargar desde BD
                svcSuc.CargarSucursalesCliente();

                // mapear a DTO compartido
                var sucursalesDtos = svcSuc.listaSucursalCliente
                    .Select(x => new SucursalClienteDto
                    {
                        NC = x.NC,
                        Empresa = x.Empresa,
                        IdCliente = x.IdCliente,
                        Sucursal = x.Sucursal
                    })
                    .ToList();

                // inyectar en ReportService
                var reportService = new TAAS.Reports.ReportService(sucursalesDtos);

                ObtenerMailsPorBuzon(buzones);

                var semaphore = new SemaphoreSlim(initialCount: 20, maxCount: 20);

                var smtp = await ServicioEmail.instancia.getNewSmptClient();

                var sendLock = new SemaphoreSlim(1, 1);

                var tasksCon = buzonesConAcreditaciones.Select(async b =>
                {
                    // preparar excelStream, subject, body, fileName, destino…
                    b.MontoTotal = b.Acreditaciones.Sum(a => a.Monto);

                    var b2 = new BuzonDTO2
                    {
                        NC = b.NC,
                        NN = b.NN,
                        Empresa = b.Empresa,
                        FechaInicio = b.FechaInicio,
                        Cierre = b.Cierre,
                        MontoTotal = b.MontoTotal,
                        Moneda = b.Moneda,
                        Divisa = b.Divisa,
                        IdOperacion = b.IdOperacion,
                        Sucursal = b.Sucursal,
                        IdOperacionFinal = b.IdOperacionFinal,
                        IdOperacionInicio = b.IdOperacionInicio,
                        NumeroEnvioMasivo = b.NumeroEnvioMasivo,
                        UltimaFechaConexion = b.UltimaFechaConexion,
                        EsHenderson = b.EsHenderson,
                        NombreWS = b.NombreWS,
                        IdCliente = b.IdCliente,
                        // mapea Acreditaciones:
                        Acreditaciones = b.Acreditaciones.Select(a => new AcreditacionDTO2
                        {
                            NC = a.NC,
                            IdOperacion = a.IdOperacion,
                            Divisa = a.Divisa,
                            Monto = a.Monto,
                            Usuario = a.Usuario,
                            FechaDep = a.FechaDep,
                            Empresa = a.Empresa
                        }).ToList()
                    };


                    Stream excelStream;
                    string subject, body, fileName;

                    excelStream = b2.IdCliente switch
                    {
                        179 => reportService.ArmarExcelMasivoParaCoboe(b2, out subject, out body, out fileName),
                        _ => reportService.ArmarExcelConReportViewer(b2, out subject, out body, out fileName)
                    };


                    await semaphore.WaitAsync();

                    try
                    {
                        // sólo UNO a la vez entra aquí:
                        await sendLock.WaitAsync();
                        try
                        {

                            await ServicioEmail.instancia
                                .EnviarExcelPorMailMasivoConMailKit(
                                   excelStream, fileName, subject, body, b._Emails, smtp);

                        }
                        catch (Exception ex)
                        {
                            // ✅ Logging mejorado: Registra error con contexto completo
                            ServicioLog.instancia.WriteLog(ex, "Todos", $"Envío Masivo {b.NumeroEnvioMasivo} - Buzón: {b.NC}");
                        }

                        finally
                        {

                            sendLock.Release();

                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToArray();

                // Buzones sin acreditaciones: enviar mail con mensaje fijo, sin adjunto (solo si tienen destinos)
                var tasksSin = buzonesSinAcreditaciones
                    .Where(b => b._Emails != null && b._Emails.Count > 0)
                    .Select(async b =>
                    {
                        // Calcular fechaRango igual que en ReportService
                        DateTime hoy = DateTime.Today;
                        DateTime fechaInicio;
                        DateTime fechaCierre;

                        if (b.EsHenderson && b.NumeroEnvioMasivo == 1)
                        {
                            int diasARestar = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;
                            var baseDate = hoy.AddDays(-diasARestar);
                            fechaInicio = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, 14, 30, 0);
                            fechaCierre = new DateTime(hoy.Year, hoy.Month, hoy.Day, 7, 0, 0);
                        }
                        else
                        {
                            var horaCierreDto = b.Cierre.TimeOfDay;
                            fechaCierre = new DateTime(hoy.Year, hoy.Month, hoy.Day, horaCierreDto.Hours, horaCierreDto.Minutes, 0);

                            if (!b.EsHenderson)
                            {
                                int diasARestar = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;
                                var baseDate = hoy.AddDays(-diasARestar);
                                var t = b.Cierre.TimeOfDay;
                                fechaInicio = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, t.Hours, t.Minutes, 0);
                            }
                            else
                            {
                                switch (b.NumeroEnvioMasivo)
                                {
                                    case 2:
                                        fechaInicio = new DateTime(hoy.Year, hoy.Month, hoy.Day, 7, 0, 0);
                                        break;
                                    default:
                                        fechaInicio = new DateTime(b.Cierre.Year, b.Cierre.Month, b.Cierre.Day, b.Cierre.Hour, b.Cierre.Minute, 0);
                                        break;
                                }
                            }
                        }

                        string inicioStr = fechaInicio.ToString("dd/MM/yyyy HH:mm");
                        string cierreStr = fechaCierre.ToString("dd/MM/yyyy HH:mm");
                        string fechaRango = $"DEL {inicioStr} AL {cierreStr}";

                        string subjectSin = $"Acreditaciones Buzón Inteligente [{b.NN}] - {cierreStr}";
                        string bodySin = $"Acreditaciones del Buzón Inteligente {b.NN} del <strong>{fechaRango}</strong>";

                        // Agregar información de última conexión del buzón si está disponible
                        if (b.UltimaFechaConexion != DateTime.MinValue)
                        {
                            string fechaUltimaConexionStr = b.UltimaFechaConexion.ToString("dd/MM/yyyy HH:mm");
                            bodySin += $"<br/><br/>Por favor, tener en cuenta fecha y hora de última conexión del buzón registrada a las: <strong>{fechaUltimaConexionStr}</strong>";
                        }
                        await semaphore.WaitAsync();
                        try
                        {
                            await sendLock.WaitAsync();
                            try
                            {
                                await ServicioEmail.instancia.EnviarExcelPorMailMasivoConMailKit(
                                    excelStream: null, fileName: null, subjectSin, bodySin, b._Emails, smtp);
                            }
                            catch (Exception ex)
                            {
                                ServicioLog.instancia.WriteLog(ex, "Todos", $"Envío Masivo (sin acreditaciones) {b.NumeroEnvioMasivo} - Buzón: {b.NC}");
                            }
                            finally
                            {
                                sendLock.Release();
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }).ToArray();

                var tasks = tasksCon.Concat(tasksSin).ToArray();
                await Task.WhenAll(tasks);

                await smtp.DisconnectAsync(true);

                // ✅ Logging informativo: Fin exitoso del proceso
                ServicioLog.instancia.WriteInfo(
                    $"Procesamiento de envío masivo {numEnvioMasivo} completado exitosamente",
                    $"NumEnvioMasivo: {numEnvioMasivo} | Buzones con acreditaciones: {buzonesConAcreditaciones.Count} | Buzones sin acreditaciones: {buzonesSinAcreditaciones.Count}");
            }
            catch (Exception e)
            {
                // ✅ Logging mejorado: Registra error completo con contexto
                ServicioLog.instancia.WriteLog(e, "Todos", $"Envío Masivo {numEnvioMasivo}");
                throw; // Re-lanzar para que el caller pueda manejarlo
            }
        }

        private void ObtenerMailsPorBuzon(List<BuzonDTO> buzonesConAcreditaciones)
        {
            foreach (var b in buzonesConAcreditaciones)
            {
                foreach (var e in ServicioCC.getInstancia().getBuzones())
                {
                    // ✅ Normalizar NC con TRIM para evitar problemas de espacios en blanco
                    string ncBuzon = b.NC?.Trim() ?? b.NC;
                    string ncServicioCC = e.NC?.Trim() ?? e.NC;

                    if (ncBuzon == ncServicioCC)
                    {
                        b._Emails = e._listaEmails;
                        break; // ✅ Optimización: salir del loop interno al encontrar coincidencia
                    }
                }
            }
        }

        public async Task hidratarDTOconSusAcreditaciones(List<BuzonDTO> deps, int numEnvioMasivo)
        {
            if (deps == null || deps.Count == 0) return;


            // ✅ Normalizar NC (trim) para evitar problemas de espacios en blanco
            var mapaBuzones = deps
                .GroupBy(b => b.NC?.Trim() ?? b.NC)
                .ToDictionary(
                    g => g.Key,
                    g => g.First()
                );

            // ✅ Logging para diagnóstico
            ServicioLog.instancia.WriteInfo(
                $"Hidratando acreditaciones | Total buzones: {deps.Count} | " +
                $"Buzones únicos en mapa: {mapaBuzones.Count} | " +
                $"NCs en mapa: {string.Join(", ", mapaBuzones.Keys.Take(10))}",
                "ServicioEnvioMasivo | hidratarDTOconSusAcreditaciones");

            var hendersons = deps.Where(b => b.EsHenderson).Select(b => b.NC?.Trim() ?? b.NC).Distinct().ToList();
            var normals = deps.Where(b => !b.EsHenderson).Select(b => b.NC?.Trim() ?? b.NC).Distinct().ToList();

            // ✅ Logging para diagnóstico
            ServicioLog.instancia.WriteInfo(
                $"Clasificación buzones | Henderson: {hendersons.Count} | Normales: {normals.Count} | " +
                $"NCs normales: {string.Join(", ", normals.Take(10))}",
                "ServicioEnvioMasivo | hidratarDTOconSusAcreditaciones");


            DataTable BuildTvp(List<string> list)
            {
                var tvp = new DataTable();
                tvp.Columns.Add("NC", typeof(string));
                foreach (var nc in list) tvp.Rows.Add(nc);
                return tvp;
            }
            var tvpH = BuildTvp(hendersons);

            var tvpN = BuildTvp(normals);

            DateTime today = DateTime.Today;

            using (var conn = new SqlConnection(ConfiguracionGlobal.Conexion22))
            {
                await conn.OpenAsync();

                // A) Sólo Henderson - Lógica específica para Henderson (no se modifica)
                if (hendersons.Any())
                {
                    // Calcular fechas para Henderson según numEnvioMasivo
                    DateTime startH, endH;

                    // ✅ Si numEnvioMasivo > 2, buscar todo el día (no filtrar por rango)
                    if (numEnvioMasivo > 2)
                    {
                        // Mostrar todo el día: desde 00:00:00 hasta 23:59:59
                        startH = today.Date;
                        endH = today.Date.AddDays(1).AddSeconds(-1);
                    }
                    else if (numEnvioMasivo == 1)
                    {
                        // Tanda 1: desde día anterior 14:30 hasta hoy 7:00
                        switch (today.DayOfWeek)
                        {
                            case DayOfWeek.Monday:
                                startH = today.AddDays(-3).AddHours(14).AddMinutes(30);
                                break;
                            default:
                                startH = today.AddDays(-1).AddHours(14).AddMinutes(30);
                                break;
                        }
                        endH = today.AddHours(7);
                    }
                    else // numEnvioMasivo == 2
                    {
                        // Tanda 2: desde hoy 7:00 hasta hoy 14:30
                        startH = today.AddHours(7);
                        endH = today.AddHours(14).AddMinutes(30);
                    }

                    // ✅ Logging para diagnóstico
                    ServicioLog.instancia.WriteInfo(
                        $"Filtro Henderson | NumEnvioMasivo: {numEnvioMasivo} | " +
                        $"Desde: {startH:yyyy-MM-dd HH:mm:ss} | Hasta: {endH:yyyy-MM-dd HH:mm:ss}",
                        "ServicioEnvioMasivo | hidratarDTOconSusAcreditaciones");

                    const string sqlH = @"
                                    SELECT * 
                                    FROM acreditaciondepositodiegotest
                                    WHERE fecha > @fechaInicio AND fecha <= @fechaFin
                                    AND idbuzon IN (SELECT NC FROM @ListaH)";

                    using var cmdH = new SqlCommand(sqlH, conn);
                    // Parámetro TVP para Henderson
                    var pH = cmdH.Parameters.Add("@ListaH", SqlDbType.Structured);
                    pH.Value = tvpH;
                    pH.TypeName = "dbo.ListaNC";
                    // Parámetros de fecha
                    cmdH.Parameters.AddWithValue("@fechaInicio", startH);
                    cmdH.Parameters.AddWithValue("@fechaFin", endH);

                    using var readerH = await cmdH.ExecuteReaderAsync();
                    await MapearAcreditaciones(readerH, mapaBuzones);
                }

                // B) Sólo normales
                if (normals.Any())
                {
                    // ✅ Simplificado: buscar acreditaciones normales por fecha actual (GETDATE())
                    // ✅ Logging para diagnóstico
                    ServicioLog.instancia.WriteInfo(
                        $"Filtro buzones normales | Fecha actual: {today:yyyy-MM-dd} | " +
                        $"Buzones: {normals.Count}",
                        "ServicioEnvioMasivo | hidratarDTOconSusAcreditaciones");

                    // ✅ Consulta SQL: filtra por FECHA = fecha actual (GETDATE())
                    const string sqlN = @"SELECT * 
                                           FROM acreditaciondepositodiegotest
                                           WHERE CONVERT(DATE, FECHA) = CONVERT(DATE, GETDATE())
                                             AND idbuzon IN (SELECT NC FROM @ListaN)";

                    using var cmdN = new SqlCommand(sqlN, conn);
                    // Parámetro TVP para normales
                    var pN = cmdN.Parameters.Add("@ListaN", SqlDbType.Structured);
                    pN.Value = tvpN;
                    pN.TypeName = "dbo.ListaNC";

                    using var readerN = await cmdN.ExecuteReaderAsync();
                    await MapearAcreditaciones(readerN, mapaBuzones);
                }
            }
        }

        // Método auxiliar que mapea cada fila del reader al BuzonDTO correspondiente
        private async Task MapearAcreditaciones(SqlDataReader reader, Dictionary<string, BuzonDTO> mapa)
        {
            int ncOrd = reader.GetOrdinal("IDBUZON");
            int opOrd = reader.GetOrdinal("IDOPERACION");
            int cuentaOrd = reader.GetOrdinal("IDCUENTA");
            int monOrd = reader.GetOrdinal("MONEDA");
            int montoOrd = reader.GetOrdinal("MONTO");
            int bancoOrd = reader.GetOrdinal("IDBANCO");

            int acreditacionesMapeadas = 0;
            int acreditacionesNoMapeadas = 0;
            var ncsNoEncontrados = new HashSet<string>();

            while (await reader.ReadAsync())
            {
                var acc = new AcreditacionDTO
                {
                    NC = reader.GetString(ncOrd)?.Trim(), // ✅ Normalizar espacios
                    IdOperacion = reader.GetInt64(opOrd),
                    IdCuenta = reader.GetInt32(cuentaOrd),
                    Divisa = reader.GetInt32(monOrd),
                    Monto = reader.GetDouble(montoOrd),
                    IdBanco = reader.GetInt32(bancoOrd)
                };
                acc.setMoneda();

                acc.Banco = ServicioBanco.getInstancia().getById(acc.IdBanco);

                if (mapa.TryGetValue(acc.NC, out var buzon))
                {
                    buzon.Acreditaciones.Add(acc);
                    acreditacionesMapeadas++;
                }
                else
                {
                    acreditacionesNoMapeadas++;
                    ncsNoEncontrados.Add(acc.NC);

                    // ✅ Logging detallado para diagnóstico
                    ServicioLog.instancia.WriteWarning(
                        $"Acreditación no mapeada | IDBUZON: '{acc.NC}' | IDOPERACION: {acc.IdOperacion} | " +
                        $"IDCUENTA: {acc.IdCuenta} | Buzones en mapa: {mapa.Count} | " +
                        $"NCs en mapa: {string.Join(", ", mapa.Keys.Take(5))}",
                        "ServicioEnvioMasivo | MapearAcreditaciones");
                }
            }

            // ✅ Logging resumen
            if (acreditacionesNoMapeadas > 0)
            {
                ServicioLog.instancia.WriteWarning(
                    $"Resumen mapeo acreditaciones | Mapeadas: {acreditacionesMapeadas} | " +
                    $"No mapeadas: {acreditacionesNoMapeadas} | NCs no encontrados: {string.Join(", ", ncsNoEncontrados.Take(10))}",
                    "ServicioEnvioMasivo | MapearAcreditaciones");
            }
        }


        public async Task obtenerUsuarioYFechaDelDeposito(List<BuzonDTO> buzones)
        {
            // 1. Early exit
            if (buzones == null || buzones.Count == 0) return;

            // 2. Recolectar pares únicos (NC, IdOperacion)
            var keys = buzones
              .SelectMany(b => b.Acreditaciones)
              .Select(a => (a.NC, a.IdOperacion))
              .Distinct()
              .ToList();

            // 3. Llenar un DataTable para el TVP
            var tvp = new DataTable();
            tvp.Columns.Add("NC", typeof(string));
            // ✅ Fix: IdOperacion en DEPOSITOS es int, no bigint
            tvp.Columns.Add("IdOperacion", typeof(int));

            foreach (var (nc, idOp) in keys)
            {
                // Convertir long a int para el TVP (IdOperacion en DEPOSITOS es int)
                // Si es mayor que int.MaxValue, no se puede buscar en DEPOSITOS (que solo tiene int)
                // así que lo omitimos del TVP
                if (idOp > int.MaxValue)
                {
                    ServicioLog.instancia.WriteInfo($"IdOperacion fuera de rango omitido",
                        $"NC: '{nc}', IdOperacion: {idOp} (mayor que int.MaxValue)");
                    continue;
                }

                int idOpInt = (int)idOp;
                // Normalizar NC (trim) antes de agregar al TVP
                var ncNormalized = string.IsNullOrWhiteSpace(nc) ? nc : nc.Trim();
                tvp.Rows.Add(ncNormalized, idOpInt);
            }

            // 4. Ejecutar un solo SELECT con TRIM para manejar espacios en códigos
            var sql = @"
                        SELECT 
                        LTRIM(RTRIM(d.codigo))   AS NC,
                        d.idoperacion AS IdOperacion,
                        d.usuario,
                        d.fechadep  AS FechaDep,
                        d.empresa
                        FROM depositos d
                        INNER JOIN @ListaKeys k
                        ON LTRIM(RTRIM(d.codigo)) = LTRIM(RTRIM(k.NC))
                        AND d.idoperacion = k.IdOperacion;";

            using var conn = new SqlConnection(ConfiguracionGlobal.ConexionWebBuzones);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            var p = cmd.Parameters.AddWithValue("@ListaKeys", tvp);
            p.SqlDbType = SqlDbType.Structured;
            p.TypeName = "dbo.ListaDepositosKey";

            // 5. Leer TODO en un diccionario
            var dict = new Dictionary<(string NC, long IdOp), (string Usuario, DateTime FechaDep, string Empresa)>();
            using var reader = await cmd.ExecuteReaderAsync();
            int ordNc = reader.GetOrdinal("NC");
            int ordOp = reader.GetOrdinal("IdOperacion");
            int ordUser = reader.GetOrdinal("usuario");
            int ordFecha = reader.GetOrdinal("FechaDep");
            int ordEmp = reader.GetOrdinal("empresa");

            int totalRows = 0;
            int gendilRows = 0;
            while (await reader.ReadAsync())
            {
                totalRows++;
                var nc = reader.GetString(ordNc);
                // ✅ Fix: La columna idoperacion en la tabla depositos es Int32
                // Convertimos a long para mantener compatibilidad con el diccionario que usa long
                int idOpInt = reader.IsDBNull(ordOp) ? 0 : reader.GetInt32(ordOp);
                long idOp = idOpInt; // Convertir a long para el diccionario
                var user = reader.IsDBNull(ordUser) ? null : reader.GetString(ordUser);
                var fechaDep = reader.GetDateTime(ordFecha);
                var emp = reader.IsDBNull(ordEmp) ? null : reader.GetString(ordEmp);

                // Normalizar NC (trim) para el diccionario
                var ncNormalized = string.IsNullOrWhiteSpace(nc) ? nc : nc.Trim();

                dict[(ncNormalized, idOp)] = (user, fechaDep, emp);
            }


            // 6. Obtener empresas desde cuentasbuzones como fallback para los casos donde no esté en depositos
            var empresasFallback = await obtenerEmpresasDesdeCuentasBuzones(buzones);

            // 7. Asignar a cada AcreditacionDTO
            bool empresaDeLaAcreditacionAsignadaAlBuzon = false;
            foreach (var b in buzones)
            {

                foreach (var a in b.Acreditaciones)
                {
                    // Normalizar NC para la búsqueda (trim)
                    var ncNormalized = string.IsNullOrWhiteSpace(a.NC) ? a.NC : a.NC.Trim();

                    // El diccionario usa long, así que usamos el IdOperacion directamente (ya es long)
                    // Pero necesitamos convertir a int para el TVP, y luego de vuelta a long para buscar
                    int idOpInt = a.IdOperacion > int.MaxValue ? 0 : (int)a.IdOperacion;
                    long idOpForSearch = idOpInt; // Convertir de vuelta a long para buscar en el diccionario



                    if (dict.TryGetValue((ncNormalized, idOpForSearch), out var info))
                    {
                        a.Usuario = info.Usuario;
                        a.FechaDep = info.FechaDep;



                        // Si la empresa está vacía o null en depositos, usar el fallback desde cuentasbuzones
                        if (string.IsNullOrWhiteSpace(info.Empresa))
                        {
                            // Intentar obtener desde fallback usando NC e IdCuenta
                            var empresaFallbackTupla = empresasFallback
                                .FirstOrDefault(e => e.NC == a.NC && e.IdCuenta == a.IdCuenta);
                            a.Empresa = empresaFallbackTupla.NC != null ? empresaFallbackTupla.Empresa : info.Empresa;


                        }
                        else
                        {
                            a.Empresa = info.Empresa;
                        }

                        if (empresaDeLaAcreditacionAsignadaAlBuzon == false && !string.IsNullOrWhiteSpace(a.Empresa))
                        {
                            empresaDeLaAcreditacionAsignadaAlBuzon = true;
                            b.Empresa = a.Empresa;
                        }
                    }
                    else
                    {


                        var empresaFallbackTupla = empresasFallback
                            .FirstOrDefault(e => e.NC == a.NC && e.IdCuenta == a.IdCuenta);
                        a.Usuario = null;
                        a.Empresa = empresaFallbackTupla.NC != null ? empresaFallbackTupla.Empresa : null;



                        if (empresaDeLaAcreditacionAsignadaAlBuzon == false && !string.IsNullOrWhiteSpace(a.Empresa))
                        {
                            empresaDeLaAcreditacionAsignadaAlBuzon = true;
                            b.Empresa = a.Empresa;
                        }
                    }
                }
                empresaDeLaAcreditacionAsignadaAlBuzon = false;
            }
        }
        private async Task<List<(string NC, int IdCuenta, string Empresa)>> obtenerEmpresasDesdeCuentasBuzones(List<BuzonDTO> buzones)
        {
            if (buzones == null || buzones.Count == 0) return new List<(string, int, string)>();

            // Recolectar pares únicos (NC, IdCuenta) de todas las acreditaciones
            var keys = buzones
                .SelectMany(b => b.Acreditaciones)
                .Select(a => (a.NC, a.IdCuenta))
                .Distinct()
                .ToList();

            if (keys.Count == 0) return new List<(string, int, string)>();

            // Obtener lista de NCs únicos para filtrar
            var ncs = keys.Select(k => k.NC).Distinct().ToList();
            var idCuentasSet = keys.Select(k => k.IdCuenta).Distinct().ToHashSet();

            // Crear DataTable para TVP de NCs
            var tvpNcs = new DataTable();
            tvpNcs.Columns.Add("NC", typeof(string));
            foreach (var nc in ncs)
                tvpNcs.Rows.Add(nc);

            // Consulta que obtiene todas las empresas para los NCs, luego filtramos por IdCuenta en memoria
            var sql = @"
                SELECT DISTINCT
                    config.NC,
                    cb.ID AS IdCuenta,
                    cb.EMPRESA
                FROM ConfiguracionAcreditacion config
                INNER JOIN cuentasbuzones cb ON config.CuentasBuzonesId = cb.ID
                INNER JOIN @ListaNC k ON config.NC = k.NC
                WHERE cb.EMPRESA IS NOT NULL 
                AND LTRIM(RTRIM(cb.EMPRESA)) <> ''";

            var resultado = new List<(string NC, int IdCuenta, string Empresa)>();

            using var conn = new SqlConnection(ConfiguracionGlobal.Conexion22);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            var p = cmd.Parameters.AddWithValue("@ListaNC", tvpNcs);
            p.SqlDbType = SqlDbType.Structured;
            p.TypeName = "dbo.ListaNC";

            using var reader = await cmd.ExecuteReaderAsync();
            int ordNc = reader.GetOrdinal("NC");
            int ordIdCuenta = reader.GetOrdinal("IdCuenta");
            int ordEmpresa = reader.GetOrdinal("EMPRESA");

            while (await reader.ReadAsync())
            {
                var nc = reader.GetString(ordNc);
                var idCuenta = reader.GetInt32(ordIdCuenta);
                var empresa = reader.IsDBNull(ordEmpresa) ? null : reader.GetString(ordEmpresa);

                // Filtrar en memoria solo los IdCuentas que necesitamos
                if (idCuentasSet.Contains(idCuenta) && !string.IsNullOrWhiteSpace(empresa))
                {
                    resultado.Add((nc, idCuenta, empresa));
                }
            }

            return resultado;
        }

        public async Task obtenerFechaUltimaConexionDelBuzon(List<BuzonDTO> buzones)
        {
            if (buzones == null || buzones.Count == 0) return;

            var keys = buzones.Select(b => b.NC).Distinct().ToList();

            var tvp = new DataTable();

            tvp.Columns.Add("NC", typeof(string));

            foreach (var nc in keys)
                tvp.Rows.Add(nc);

            var query = @" Select k.NC,Niveles.FechaUltConex from Niveles inner join @ListaNC k 
                           ON niveles.CodigoBuzon = k.NC";

            using var conn = new SqlConnection(ConfiguracionGlobal.ConexionWebBuzones);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(query, conn);

            var p = cmd.Parameters.AddWithValue("@ListaNC", tvp);

            p.SqlDbType = SqlDbType.Structured;

            p.TypeName = "dbo.ListaNC";

            var dict = new Dictionary<string, DateTime>();
            using var reader = await cmd.ExecuteReaderAsync();
            int ordNc = reader.GetOrdinal("NC");
            int ordFecha = reader.GetOrdinal("FechaUltConex");
            while (await reader.ReadAsync())
            {
                var nc = reader.GetString(ordNc);
                var fecha = reader.GetDateTime(ordFecha);
                dict[nc] = fecha;
            }

            foreach (var b in buzones)
            {
                if (dict.TryGetValue(b.NC, out var f))
                    b.UltimaFechaConexion = f;
                else
                    b.UltimaFechaConexion = DateTime.MinValue;
            }
        }

        private async Task<List<BuzonDTO>> getBuzonesByNumeroEnvioMasivo(int numEnvioMasivo)
        {

            var desdeTime = new TimeSpan(0, 0, 0);

            var hastaTime = new TimeSpan(0, 0, 0);

            string query = "";

            List<BuzonDTO> retorno = new List<BuzonDTO>();

            switch (numEnvioMasivo)
            {
                //Son son los <= 7
                case 1:
                    desdeTime = new TimeSpan(0, 0, 0);
                    hastaTime = new TimeSpan(7, 0, 0);
                    break;

                //Son los cierre > 7 pero < a 14:30
                case 2:
                    desdeTime = new TimeSpan(7, 0, 0);
                    hastaTime = new TimeSpan(14, 30, 0);
                    break;

                case 3:
                    // rangos de 14:30 a 17:00
                    desdeTime = new TimeSpan(14, 30, 0);
                    hastaTime = new TimeSpan(17, 0, 0);
                    break;

                case 4:
                    // rangos de 17:00 a 19:00
                    desdeTime = new TimeSpan(17, 0, 0);
                    hastaTime = new TimeSpan(19, 0, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(numEnvioMasivo));
            }

                query =     @"SELECT c.NC, c.NN, c.SUCURSAL, c.CIERRE,c.IDCLIENTE , ws.NombreWS
                            from
                            cc as c 
                            left join 
                            cc_nombrews as ws 
                            on ws.NC = c.NC 
                            where c.estado = 'alta'
                            AND CAST(c.CIERRE AS time) > @desdeTime 
                            AND CAST(c.CIERRE AS time) <= @hastaTime";

            using (SqlConnection conn = new SqlConnection(ConfiguracionGlobal.Conexion22))
            {

                await conn.OpenAsync();

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@desdeTime", desdeTime);

                cmd.Parameters.AddWithValue("@hastaTime", hastaTime);

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {

                    int ncOrdinal = reader.GetOrdinal("NC");

                    int nnOrdinal = reader.GetOrdinal("NN");

                    int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");

                    int cierreOrdinal = reader.GetOrdinal("CIERRE");

                    int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");

                    int nombreWSOrdinal = reader.GetOrdinal("NombreWS");

                    while (await reader.ReadAsync())
                    {
                        BuzonDTO dto = new BuzonDTO();
                        dto.NC = reader.GetString(ncOrdinal)?.Trim(); // ✅ Normalizar espacios
                        dto.NN = reader.GetString(nnOrdinal);
                        dto.Sucursal = reader.GetString(sucursalOrdinal);
                        dto.Cierre = reader.GetDateTime(cierreOrdinal);
                        dto.Email = "acreditaciones@tecnisegur.com.uy";
                        dto.IdCliente = reader.GetInt32(idClienteOrdinal);
                        dto.EsHenderson = dto.esHenderson();
                        dto.NumeroEnvioMasivo = numEnvioMasivo;
                        if (!reader.IsDBNull(nombreWSOrdinal))
                        {
                            dto.NombreWS = reader.GetString(nombreWSOrdinal);
                        }
                        else
                        {
                            dto.NombreWS = "NO_DEFINIDO";
                        }
                        retorno.Add(dto);

                    }
                }

                if (numEnvioMasivo == 1)
                {

                    string queryParaObtenerHenderson = @"select distinct cc.NC,NN,SUCURSAL,CIERRE,cc.IDCLIENTE,cnws.NombreWS from cc
                                                        inner join ClientesRelacionadosTest as cr 
                                                        on cc.IDCLIENTE = cr.IdRazonSocial 
                                                        left join CC_NombreWS cnws
                                                        on cc.NC = cnws.NC
                                                        where cr.IdCliente = 164 
                                                        and tanda = 1 and estado = 'alta' ";

                    SqlCommand cmd2 = new SqlCommand(queryParaObtenerHenderson, conn);

                    using (SqlDataReader reader2 = await cmd2.ExecuteReaderAsync())
                    {
                        int ncOrdinal = reader2.GetOrdinal("NC");

                        int nnOrdinal = reader2.GetOrdinal("NN");

                        int sucursalOrdinal = reader2.GetOrdinal("SUCURSAL");

                        int cierreOrdinal = reader2.GetOrdinal("CIERRE");

                        int idClienteOrdinal = reader2.GetOrdinal("IDCLIENTE");

                        int nombreWSOrdinal = reader2.GetOrdinal("NombreWS");

                        while (await reader2.ReadAsync())
                        {

                            BuzonDTO dto = new BuzonDTO();
                            dto.NC = reader2.GetString(ncOrdinal);
                            dto.NN = reader2.GetString(nnOrdinal);
                            dto.Sucursal = reader2.GetString(sucursalOrdinal);
                            dto.Cierre = reader2.GetDateTime(cierreOrdinal);
                            dto.Email = "acreditaciones@tecnisegur.com.uy";
                            dto.IdCliente = reader2.GetInt32(idClienteOrdinal);
                            dto.EsHenderson = true;
                            dto.NumeroEnvioMasivo = numEnvioMasivo;
                            if (!reader2.IsDBNull(nombreWSOrdinal))
                            {
                                dto.NombreWS = reader2.GetString(nombreWSOrdinal);
                            }
                            else
                            {
                                dto.NombreWS = "NO_DEFINIDO";
                            }
                            retorno.Add(dto);
                        }
                    }

                }

                if (numEnvioMasivo == 3)
                {
                    // ✅ Incluir buzones específicos del masivo 1 en el masivo 3
                    string queryParaObtenerBuzonesEspecificos = @"SELECT c.NC, c.NN, c.SUCURSAL, c.CIERRE, c.IDCLIENTE, ws.NombreWS
                                                                    FROM cc as c 
                                                                    LEFT JOIN cc_nombrews as ws ON ws.NC = c.NC 
                                                                    WHERE c.estado = 'alta'
                                                                    AND c.NC IN (
                                                                        'EA20L0108N12000027',
                                                                        'EA23L0725N12000083',
                                                                        'EA23L0725N12000094',
                                                                        'EA23L0725N12000101',
                                                                        'EA23L0725N12000104',
                                                                        'EA23L0810N12000117',
                                                                        'EA24L0101N13000011'
                                                                    )";

                    SqlCommand cmd3 = new SqlCommand(queryParaObtenerBuzonesEspecificos, conn);

                    using (SqlDataReader reader3 = await cmd3.ExecuteReaderAsync())
                    {
                        int ncOrdinal = reader3.GetOrdinal("NC");
                        int nnOrdinal = reader3.GetOrdinal("NN");
                        int sucursalOrdinal = reader3.GetOrdinal("SUCURSAL");
                        int cierreOrdinal = reader3.GetOrdinal("CIERRE");
                        int idClienteOrdinal = reader3.GetOrdinal("IDCLIENTE");
                        int nombreWSOrdinal = reader3.GetOrdinal("NombreWS");

                        while (await reader3.ReadAsync())
                        {
                            BuzonDTO dto = new BuzonDTO();
                            dto.NC = reader3.GetString(ncOrdinal)?.Trim(); // ✅ Normalizar espacios
                            dto.NN = reader3.GetString(nnOrdinal);
                            dto.Sucursal = reader3.GetString(sucursalOrdinal);
                            dto.Cierre = reader3.GetDateTime(cierreOrdinal);
                            dto.Email = "acreditaciones@tecnisegur.com.uy";
                            dto.IdCliente = reader3.GetInt32(idClienteOrdinal);
                            dto.EsHenderson = dto.esHenderson();
                            dto.NumeroEnvioMasivo = numEnvioMasivo;
                            if (!reader3.IsDBNull(nombreWSOrdinal))
                            {
                                dto.NombreWS = reader3.GetString(nombreWSOrdinal);
                            }
                            else
                            {
                                dto.NombreWS = "NO_DEFINIDO";
                            }
                            retorno.Add(dto);
                        }
                    }
                }
            }

            return retorno;

        }

        public class TotalesImprimir
        {
            public string EMPRESA { get; set; }
            public string TOTALPESOS { get; set; }
            public string TOTALDOLARES { get; set; }
            public string TOTALARG { get; set; }
            public string TOTALEUROS { get; set; }
            public string TOTALREALES { get; set; }
        }

        public class DepositosCCImprimir
        {
            public string OPERACION { get; set; }
            public string MONEDA { get; set; }
            public string FECHA { get; set; }
            public string TOTAL { get; set; }
            public string USUARIO { get; set; }
            public string EMPRESA { get; set; }
            public string SUCURSAL { get; set; }
        }
    }
}
