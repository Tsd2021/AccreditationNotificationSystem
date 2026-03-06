using Microsoft.Data.SqlClient;
using SharedDTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ANS.Model.Services
{
    public class ServicioEnvioSemanalFrog
    {
        private readonly ServicioEnvioMasivo _servicioEnvioMasivo = ServicioEnvioMasivo.getInstancia();
        private readonly ServicioEmail _emailService = ServicioEmail.getInstancia();

        private (DateTime desde, DateTime hasta) CalcularRangoSemana(DateTime fechaReferencia)
        {
            var fecha = fechaReferencia.Date;
            int diffToMonday = ((int)fecha.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var lunes = fecha.AddDays(-diffToMonday);
            var viernes = lunes.AddDays(4);

            DateTime desde = new DateTime(lunes.Year, lunes.Month, lunes.Day, 0, 0, 0);
            DateTime hasta = new DateTime(viernes.Year, viernes.Month, viernes.Day, 23, 59, 59);

            return (desde, hasta);
        }

        public async Task ProcesarSemanaAsync(DateTime fechaReferencia)
        {
            var (desde, hasta) = CalcularRangoSemana(fechaReferencia);

            ServicioLog.instancia.WriteInfo(
                $"Iniciando envío semanal FROG | Desde: {desde:yyyy-MM-dd HH:mm:ss} | Hasta: {hasta:yyyy-MM-dd HH:mm:ss}",
                "ServicioEnvioSemanalFrog | ProcesarSemanaAsync");

            var buzonesFrog = await ObtenerBuzonesFrogAsync();

            if (buzonesFrog.Count == 0)
            {
                ServicioLog.instancia.WriteInfo(
                    "No se encontraron buzones FROG para procesar en el envío semanal.",
                    "ServicioEnvioSemanalFrog | ProcesarSemanaAsync");
                return;
            }

            await _servicioEnvioMasivo.hidratarDTOconSusAcreditacionesPorRango(buzonesFrog, desde, hasta);
            await _servicioEnvioMasivo.obtenerUsuarioYFechaDelDeposito(buzonesFrog);
            await _servicioEnvioMasivo.obtenerFechaUltimaConexionDelBuzon(buzonesFrog);

            ObtenerMailsPorBuzon(buzonesFrog);

            var datosPorBuzon = ConstruirDtosPorBuzon(buzonesFrog);

            using var smtp = await _emailService.getNewSmptClient();

            foreach (var kvp in datosPorBuzon)
            {
                string buzonNombre = kvp.Key;
                var datos = kvp.Value;

                if (datos.Count == 0)
                    continue;

                var primeraFila = datos[0];
                var buzonOriginal = buzonesFrog.FirstOrDefault(b => string.Equals(b.NN, buzonNombre, StringComparison.OrdinalIgnoreCase))
                                    ?? buzonesFrog.First();

                string subject;
                string body;
                string fileName;

                var reportService = new TAAS.Reports.ReportService();

                Stream excelStream = reportService.ArmarExcelSemanalFrog(
                    datos,
                    desde,
                    hasta,
                    buzonNombre,
                    buzonOriginal.UltimaFechaConexion,
                    out subject,
                    out body,
                    out fileName);

                if (buzonOriginal._Emails == null || buzonOriginal._Emails.Count == 0)
                {
                    ServicioLog.instancia.WriteWarning(
                        $"Buzón FROG [{buzonNombre}] sin destinatarios de email configurados. No se envía Excel.",
                        "ServicioEnvioSemanalFrog | ProcesarSemanaAsync");
                    continue;
                }

                await _emailService.EnviarExcelPorMailMasivoConMailKit(
                    excelStream,
                    fileName,
                    subject,
                    body,
                    buzonOriginal._Emails,
                    smtp);
            }

            ServicioLog.instancia.WriteInfo(
                $"Envío semanal FROG completado correctamente. Buzones procesados: {datosPorBuzon.Count}",
                "ServicioEnvioSemanalFrog | ProcesarSemanaAsync");
        }

        private async Task<List<BuzonDTO>> ObtenerBuzonesFrogAsync()
        {
            var resultado = new List<BuzonDTO>();

            string query = @"SELECT c.NC, c.NN, c.SUCURSAL, c.CIERRE, c.IDCLIENTE, ws.NombreWS
                             FROM cc AS c
                             LEFT JOIN cc_nombrews AS ws ON ws.NC = c.NC
                             WHERE c.estado = 'alta'
                             AND c.IDCLIENTE = 160";

            using (var conn = new SqlConnection(ConfiguracionGlobal.Conexion22))
            {
                await conn.OpenAsync();

                using var cmd = new SqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                int ncOrdinal = reader.GetOrdinal("NC");
                int nnOrdinal = reader.GetOrdinal("NN");
                int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");
                int cierreOrdinal = reader.GetOrdinal("CIERRE");
                int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");
                int nombreWSOrdinal = reader.GetOrdinal("NombreWS");

                while (await reader.ReadAsync())
                {
                    var dto = new BuzonDTO
                    {
                        NC = reader.GetString(ncOrdinal)?.Trim(),
                        NN = reader.GetString(nnOrdinal),
                        Sucursal = reader.GetString(sucursalOrdinal),
                        Cierre = reader.GetDateTime(cierreOrdinal),
                        Email = "acreditaciones@tecnisegur.com.uy",
                        IdCliente = reader.GetInt32(idClienteOrdinal),
                        NumeroEnvioMasivo = 0
                    };

                    dto.EsHenderson = dto.esHenderson();

                    if (!reader.IsDBNull(nombreWSOrdinal))
                    {
                        dto.NombreWS = reader.GetString(nombreWSOrdinal);
                    }
                    else
                    {
                        dto.NombreWS = "NO_DEFINIDO";
                    }

                    resultado.Add(dto);
                }
            }

            ServicioLog.instancia.WriteInfo(
                $"Buzones FROG encontrados para envío semanal: {resultado.Count}",
                "ServicioEnvioSemanalFrog | ObtenerBuzonesFrogAsync");

            return resultado;
        }

        private void ObtenerMailsPorBuzon(List<BuzonDTO> buzones)
        {
            foreach (var b in buzones)
            {
                foreach (var e in ServicioCC.getInstancia().getBuzones())
                {
                    string ncBuzon = b.NC?.Trim() ?? b.NC;
                    string ncServicioCC = e.NC?.Trim() ?? e.NC;

                    if (ncBuzon == ncServicioCC)
                    {
                        b._Emails = e._listaEmails;
                        break;
                    }
                }
            }
        }

        private Dictionary<string, List<FrogAcreditacionSucursalDto>> ConstruirDtosPorBuzon(List<BuzonDTO> buzonesFrog)
        {
            var resultado = new Dictionary<string, List<FrogAcreditacionSucursalDto>>(StringComparer.OrdinalIgnoreCase);

            foreach (var buzon in buzonesFrog)
            {
                if (buzon.Acreditaciones == null || buzon.Acreditaciones.Count == 0)
                    continue;

                string buzonNombre = buzon.NN;

                var agrupadoPorDia = buzon.Acreditaciones
                    .GroupBy(a => a.FechaAcreditacion.Date)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(x => (decimal)x.Monto));

                var lista = new List<FrogAcreditacionSucursalDto>();

                foreach (var a in buzon.Acreditaciones)
                {
                    DateTime fechaAcred = a.FechaAcreditacion.Date;
                    agrupadoPorDia.TryGetValue(fechaAcred, out decimal montoTotalDia);

                    var dto = new FrogAcreditacionSucursalDto
                    {
                        Buzon = buzonNombre,
                        NC = buzon.NC,
                        Sucursal = buzon.Sucursal,
                        FechaOperacion = a.FechaDep,
                        FechaAcreditacion = a.FechaAcreditacion,
                        Moneda = a.Moneda,
                        Divisa = a.Divisa,
                        ImporteOperacion = (decimal)a.Monto,
                        MontoTotalDia = montoTotalDia,
                        Empresa = a.Empresa,
                        Usuario = a.Usuario
                    };

                    lista.Add(dto);
                }

                if (!resultado.ContainsKey(buzonNombre))
                {
                    resultado[buzonNombre] = new List<FrogAcreditacionSucursalDto>();
                }

                resultado[buzonNombre].AddRange(lista);
            }

            return resultado;
        }
    }
}

