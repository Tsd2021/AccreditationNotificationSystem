//using Microsoft.Data.SqlClient;
//using SharedDTOs;
//using TAAS.Reports;

//namespace ANS.Model.Services
//{
//    public class ServicioEnvioAcreditacionManual
//    {
//        public static ServicioEnvioAcreditacionManual instancia;
//        public ServicioEmail _emailService { get; set; } = ServicioEmail.getInstancia();
//        public ReportService _reportService = new TAAS.Reports.ReportService();

//        public static ServicioEnvioAcreditacionManual getInstancia()
//        {

//            if (instancia == null)
//            {
//                instancia = new ServicioEnvioAcreditacionManual();
//            }

//            return instancia;

//        }

//        public async Task EnviarAcreditacionManual(BuzonDTO buzon, int numTanda, DateTime fecha)
//        {
//            try
//            {
//                await GetAcreditacionesByBuzonTandaYFecha(buzon, numTanda, fecha);

//                if(buzon.Acreditaciones == null || !buzon.Acreditaciones.Any())
//                {
//                    return;
//                }

//                List<BuzonDTO> buzones = new List<BuzonDTO>();

//                buzones.Add(buzon);

//                await ServicioEnvioMasivo.getInstancia().obtenerUsuarioYFechaDelDeposito(buzones);

//                await GenerarReporteYEnviarEmail(buzon,fecha);
//            }

//            catch (Exception ex)
//            {
//                ServicioLog.instancia.WriteLog(ex, $"{buzon.NC}", "- Envío Acreditación Manual Fallido");
//                throw;
//            }
//        }

//        private async Task GetAcreditacionesByBuzonTandaYFecha(BuzonDTO buzon, int numTanda, DateTime fecha)
//        {
//            try
//            {
//                if (buzon == null) throw new Exception("Error: El buzon no puede ser nulo.");

//                if (fecha <= DateTime.MinValue || fecha >= DateTime.MaxValue)
//                    throw new Exception("Error: La fecha elegida es incorrecta.");

//                buzon.NumeroEnvioMasivo = numTanda;

//                using (var conn = new SqlConnection(ConfiguracionGlobal.Conexion22))
//                {
//                    await conn.OpenAsync();

//                    using (var cmd = ArmarCommand(conn, buzon, numTanda, fecha))

//                    using (var reader = await cmd.ExecuteReaderAsync())
//                    {
//                        var acreditaciones = await MapearAcreditaciones(reader);

//                        buzon.Acreditaciones = acreditaciones;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                //ServicioLog.instancia.WriteLog(ex, $"{buzon.NC}", "- Error al obtener acreditaciones");
//                Console.WriteLine(ex.Message);
//                throw;
//            }
//        }

//        private SqlCommand ArmarCommand(SqlConnection conn, BuzonDTO b, int numTanda, DateTime fecha)
//        {

//            try
//            {

//                var (desde, cierre) = CalcularVentana(b, numTanda, fecha);

//                var sqlobsolete =
//                        @"
//                        SELECT  a.IDBUZON, a.IDOPERACION, a.IDCUENTA, a.MONEDA, a.MONTO, a.FECHA
//                        FROM    ACREDITACIONESDEPOSITOS a
//                        JOIN cc c ON a.IDBUZON = c.nc 
//                        WHERE   a.IDBUZON = @NC
//                        AND a.FECHA >= @Desde
//                        AND a.FECHA <= @Cierre
//                        AND c.CIERRE <= @Cierre  
//                        ORDER BY a.IDOPERACION DESC;";

//                var sql = @"


//                            SELECT  a.IDBUZON, a.IDOPERACION, a.IDCUENTA, a.MONEDA, a.MONTO, a.FECHA
//                            FROM    AcreditacionDepositoDiegoTest a
//                            JOIN    cc c
//                                    ON LTRIM(RTRIM(a.IDBUZON)) = LTRIM(RTRIM(c.nc))
//                            WHERE   LTRIM(RTRIM(a.IDBUZON)) = LTRIM(RTRIM(@NC))
//                                AND   a.FECHA >= @Desde
//                                AND   a.FECHA <= @Cierre
//                                AND   c.CIERRE <= @Cierre
//                            ORDER BY a.IDOPERACION DESC;";

