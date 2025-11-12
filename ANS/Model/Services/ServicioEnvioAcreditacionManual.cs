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

using Microsoft.Data.SqlClient;
using SharedDTOs;
using System.IO;
using TAAS.Reports;

namespace ANS.Model.Services
{
    public class ServicioEnvioAcreditacionManual
    {
        public static ServicioEnvioAcreditacionManual instancia;

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
            if (instancia == null)
            {
                instancia = new ServicioEnvioAcreditacionManual();
            }
            return instancia;
        }

        public async Task EnviarAcreditacionManual(BuzonDTO buzon, int numTanda, DateTime fecha)
        {
            try
            {
                await GetAcreditacionesByBuzonTandaYFecha(buzon, numTanda, fecha);

                if (buzon.Acreditaciones == null || !buzon.Acreditaciones.Any())
                    return;

                var buzones = new List<BuzonDTO> { buzon };

                // Completa Usuario/FechaDep/Empresa en las acreditaciones
                foreach(var acc in buzon.Acreditaciones)
                {
                    if (acc.Banco.NombreBanco.ToUpper() == VariablesGlobales.scotiabank.ToUpper())
                    {
                        Console.WriteLine(acc.Empresa);
                    }
                }
                await ServicioEnvioMasivo.getInstancia().obtenerUsuarioYFechaDelDeposito(buzones);

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

                using (var conn = new SqlConnection(ConfiguracionGlobal.Conexion22))
                {
                    await conn.OpenAsync();

                    using (var cmd = ArmarCommand(conn, buzon, numTanda, fecha))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var acreditaciones = await MapearAcreditaciones(reader);
                        buzon.Acreditaciones = acreditaciones;
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

            var sql = @"
                SELECT  a.IDBUZON, a.IDOPERACION, a.IDCUENTA, a.MONEDA, a.MONTO, a.FECHA, a.IDBANCO 
                FROM    AcreditacionDepositoDiegoTest a
                JOIN    cc c
                        ON LTRIM(RTRIM(a.IDBUZON)) = LTRIM(RTRIM(c.nc))
                WHERE   LTRIM(RTRIM(a.IDBUZON)) = LTRIM(RTRIM(@NC))
                    AND a.FECHA >= @Desde
                    AND a.FECHA <= @Cierre
                    AND c.CIERRE <= @Cierre
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

            if (b.esHenderson())
            {
                if (numTanda == 1) cierre = fecha.Date.AddHours(7);
                else if (numTanda == 2) cierre = fecha.Date.AddHours(14).AddMinutes(30);
                else throw new Exception("NumTanda inválido. Debe ser 1 o 2 para Henderson.");
            }
            else
            {
                cierre = fecha.Date.AddHours(b.Cierre.Hour).AddMinutes(b.Cierre.Minute);
            }

            return (desde, cierre);
        }

        private async Task<List<AcreditacionDTO>> MapearAcreditaciones(SqlDataReader reader)
        {
            var acreditaciones = new List<AcreditacionDTO>();

            int ncOrd = reader.GetOrdinal("IDBUZON");
            int opOrd = reader.GetOrdinal("IDOPERACION");
            int cuentaOrd = reader.GetOrdinal("IDCUENTA");
            int monOrd = reader.GetOrdinal("MONEDA");
            int montoOrd = reader.GetOrdinal("MONTO");
            int nombreBancoOrd = reader.GetOrdinal("IDBANCO");

            while (await reader.ReadAsync())
            {
                var acc = new AcreditacionDTO
                {
                    NC = reader.GetString(ncOrd),
                    IdOperacion = reader.GetInt64(opOrd),
                    IdCuenta = reader.GetInt32(cuentaOrd),
                    Divisa = reader.GetInt32(monOrd),
                    Monto = reader.GetDouble(montoOrd),
                    IdBanco = reader.GetInt32(nombreBancoOrd)
                };
                acc.setMoneda();

                acc.Banco = ServicioBanco.getInstancia().getById(acc.IdBanco);

                acreditaciones.Add(acc);
            }
            return acreditaciones;
        }

        private async Task GenerarReporteYEnviarEmail(BuzonDTO b, DateTime fechaElegida)
        {
            var semaphore = new SemaphoreSlim(initialCount: 20, maxCount: 20);
            var smtp = await ServicioEmail.instancia.getNewSmptClient();
            var sendLock = new SemaphoreSlim(1, 1);

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
                    await ServicioEmail.instancia.EnviarExcelPorMailMasivoConMailKit(
                        excelStream, fileName, subject, body, b._Emails, smtp);
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


