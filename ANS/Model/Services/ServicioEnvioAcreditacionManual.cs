using Microsoft.Data.SqlClient;
using SharedDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAAS.Reports;

namespace ANS.Model.Services
{
    public class ServicioEnvioAcreditacionManual
    {
        public static ServicioEnvioAcreditacionManual instancia;
        public ServicioEmail _emailService { get; set; } = ServicioEmail.getInstancia();
        public ReportService _reportService = new TAAS.Reports.ReportService();

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

                if(buzon.Acreditaciones == null || !buzon.Acreditaciones.Any())
                {
                    return;
                }

                List<BuzonDTO> buzones = new List<BuzonDTO>();

                buzones.Add(buzon);

                await ServicioEnvioMasivo.getInstancia().obtenerUsuarioYFechaDelDeposito(buzones);

                await GenerarReporteYEnviarEmail(buzon,fecha);
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
            catch (Exception ex)
            {
                //ServicioLog.instancia.WriteLog(ex, $"{buzon.NC}", "- Error al obtener acreditaciones");
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private SqlCommand ArmarCommand(SqlConnection conn, BuzonDTO b, int numTanda, DateTime fecha)
        {

            try
            {

                var (desde, cierre) = CalcularVentana(b, numTanda, fecha);

                var sqlobsolete =
                        @"
                        SELECT  a.IDBUZON, a.IDOPERACION, a.IDCUENTA, a.MONEDA, a.MONTO, a.FECHA
                        FROM    ACREDITACIONESDEPOSITOS a
                        JOIN cc c ON a.IDBUZON = c.nc 
                        WHERE   a.IDBUZON = @NC
                        AND a.FECHA >= @Desde
                        AND a.FECHA <= @Cierre
                        AND c.CIERRE <= @Cierre  
                        ORDER BY a.IDOPERACION DESC;";

                var sql = @"


                            SELECT  a.IDBUZON, a.IDOPERACION, a.IDCUENTA, a.MONEDA, a.MONTO, a.FECHA
                            FROM    AcreditacionDepositoDiegoTest a
                            JOIN    cc c
                                    ON LTRIM(RTRIM(a.IDBUZON)) = LTRIM(RTRIM(c.nc))
                            WHERE   LTRIM(RTRIM(a.IDBUZON)) = LTRIM(RTRIM(@NC))
                                AND   a.FECHA >= @Desde
                                AND   a.FECHA <= @Cierre
                                AND   c.CIERRE <= @Cierre
                            ORDER BY a.IDOPERACION DESC;";

                var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add(new SqlParameter("@NC", System.Data.SqlDbType.VarChar, 50) { Value = b.NC ?? (object)DBNull.Value });

                cmd.Parameters.Add(new SqlParameter("@Desde", System.Data.SqlDbType.DateTime) { Value = desde });

                cmd.Parameters.Add(new SqlParameter("@Cierre", System.Data.SqlDbType.DateTime) { Value = cierre });

                return cmd;
            }
            catch (Exception ex)
            {
                //ServicioLog.instancia.WriteLog(ex, $"{b.NC}", "- Error al armar el comando SQL");
                Console.WriteLine(ex.Message);
                throw;
            }

        }

        private (DateTime desde, DateTime cierre) CalcularVentana(BuzonDTO b, int numTanda, DateTime fecha)
        {
            var desde = fecha.Date;

            DateTime cierre;

            if (b.esHenderson())
            {
                if (numTanda == 1)
                {

                    cierre = fecha.Date.AddHours(7);
                }
                else if (numTanda == 2)
                {

                    cierre = fecha.Date.AddHours(14).AddMinutes(30);
                }
                else
                {
                    throw new Exception("NumTanda inválido. Debe ser 1 o 2 para Henderson.");
                }
            }

            else
            {

                cierre = fecha.Date.AddHours(b.Cierre.Hour).AddMinutes(b.Cierre.Minute);

            }

            return (desde, cierre);
        }

        private async Task<List<AcreditacionDTO>> MapearAcreditaciones(SqlDataReader reader)
        {

            List<AcreditacionDTO> acreditaciones = new List<AcreditacionDTO>();

            // -- ORDINALS -- //
            int ncOrd = reader.GetOrdinal("IDBUZON");
            int opOrd = reader.GetOrdinal("IDOPERACION");
            int cuentaOrd = reader.GetOrdinal("IDCUENTA");
            int monOrd = reader.GetOrdinal("MONEDA");
            int montoOrd = reader.GetOrdinal("MONTO");


            // -- READ & MAP ACCREDITATIONS -- //
            while (await reader.ReadAsync())
            {
                var acc = new AcreditacionDTO
                {
                    NC = reader.GetString(ncOrd),
                    IdOperacion = reader.GetInt64(opOrd),
                    IdCuenta = reader.GetInt32(cuentaOrd),
                    Divisa = reader.GetInt32(monOrd),
                    Monto = reader.GetDouble(montoOrd)
                };
                acc.setMoneda();
                acreditaciones.Add(acc);
            }
            return acreditaciones;
        }

        private async Task GenerarReporteYEnviarEmail(BuzonDTO b,DateTime fechaElegida)
        {
 
            var semaphore = new SemaphoreSlim(initialCount: 20, maxCount: 20);

            var smtp = await ServicioEmail.instancia.getNewSmptClient();

            var sendLock = new SemaphoreSlim(1, 1);

            var reportService = new ReportService();

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

            var excelStream = reportService.ArmarYEnviarExcelDeUnBuzon(b2,fechaElegida, out var subject, out var body, out var fileName);

            await semaphore.WaitAsync();

            try
            {
     
                await sendLock.WaitAsync();

                try
                {

                    await ServicioEmail.instancia
                        .EnviarExcelPorMailMasivoConMailKit(
                           excelStream, fileName, subject, body, b._Emails, smtp);

                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Error al enviar el correo: {ex.Message}");
                }

                finally
                {

                    sendLock.Release();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al esperar el semáforo: {ex.Message}");
            }
            finally
            {
                semaphore.Release();

            }
        }
    }
}