//                var cmd = new SqlCommand(sql, conn);

//                cmd.Parameters.Add(new SqlParameter("@NC", System.Data.SqlDbType.VarChar, 50) { Value = b.NC ?? (object)DBNull.Value });

//                cmd.Parameters.Add(new SqlParameter("@Desde", System.Data.SqlDbType.DateTime) { Value = desde });

//                cmd.Parameters.Add(new SqlParameter("@Cierre", System.Data.SqlDbType.DateTime) { Value = cierre });

//                return cmd;
//            }
//            catch (Exception ex)
//            {
//                //ServicioLog.instancia.WriteLog(ex, $"{b.NC}", "- Error al armar el comando SQL");
//                Console.WriteLine(ex.Message);
//                throw;
//            }

//        }

//        private (DateTime desde, DateTime cierre) CalcularVentana(BuzonDTO b, int numTanda, DateTime fecha)
//        {
//            var desde = fecha.Date;

//            DateTime cierre;

//            if (b.esHenderson())
//            {
//                if (numTanda == 1)
//                {

//                    cierre = fecha.Date.AddHours(7);
//                }
//                else if (numTanda == 2)
//                {

//                    cierre = fecha.Date.AddHours(14).AddMinutes(30);
//                }
//                else
//                {
//                    throw new Exception("NumTanda inválido. Debe ser 1 o 2 para Henderson.");
//                }
//            }

//            else
//            {

//                cierre = fecha.Date.AddHours(b.Cierre.Hour).AddMinutes(b.Cierre.Minute);

//            }

//            return (desde, cierre);
//        }

//        private async Task<List<AcreditacionDTO>> MapearAcreditaciones(SqlDataReader reader)
//        {

//            List<AcreditacionDTO> acreditaciones = new List<AcreditacionDTO>();

//            // -- ORDINALS -- //
//            int ncOrd = reader.GetOrdinal("IDBUZON");
//            int opOrd = reader.GetOrdinal("IDOPERACION");
//            int cuentaOrd = reader.GetOrdinal("IDCUENTA");
//            int monOrd = reader.GetOrdinal("MONEDA");
//            int montoOrd = reader.GetOrdinal("MONTO");


//            // -- READ & MAP ACCREDITATIONS -- //
//            while (await reader.ReadAsync())
//            {
//                var acc = new AcreditacionDTO
//                {
//                    NC = reader.GetString(ncOrd),
//                    IdOperacion = reader.GetInt64(opOrd),
//                    IdCuenta = reader.GetInt32(cuentaOrd),
//                    Divisa = reader.GetInt32(monOrd),
//                    Monto = reader.GetDouble(montoOrd)
//                };
//                acc.setMoneda();
//                acreditaciones.Add(acc);
//            }
//            return acreditaciones;
//        }

//        private async Task GenerarReporteYEnviarEmail(BuzonDTO b,DateTime fechaElegida)
//        {

//            var semaphore = new SemaphoreSlim(initialCount: 20, maxCount: 20);

//            var smtp = await ServicioEmail.instancia.getNewSmptClient();

//            var sendLock = new SemaphoreSlim(1, 1);

//            var reportService = new ReportService();

//            b.MontoTotal = b.Acreditaciones.Sum(a => a.Monto);

//            var b2 = new BuzonDTO2
//            {
//                NC = b.NC,
//                NN = b.NN,
//                Empresa = b.Empresa,
//                FechaInicio = b.FechaInicio,
//                Cierre = b.Cierre,
//                MontoTotal = b.MontoTotal,
//                Moneda = b.Moneda,
//                Divisa = b.Divisa,
//                IdOperacion = b.IdOperacion,
//                Sucursal = b.Sucursal,
//                IdOperacionFinal = b.IdOperacionFinal,
//                IdOperacionInicio = b.IdOperacionInicio,
//                NumeroEnvioMasivo = b.NumeroEnvioMasivo,
//                UltimaFechaConexion = b.UltimaFechaConexion,
//                EsHenderson = b.EsHenderson,
//                NombreWS = b.NombreWS,
//                Acreditaciones = b.Acreditaciones.Select(a => new AcreditacionDTO2
//                {
//                    NC = a.NC,
//                    IdOperacion = a.IdOperacion,
//                    Divisa = a.Divisa,
//                    Monto = a.Monto,
//                    Usuario = a.Usuario,
//                    FechaDep = a.FechaDep,
//                    Empresa = a.Empresa
//                }).ToList()
//            };

//            var excelStream = reportService.ArmarYEnviarExcelDeUnBuzon(b2,fechaElegida, out var subject, out var body, out var fileName);

//            await semaphore.WaitAsync();

//            try
//            {

//                await sendLock.WaitAsync();

//                try
//                {

//                    await ServicioEmail.instancia
//                        .EnviarExcelPorMailMasivoConMailKit(
//                           excelStream, fileName, subject, body, b._Emails, smtp);

//                }
//                catch (Exception ex)
//                {

//                    Console.WriteLine($"Error al enviar el correo: {ex.Message}");
//                }

//                finally
//                {

//                    sendLock.Release();

//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error al esperar el semáforo: {ex.Message}");
//            }
//            finally
//            {
//                semaphore.Release();

//            }
//        }
//    }
//}

using ANS.Runtime;
using Microsoft.Data.SqlClient;
using SharedDTOs;
using System.IO;
using TAAS.Reports;

namespace ANS.Model.Services
{
    public class ServicioEnvioAcreditacionManual
    {
        // Destinos fijos para el envío "Excel SOLO B2B" (buzones HENDERSON)
        // (mismo formato que en ServicioEnvioMasivo: lista separada por ',' o ';')
        private const string B2B_MAIL_DESTINO =
            "sectorbancos@tiendainglesa.com.uy, sdeliotti@tiendainglesa.com.uy, agomez@tiendainglesa.com.uy, pepper@tiendainglesa.com.uy";

        // Para pruebas en TEST: simulamos "B2B" usando esta empresa
        private const string TEST_EMPRESA_B2B_SIMULADA = "FARMACIA TIENDA INGLESA";

        private static bool IsB2BDestinoConfigurado()
        {
            return !string.IsNullOrWhiteSpace(B2B_MAIL_DESTINO) &&
                   !string.Equals(B2B_MAIL_DESTINO, "PENDIENTE_DESTINO_B2B", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsEmpresaB2B(string? empresa)
        {
            var emp = empresa?.Trim();
            if (string.IsNullOrWhiteSpace(emp))
                return false;

            if (string.Equals(emp, "B2B", StringComparison.OrdinalIgnoreCase))
                return true;

            return AppRuntime.IsTest &&
                   emp.Contains(TEST_EMPRESA_B2B_SIMULADA, StringComparison.OrdinalIgnoreCase);
        }

        private static string AppendSuffixToFileName(string fileName, string suffix)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return fileName;
            if (string.IsNullOrEmpty(suffix)) return fileName;

            var ext = Path.GetExtension(fileName);
            var baseName = Path.GetFileNameWithoutExtension(fileName);

            if (string.IsNullOrWhiteSpace(ext))
            {
                return $"{baseName}{suffix}";
            }

            return $"{baseName}{suffix}{ext}";
        }

        private static List<Email> ParseDestinosB2B()
        {
            var correosB2B = B2B_MAIL_DESTINO
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return correosB2B.Select(c => new Email
            {
                Correo = c,
                Activo = true,
                EsPrincipal = true
            }).ToList();
        }

        // ✅ Thread-safe: Lazy<T> garantiza inicialización única
        // El constructor privado carga datos pesados (sucursales), thread-safety previene múltiples cargas
        private static readonly Lazy<ServicioEnvioAcreditacionManual> _lazy = 
            new Lazy<ServicioEnvioAcreditacionManual>(() => new ServicioEnvioAcreditacionManual());
        
        public static ServicioEnvioAcreditacionManual instancia => _lazy.Value;

        public ServicioEmail _emailService { get; set; } = ServicioEmail.getInstancia();

        // ✅ Ya no instanciamos vacío. La seteamos en el ctor privado.
        public ReportService _reportService { get; private set; }

        // ✅ Ctor privado: carga sucursales, mapea a SharedDTOs y crea ReportService con inyección
        private ServicioEnvioAcreditacionManual()
        {
            var svcSuc = ServicioSucursalesClientes.getInstancia();

            // opcional: limpiar para evitar duplicados si este singleton se recreara (no debería)
            svcSuc.listaSucursalCliente.Clear();

            svcSuc.CargarSucursalesCliente();

            var sucursalesDtos = svcSuc.listaSucursalCliente
                .Select(x => new SucursalClienteDto
                {
                    NC = x.NC,
                    Empresa = x.Empresa,
                    IdCliente = x.IdCliente,
                    Sucursal = x.Sucursal
                })
                .ToList();

            _reportService = new ReportService(sucursalesDtos);
        }

        public static ServicioEnvioAcreditacionManual getInstancia()
        {
            return _lazy.Value;
        }

        public async Task EnviarAcreditacionManual(BuzonDTO buzon, int numTanda, DateTime fecha)
        {
            try
            {

                await GetAcreditacionesByBuzonTandaYFecha(buzon, numTanda, fecha);

                var buzones = new List<BuzonDTO> { buzon };
 
                await ServicioEnvioMasivo.getInstancia().obtenerUsuarioYFechaDelDeposito(buzones);

                await ServicioEnvioMasivo.getInstancia().obtenerFechaUltimaConexionDelBuzon(buzones);

                await GenerarReporteYEnviarEmail(buzon, fecha);
            }
            catch (Exception ex)
            {
                ServicioLog.instancia.WriteLog(ex, $"{buzon.NC}", "- Envío Acreditación Manual Fallido");
                throw;
            }
        }

        private async Task GetAcreditacionesByBuzonTandaYFecha(BuzonDTO buzon, int numTanda, DateTime fecha)
        {
            try
            {
                if (buzon == null) throw new Exception("Error: El buzon no puede ser nulo.");
                if (fecha <= DateTime.MinValue || fecha >= DateTime.MaxValue)
                    throw new Exception("Error: La fecha elegida es incorrecta.");

                buzon.NumeroEnvioMasivo = numTanda;
                
                // ✅ Normalizar NC para evitar problemas de espacios
                if (!string.IsNullOrWhiteSpace(buzon.NC))
                    buzon.NC = buzon.NC.Trim();

                using (var conn = new SqlConnection(ConfiguracionGlobal.Conexion22))
                {
                    await conn.OpenAsync();

                    using (var cmd = ArmarCommand(conn, buzon, numTanda, fecha))
                    {
                 
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            var acreditaciones = await MapearAcreditaciones(reader, buzon.NC);
                            buzon.Acreditaciones = acreditaciones;
                            
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private SqlCommand ArmarCommand(SqlConnection conn, BuzonDTO b, int numTanda, DateTime fecha)
        {
            var (desde, cierre) = CalcularVentana(b, numTanda, fecha);

            // ✅ Usar TableNameResolver para obtener nombre de tabla según RuntimeMode
            var tableName = TableNameResolver.AcreditacionDeposito;
            TableNameResolver.ValidateTableName(tableName, "ServicioEnvioAcreditacionManual.ArmarCommand");
            string sql = $@"
                SELECT  a.IDBUZON, a.IDOPERACION, a.IDCUENTA, a.MONEDA, a.MONTO, a.FECHA, a.IDBANCO 
                FROM    {tableName} a
                JOIN    cc c
                        ON LTRIM(RTRIM(a.IDBUZON)) = LTRIM(RTRIM(c.nc))
                WHERE   LTRIM(RTRIM(a.IDBUZON)) = LTRIM(RTRIM(@NC))
                    AND a.FECHA > @Desde
                    AND a.FECHA <= @Cierre
                ORDER BY a.IDOPERACION DESC;";


            //var sql = @"
            //    SELECT  a.IDBUZON, a.IDOPERACION, a.IDCUENTA, a.MONEDA, a.MONTO, a.FECHA, a.IDBANCO 
            //    FROM    AcreditacionDepositoDiegoTest a
            //    JOIN    cc c
            //            ON LTRIM(RTRIM(a.IDBUZON)) = LTRIM(RTRIM(c.nc))
            //    WHERE   LTRIM(RTRIM(a.IDBUZON)) = LTRIM(RTRIM(@NC))

            //    ORDER BY a.IDOPERACION DESC;";

            var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@NC", System.Data.SqlDbType.VarChar, 50) { Value = b.NC ?? (object)DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Desde", System.Data.SqlDbType.DateTime) { Value = desde });
            cmd.Parameters.Add(new SqlParameter("@Cierre", System.Data.SqlDbType.DateTime) { Value = cierre });
            return cmd;
        }

        private (DateTime desde, DateTime cierre) CalcularVentana(BuzonDTO b, int numTanda, DateTime fecha)
        {
            var desde = fecha.Date;
            DateTime cierre;

            // ✅ Filtrar por rango solo para TANDAS (Henderson) con numTanda 1 o 2
            // Para envío manual (numTanda == 0) o numTanda > 2, mostrar todo el día
            if (numTanda == 1 || numTanda == 2)
            {
                // Solo para Henderson (Tandas)
                if (b.esHenderson())
                {
                    if (numTanda == 1) 
                    {
                        // Tanda 1: desde día anterior 14:30 hasta hoy 7:00
                        desde = fecha.Date.AddDays(-1).AddHours(14).AddMinutes(30);
                        cierre = fecha.Date.AddHours(7);
                    }
                    else // numTanda == 2
                    {
                        // Tanda 2: desde hoy 7:00 hasta hoy 14:30
                        desde = fecha.Date.AddHours(7);
                        cierre = fecha.Date.AddHours(14).AddMinutes(30);
                    }
                }
                else
                {
                    // Si no es Henderson pero numTanda es 1 o 2, usar todo el día
                    cierre = fecha.Date.AddDays(1).AddSeconds(-1); // Hasta el final del día (23:59:59)
                }
            }
            else
            {
                // ✅ Envío manual (numTanda == 0) o numTanda > 2: mostrar todo el día
                cierre = fecha.Date.AddDays(1).AddSeconds(-1); // Hasta el final del día (23:59:59)
            }

            return (desde, cierre);
        }

        private async Task<List<AcreditacionDTO>> MapearAcreditaciones(SqlDataReader reader, string ncBuzon = null)
        {
            var acreditaciones = new List<AcreditacionDTO>();

            int ncOrd = reader.GetOrdinal("IDBUZON");
            int opOrd = reader.GetOrdinal("IDOPERACION");
            int cuentaOrd = reader.GetOrdinal("IDCUENTA");
            int monOrd = reader.GetOrdinal("MONEDA");
            int montoOrd = reader.GetOrdinal("MONTO");
            int nombreBancoOrd = reader.GetOrdinal("IDBANCO");

            int totalLeidas = 0;
            bool esBuzonEspecifico = ncBuzon == "EA22L0105N12000032";

            while (await reader.ReadAsync())
            {
                totalLeidas++;
                var acc = new AcreditacionDTO
                {
                    NC = reader.GetString(ncOrd)?.Trim(), // ✅ Normalizar espacios
                    IdOperacion = reader.GetInt64(opOrd),
                    IdCuenta = reader.GetInt32(cuentaOrd),
                    Divisa = reader.GetInt32(monOrd),
                    Monto = reader.GetDouble(montoOrd),
                    IdBanco = reader.GetInt32(nombreBancoOrd)
                };
                acc.setMoneda();

                acc.Banco = ServicioBanco.getInstancia().getById(acc.IdBanco);

                acreditaciones.Add(acc);
                
                // ✅ Logging específico para cada acreditación del buzón problemático
                if (esBuzonEspecifico)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Acreditación mapeada | NC: {acc.NC} | IDOPERACION: {acc.IdOperacion} | " +
                        $"IDCUENTA: {acc.IdCuenta} | MONTO: {acc.Monto} | DIVISA: {acc.Divisa} | " +
                        $"IDBANCO: {acc.IdBanco}",
                        "ServicioEnvioAcreditacionManual | MapearAcreditaciones");
                }
            }
            
            // ✅ Logging resumen
            if (esBuzonEspecifico)
            {
                ServicioLog.instancia.WriteInfo(
                    $"Resumen mapeo acreditaciones | NC: {ncBuzon} | " +
                    $"Total leídas: {totalLeidas} | Total mapeadas: {acreditaciones.Count}",
                    "ServicioEnvioAcreditacionManual | MapearAcreditaciones");
            }
            
            return acreditaciones;
        }

        private async Task GenerarReporteYEnviarEmail(BuzonDTO b, DateTime fechaElegida)
        {
            var semaphore = new SemaphoreSlim(initialCount: 20, maxCount: 20);
            MailKit.Net.Smtp.SmtpClient? smtp = null;
            bool shouldSend = true;

            if (AppRuntime.IsTest)
            {
                var settings = AppRuntime.Settings.Email;
                if (!settings.TestAllowSmtp)
                {
                    shouldSend = false; // WebServiceGuard bloquea SMTP: evitamos crear el cliente.
                }
                else
                {
                    smtp = await ServicioEmail.instancia.getNewSmptClient();
                }
            }
            else
            {
                smtp = await ServicioEmail.instancia.getNewSmptClient();
            }

            var sendLock = new SemaphoreSlim(1, 1);

            // 1) Hidratar emails desde CCEMAIL (ServicioCC)
            if (b._Emails == null || b._Emails.Count == 0)
            {
                var buzonCc = ServicioCC.getInstancia()
                    .getBuzones()
                    .FirstOrDefault(e =>
                        string.Equals(e.NC?.Trim(), b.NC?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (buzonCc != null)
                    b._Emails = buzonCc._listaEmails;
            }
            // 2) Si sigue sin mails, no intentes enviar
            if (b._Emails == null || b._Emails.Count == 0)
            {
                ServicioLog.instancia.WriteWarning(
                    $"Buzón [{b.NC}] / [{b.NN}] sin emails configurados en CCEMAIL. No se envía Excel manual.",
                    "ServicioEnvioAcreditacionManual | GenerarReporteYEnviarEmail");
                return;
            }

            // ❌ Antes: var reportService = new ReportService();
            // ✅ Ahora reutilizamos la instancia inyectada:
            var reportService = _reportService;

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
                IdCliente = b.IdCliente, // ⚠️ si lo tenés; útil para COBOE en otros métodos
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

            // Si es Henderson + hay acreditaciones B2B:
            // - el Excel normal debe excluirlas
            // - el Excel B2B separado debe contener solo B2B
            var acreditacionesB2B = b.EsHenderson
                ? b2.Acreditaciones.Where(a => EsEmpresaB2B(a.Empresa)).ToList()
                : new List<AcreditacionDTO2>();
            var acreditacionesNoB2B = b.EsHenderson
                ? b2.Acreditaciones.Where(a => !EsEmpresaB2B(a.Empresa)).ToList()
                : b2.Acreditaciones.ToList();

            if (b.EsHenderson && acreditacionesB2B.Count > 0)
            {
                b2.Acreditaciones = acreditacionesNoB2B;
                b2.MontoTotal = acreditacionesNoB2B.Sum(a => a.Monto);

                ServicioLog.instancia.WriteInfo(
                    $"HENDERSON -> B2B detectadas: {acreditacionesB2B.Count} | Excel normal con no-B2B: {acreditacionesNoB2B.Count}",
                    "ServicioEnvioAcreditacionManual | Henderson B2B");
            }

            Stream excelStream; 

            string subject, body, fileName;

            if (b2.IdCliente == 179)
            {
                // COBOE: usar sucursal mapeada como EMPRESA
                excelStream = reportService.ArmarExcelMasivoParaCoboe(b2, out subject, out body, out fileName);
            }
            else
            {
                // Resto: reporte manual estándar con fecha elegida
                excelStream = reportService.ArmarYEnviarExcelDeUnBuzon(b2, fechaElegida, out subject, out body, out fileName);
            }

            await semaphore.WaitAsync();

            try
            {
                await sendLock.WaitAsync();
                try
                {
                    if (shouldSend)
                    {
                        await ServicioEmail.instancia.EnviarExcelPorMailMasivoConMailKit(
                            excelStream, fileName, subject, body, b._Emails, smtp);
                    }
                    else
                    {
                        ServicioLog.instancia.WriteInfo(
                            $"TEST: SMTP bloqueado, se omite envío mail normal en manual | NC: {b.NC}",
                            "ServicioEnvioAcreditacionManual | Henderson B2B");
                    }

                    // Envío B2B separado (solo Henderson + acreditaciones B2B)
                    if (b.EsHenderson)
                    {
                        if (acreditacionesB2B.Count > 0)
                        {
                            if (IsB2BDestinoConfigurado())
                            {
                                try
                                {
                                    ServicioLog.instancia.WriteInfo(
                                        $"Generando mail B2B separado | NC: {b.NC} | acreditaciones B2B: {acreditacionesB2B.Count}",
                                        "ServicioEnvioAcreditacionManual | Henderson B2B");

                                    var b2BDto = new BuzonDTO2
                                    {
                                        NC = b2.NC,
                                        NN = b2.NN,
                                        Empresa = b2.Empresa,
                                        FechaInicio = b2.FechaInicio,
                                        Cierre = b2.Cierre,
                                        MontoTotal = acreditacionesB2B.Sum(a => a.Monto),
                                        Moneda = b2.Moneda,
                                        Divisa = b2.Divisa,
                                        IdOperacion = b2.IdOperacion,
                                        Sucursal = b2.Sucursal,
                                        IdOperacionFinal = b2.IdOperacionFinal,
                                        IdOperacionInicio = b2.IdOperacionInicio,
                                        NumeroEnvioMasivo = b2.NumeroEnvioMasivo,
                                        UltimaFechaConexion = b2.UltimaFechaConexion,
                                        EsHenderson = b2.EsHenderson,
                                        NombreWS = b2.NombreWS,
                                        IdCliente = b2.IdCliente,
                                        Acreditaciones = acreditacionesB2B
                                    };

                                    Stream? excelStreamB2B;
                                    string? subjectB2B;
                                    string? bodyB2B;
                                    string? fileNameB2B;

                                    excelStreamB2B = b2BDto.IdCliente switch
                                    {
                                        179 => reportService.ArmarExcelMasivoParaCoboe(b2BDto, out subjectB2B, out bodyB2B, out fileNameB2B),
                                        _ => reportService.ArmarYEnviarExcelDeUnBuzon(b2BDto, fechaElegida, out subjectB2B, out bodyB2B, out fileNameB2B)
                                    };

                                    if (excelStreamB2B != null && excelStreamB2B.CanSeek)
                                    {
                                        excelStreamB2B.Position = 0;
                                    }

                                    if (!string.IsNullOrWhiteSpace(fileNameB2B))
                                    {
                                        fileNameB2B = AppendSuffixToFileName(fileNameB2B, "_B2B");
                                    }

                                    var destinosB2B = ParseDestinosB2B();

                                    if (excelStreamB2B != null &&
                                        !string.IsNullOrWhiteSpace(fileNameB2B) &&
                                        subjectB2B != null &&
                                        bodyB2B != null &&
                                        destinosB2B != null &&
                                        destinosB2B.Count > 0)
                                    {
                                        if (shouldSend)
                                        {
                                            await ServicioEmail.instancia.EnviarExcelPorMailMasivoConMailKit(
                                                excelStreamB2B,
                                                fileNameB2B,
                                                subjectB2B,
                                                bodyB2B,
                                                destinosB2B,
                                                smtp);
                                        }
                                        else
                                        {
                                            ServicioLog.instancia.WriteInfo(
                                                $"TEST: SMTP bloqueado, se omite envío mail B2B separado en manual | NC: {b.NC}",
                                                "ServicioEnvioAcreditacionManual | Henderson B2B");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ServicioLog.instancia.WriteLog(
                                        ex,
                                        "Todos",
                                        $"Fallo enviando mail B2B separado en manual | NC: {b.NC}. El mail normal ya fue enviado.");
                                }
                            }
                            else
                            {
                                ServicioLog.instancia.WriteInfo(
                                    $"Se omite mail B2B separado en manual porque B2B_MAIL_DESTINO sigue en modo placeholder | NC: {b.NC}",
                                    "ServicioEnvioAcreditacionManual | Henderson B2B");
                            }
                        }
                    }
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
        }
    }
}


