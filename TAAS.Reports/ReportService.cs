using Microsoft.Reporting.NETCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using SharedDTOs;
using System.Globalization;

namespace TAAS.Reports
{
    public class ReportService
    {
        private readonly IReadOnlyList<SucursalClienteDto> _sucursalesClientes;

        public ReportService(IReadOnlyList<SucursalClienteDto> sucursalesClientes)
        {
            _sucursalesClientes = sucursalesClientes ?? Array.Empty<SucursalClienteDto>();
        }

        public ReportService()
        {

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


        private string MapEmpresaToSucursal(string empresaOriginal, int idCliente /*, string? nc = null*/)
        {
            var dto = _sucursalesClientes.FirstOrDefault(d =>
                d.IdCliente == idCliente &&
                string.Equals(d.Empresa, empresaOriginal, StringComparison.OrdinalIgnoreCase)
            // && (string.IsNullOrEmpty(nc) || string.Equals(d.NC, nc, StringComparison.OrdinalIgnoreCase))
            );
            return dto?.Sucursal ?? empresaOriginal;
        }

        public Stream ArmarExcelConReportViewer(BuzonDTO2 buzonDTO, out string subject, out string body, out string fileName)
        {
            try
            {

                string nombreBuzonParaUsar = buzonDTO.NN;

                if (!string.IsNullOrEmpty(buzonDTO.NombreWS))
                {
                    if (!buzonDTO.NombreWS.Equals("NO_DEFINIDO"))
                    {
                        nombreBuzonParaUsar = buzonDTO.NombreWS;
                    }
                }
                DateTime hoy = DateTime.Today;

                DateTime fechaInicio;

                DateTime fechaCierre;

                if (buzonDTO.EsHenderson && buzonDTO.NumeroEnvioMasivo == 1)
                {
                    int diasARestar = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;

                    var baseDate = hoy.AddDays(-diasARestar);

                    fechaInicio = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, 14, 30, 0);

                    fechaCierre = new DateTime(hoy.Year, hoy.Month, hoy.Day, 7, 0, 0);
                }
                else
                {
                    var horaCierreDto = buzonDTO.Cierre.TimeOfDay;

                    fechaCierre = new DateTime(hoy.Year, hoy.Month, hoy.Day, horaCierreDto.Hours, horaCierreDto.Minutes, 0);

                    if (!buzonDTO.EsHenderson)
                    {
                        int diasARestar = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;

                        var baseDate = hoy.AddDays(-diasARestar);

                        var t = buzonDTO.Cierre.TimeOfDay;

                        fechaInicio = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, t.Hours, t.Minutes, 0);
                    }
                    else
                    {
                        switch (buzonDTO.NumeroEnvioMasivo)
                        {
                            case 2:

                                fechaInicio = new DateTime(hoy.Year, hoy.Month, hoy.Day, 7, 0, 0);

                                break;
                            default:

                                var cierreDto = buzonDTO.Cierre;

                                fechaInicio = new DateTime(cierreDto.Year, cierreDto.Month, cierreDto.Day, cierreDto.Hour, cierreDto.Minute, 0);
                                break;
                        }
                    }
                }

                string inicioStr = fechaInicio.ToString("dd/MM/yyyy HH:mm");

                string cierreStr = fechaCierre.ToString("dd/MM/yyyy HH:mm");

                string fechaRango = $"DEL {inicioStr} AL {cierreStr}";

                string exeFolder = AppContext.BaseDirectory;
                string reportPath = Path.Combine(exeFolder, "Reports", "TotalesyDepositosCC.rdlc");
                if (!File.Exists(reportPath))
                    throw new FileNotFoundException($"No se encontró el informe en: {reportPath}");


                string Formato(double v) =>
                                 v == 0.0
                                     ? v.ToString("0")    // "0" sin decimales
                                     : v.ToString("N2");  // "123,45" con dos decimales


                var totales = buzonDTO.Acreditaciones
                    .GroupBy(a => a.Empresa)
                    .Select(g =>
                    {

                        double sumaPesos = g.Where(a => a.Divisa == 1).Sum(a => (double)a.Monto);
                        double sumaDolares = g.Where(a => a.Divisa == 2).Sum(a => (double)a.Monto);
                        double sumaArg = g.Where(a => a.Divisa == 3).Sum(a => (double)a.Monto);
                        double sumaReales = g.Where(a => a.Divisa == 4).Sum(a => (double)a.Monto);
                        double sumaEuros = g.Where(a => a.Divisa == 5).Sum(a => (double)a.Monto);

                        return new TotalesImprimir
                        {
                            EMPRESA = g.Key,
                            TOTALPESOS = Formato(sumaPesos),
                            TOTALDOLARES = Formato(sumaDolares),
                            TOTALARG = Formato(sumaArg),
                            TOTALREALES = Formato(sumaReales),
                            TOTALEUROS = Formato(sumaEuros),
                        };
                    })
                    .ToList();

                totales.Add(new TotalesImprimir
                {
                    EMPRESA = "TOTAL",
                    TOTALPESOS = totales.Sum(t => double.Parse(t.TOTALPESOS)).ToString("N2"),
                    TOTALDOLARES = totales.Sum(t => double.Parse(t.TOTALDOLARES)).ToString("N2"),
                    TOTALARG = totales.Sum(t => double.Parse(t.TOTALARG)).ToString("N2"),
                    TOTALREALES = totales.Sum(t => double.Parse(t.TOTALREALES)).ToString("N2"),
                    TOTALEUROS = totales.Sum(t => double.Parse(t.TOTALEUROS)).ToString("N2"),
                });



                var depositos = buzonDTO.Acreditaciones
                    .OrderByDescending(a => a.IdOperacion)
                    .Select(a => new DepositosCCImprimir
                    {
                        OPERACION = a.IdOperacion.ToString(),
                        MONEDA = a.Divisa switch
                        {
                            1 => "UYU",
                            2 => "USD",
                            3 => "ARS",
                            4 => "BRL",
                            5 => "EUR",
                            _ => "N/A"
                        },
                        FECHA = a.FechaDep.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
                        TOTAL = a.Monto.ToString("N2"),
                        USUARIO = a.Usuario,
                        EMPRESA = a.Empresa,
                        SUCURSAL = nombreBuzonParaUsar
                    })
                    .ToList();


                var report = new LocalReport();
                using var streamRdlc = File.OpenRead(reportPath);
                report.LoadReportDefinition(streamRdlc);
                report.DisplayName = Path.GetFileNameWithoutExtension(reportPath);

                report.DataSources.Clear();
                report.DataSources.Add(new ReportDataSource("DataSet1", totales));
                report.DataSources.Add(new ReportDataSource("DataSet2", depositos));


                report.SetParameters(new ReportParameter("FECHA1", fechaRango));
                report.SetParameters(new ReportParameter("SUCURSAL", buzonDTO.NN));


                var deviceInfo = @"
<DeviceInfo>
  <SimplePageHeaders>True</SimplePageHeaders>
</DeviceInfo>";
                byte[] excelBytes = report.Render(
                    format: "EXCEL",
                    deviceInfo: deviceInfo,
                    out _, out _, out string fileExt, out _, out _
                );

                // 2) Cárgalo con NPOI para renombrar la hoja
                using var inStream = new MemoryStream(excelBytes);
                var hssf = new NPOI.HSSF.UserModel.HSSFWorkbook(inStream);
                hssf.SetSheetName(0, "TotalesyDepositosCC");

                // 3) Escribe en un MemoryStream **no** dispuesto inmediatamente
                var outStream = new MemoryStream();
                hssf.Write(outStream);
                outStream.Position = 0;

                // 4) Ajusta nombre de archivo, subject, body...
                fileName = $"Acreditacion{buzonDTO.NN}_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.{fileExt}";

                subject = $"Acreditaciones Buzón Inteligente [{buzonDTO.NN}] - {cierreStr}";

                body = $"Acreditaciones del Buzón Inteligente {buzonDTO.NN} del <strong>{fechaRango}</strong>";

                // Agregar información de última conexión del buzón si está disponible
                if (buzonDTO.UltimaFechaConexion != DateTime.MinValue)
                {
                    string fechaUltimaConexionStr = buzonDTO.UltimaFechaConexion.ToString("dd/MM/yyyy HH:mm");
                    body += $"<br/><br/>Por favor, tener en cuenta fecha y hora de última conexión del buzón registrada a las: <strong>{fechaUltimaConexionStr}</strong>";
                }

                // 5) Devuelves el outStream **vivo**, no el viejo excelBytes
                return outStream;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public Stream ArmarYEnviarExcelDeUnBuzon(BuzonDTO2 buzonDTO, DateTime fechaElegida, out string subject, out string body, out string fileName)
        {
            try
            {


                string nombreBuzonParaUsar = buzonDTO.NN;

                if (!string.IsNullOrEmpty(buzonDTO.NombreWS))
                {
                    if (!buzonDTO.NombreWS.Equals("NO_DEFINIDO"))
                    {
                        nombreBuzonParaUsar = buzonDTO.NombreWS;
                    }
                }
                DateTime hoy = fechaElegida;
                DateTime fechaInicio;
                DateTime fechaCierre;

                if (buzonDTO.EsHenderson && buzonDTO.NumeroEnvioMasivo == 1)
                {
                    int diasARestar = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;
                    var baseDate = hoy.AddDays(-diasARestar);
                    fechaInicio = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, 14, 30, 0);
                    fechaCierre = new DateTime(hoy.Year, hoy.Month, hoy.Day, 7, 0, 0);
                }
                else
                {
                    var horaCierreDto = buzonDTO.Cierre.TimeOfDay;
                    fechaCierre = new DateTime(hoy.Year, hoy.Month, hoy.Day, horaCierreDto.Hours, horaCierreDto.Minutes, 0);

                    if (!buzonDTO.EsHenderson)
                    {
                        int diasARestar = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;
                        var baseDate = hoy.AddDays(-diasARestar);
                        var t = buzonDTO.Cierre.TimeOfDay;
                        fechaInicio = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, t.Hours, t.Minutes, 0);
                    }
                    else
                    {
                        switch (buzonDTO.NumeroEnvioMasivo)
                        {
                            case 2:
                                fechaInicio = new DateTime(hoy.Year, hoy.Month, hoy.Day, 7, 0, 0);
                                break;
                            default:
                                var cierreDto = buzonDTO.Cierre;
                                fechaInicio = new DateTime(cierreDto.Year, cierreDto.Month, cierreDto.Day, cierreDto.Hour, cierreDto.Minute, 0);
                                break;
                        }
                    }
                }

                string inicioStr = fechaInicio.ToString("dd/MM/yyyy HH:mm");
                string cierreStr = fechaCierre.ToString("dd/MM/yyyy HH:mm");
                string fechaRango = $"DEL {inicioStr} AL {cierreStr}";

                string exeFolder = AppContext.BaseDirectory;
                string reportPath = Path.Combine(exeFolder, "Reports", "TotalesyDepositosCC.rdlc");
                if (!File.Exists(reportPath))
                    throw new FileNotFoundException($"No se encontró el informe en: {reportPath}");


                string Formato(double v) =>
                                 v == 0.0
                                     ? v.ToString("0")    // "0" sin decimales
                                     : v.ToString("N2");  // "123,45" con dos decimales


                var totales = buzonDTO.Acreditaciones
                    .GroupBy(a => a.Empresa)
                    .Select(g =>
                    {

                        double sumaPesos = g.Where(a => a.Divisa == 1).Sum(a => (double)a.Monto);
                        double sumaDolares = g.Where(a => a.Divisa == 2).Sum(a => (double)a.Monto);
                        double sumaArg = g.Where(a => a.Divisa == 3).Sum(a => (double)a.Monto);
                        double sumaReales = g.Where(a => a.Divisa == 4).Sum(a => (double)a.Monto);
                        double sumaEuros = g.Where(a => a.Divisa == 5).Sum(a => (double)a.Monto);

                        return new TotalesImprimir
                        {
                            EMPRESA = g.Key,
                            TOTALPESOS = Formato(sumaPesos),
                            TOTALDOLARES = Formato(sumaDolares),
                            TOTALARG = Formato(sumaArg),
                            TOTALREALES = Formato(sumaReales),
                            TOTALEUROS = Formato(sumaEuros),
                        };
                    })
                    .ToList();

                totales.Add(new TotalesImprimir
                {
                    EMPRESA = "TOTAL",
                    TOTALPESOS = totales.Sum(t => double.Parse(t.TOTALPESOS)).ToString("N2"),
                    TOTALDOLARES = totales.Sum(t => double.Parse(t.TOTALDOLARES)).ToString("N2"),
                    TOTALARG = totales.Sum(t => double.Parse(t.TOTALARG)).ToString("N2"),
                    TOTALREALES = totales.Sum(t => double.Parse(t.TOTALREALES)).ToString("N2"),
                    TOTALEUROS = totales.Sum(t => double.Parse(t.TOTALEUROS)).ToString("N2"),
                });



                var depositos = buzonDTO.Acreditaciones
                    .OrderByDescending(a => a.IdOperacion)
                    .Select(a => new DepositosCCImprimir
                    {
                        OPERACION = a.IdOperacion.ToString(),
                        MONEDA = a.Divisa switch
                        {
                            1 => "UYU",
                            2 => "USD",
                            3 => "ARS",
                            4 => "BRL",
                            5 => "EUR",
                            _ => "N/A"
                        },
                        FECHA = a.FechaDep.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
                        TOTAL = a.Monto.ToString("N2"),
                        USUARIO = a.Usuario,
                        EMPRESA = a.Empresa,
                        SUCURSAL = nombreBuzonParaUsar
                    })
                    .ToList();


                var report = new LocalReport();
                using var streamRdlc = File.OpenRead(reportPath);
                report.LoadReportDefinition(streamRdlc);
                report.DisplayName = Path.GetFileNameWithoutExtension(reportPath);

                report.DataSources.Clear();
                report.DataSources.Add(new ReportDataSource("DataSet1", totales));
                report.DataSources.Add(new ReportDataSource("DataSet2", depositos));


                report.SetParameters(new ReportParameter("FECHA1", fechaRango));
                report.SetParameters(new ReportParameter("SUCURSAL", buzonDTO.NN));


                var deviceInfo = @"
<DeviceInfo>
  <SimplePageHeaders>True</SimplePageHeaders>
</DeviceInfo>";
                byte[] excelBytes = report.Render(
                    format: "EXCEL",
                    deviceInfo: deviceInfo,
                    out _, out _, out string fileExt, out _, out _
                );

                // 2) Cárgalo con NPOI para renombrar la hoja
                using var inStream = new MemoryStream(excelBytes);
                var hssf = new NPOI.HSSF.UserModel.HSSFWorkbook(inStream);
                hssf.SetSheetName(0, "TotalesyDepositosCC");

                // 3) Escribe en un MemoryStream **no** dispuesto inmediatamente
                var outStream = new MemoryStream();
                hssf.Write(outStream);
                outStream.Position = 0;

                // 4) Ajusta nombre de archivo, subject, body...
                fileName = $"Acreditacion{buzonDTO.NN}_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.{fileExt}";

                subject = $"Acreditaciones Buzón Inteligente [{buzonDTO.NN}] - {cierreStr}";

                body = $"Acreditaciones del Buzón Inteligente {buzonDTO.NN} del <strong>{fechaRango}</strong>";

                // Agregar información de última conexión del buzón si está disponible
                if (buzonDTO.UltimaFechaConexion != DateTime.MinValue)
                {
                    string fechaUltimaConexionStr = buzonDTO.UltimaFechaConexion.ToString("dd/MM/yyyy HH:mm");
                    body += $"<br/><br/>Por favor, tener en cuenta fecha y hora de última conexión del buzón registrada a las: <strong>{fechaUltimaConexionStr}</strong>";
                }

                // 5) Devuelves el outStream **vivo**, no el viejo excelBytes
                return outStream;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public Stream ArmarExcelMasivoParaCoboe(BuzonDTO2 buzonDTO, out string subject, out string body, out string fileName)
        {
            try
            {
                // ===== 1) Nombre visible del buzón =====
                string nombreBuzonParaUsar = buzonDTO.NN;
                if (!string.IsNullOrEmpty(buzonDTO.NombreWS) && !buzonDTO.NombreWS.Equals("NO_DEFINIDO"))
                    nombreBuzonParaUsar = buzonDTO.NombreWS;

                // ===== 2) Ventanas de fechas (igual que original) =====
                DateTime hoy = DateTime.Today;
                DateTime fechaInicio;
                DateTime fechaCierre;

                if (buzonDTO.EsHenderson && buzonDTO.NumeroEnvioMasivo == 1)
                {
                    int diasARestar = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;
                    var baseDate = hoy.AddDays(-diasARestar);
                    fechaInicio = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, 14, 30, 0);
                    fechaCierre = new DateTime(hoy.Year, hoy.Month, hoy.Day, 7, 0, 0);
                }
                else
                {
                    var horaCierreDto = buzonDTO.Cierre.TimeOfDay;
                    fechaCierre = new DateTime(hoy.Year, hoy.Month, hoy.Day, horaCierreDto.Hours, horaCierreDto.Minutes, 0);

                    if (!buzonDTO.EsHenderson)
                    {
                        int diasARestar = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;
                        var baseDate = hoy.AddDays(-diasARestar);
                        var t = buzonDTO.Cierre.TimeOfDay;
                        fechaInicio = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, t.Hours, t.Minutes, 0);
                    }
                    else
                    {
                        switch (buzonDTO.NumeroEnvioMasivo)
                        {
                            case 2:
                                fechaInicio = new DateTime(hoy.Year, hoy.Month, hoy.Day, 7, 0, 0);
                                break;
                            default:
                                var cierreDto = buzonDTO.Cierre;
                                fechaInicio = new DateTime(cierreDto.Year, cierreDto.Month, cierreDto.Day, cierreDto.Hour, cierreDto.Minute, 0);
                                break;
                        }
                    }
                }

                string inicioStr = fechaInicio.ToString("dd/MM/yyyy HH:mm");
                string cierreStr = fechaCierre.ToString("dd/MM/yyyy HH:mm");
                string fechaRango = $"DEL {inicioStr} AL {cierreStr}";

                // ===== 3) Mapeo Empresa -> Sucursal (desde _sucursalesClientes) =====
                string MapEmpresaToSucursal(string empresaOriginal, int idCliente)
                {
                    if (string.IsNullOrWhiteSpace(empresaOriginal)) return empresaOriginal ?? "—";
                    var dto = _sucursalesClientes.FirstOrDefault(d =>
                        d.IdCliente == idCliente &&
                        string.Equals(d.Empresa, empresaOriginal, StringComparison.OrdinalIgnoreCase)
                    );
                    return dto?.Sucursal ?? empresaOriginal;
                }

                // ===== 4) Datasets =====
                string Formato(double v) => v == 0.0 ? v.ToString("0") : v.ToString("N2");

                // Proyección base con empresa real y sucursal mapeada
                var acreditacionesProy = buzonDTO.Acreditaciones.Select(a => new
                {
                    EmpresaReal = string.IsNullOrWhiteSpace(a.Empresa) ? "—" : a.Empresa,                 // p.ej. FARMASHOP / CONCEPTO OM
                    SucursalMap = MapEmpresaToSucursal(a.Empresa ?? "", buzonDTO.IdCliente),             // sucursal del DTO
                    a.Divisa,
                    a.Monto,
                    a.Usuario,
                    a.IdOperacion,
                    a.FechaDep
                })
                .ToList();

                // --- TOTALES: agrupar por (EmpresaReal, SucursalMap) y mostrar "EMPRESA (SUCURSAL)" ---
                var totales = acreditacionesProy
                    .GroupBy(a => new { a.EmpresaReal, a.SucursalMap })
                    .Select(g =>
                    {
                        double sumaPesos = g.Where(a => a.Divisa == 1).Sum(a => (double)a.Monto);
                        double sumaDolares = g.Where(a => a.Divisa == 2).Sum(a => (double)a.Monto);
                        double sumaArg = g.Where(a => a.Divisa == 3).Sum(a => (double)a.Monto);
                        double sumaReales = g.Where(a => a.Divisa == 4).Sum(a => (double)a.Monto);
                        double sumaEuros = g.Where(a => a.Divisa == 5).Sum(a => (double)a.Monto);

                        return new TotalesImprimir
                        {
                            // El RDLC muestra esta columna como "EMPRESA": va "EMPRESA REAL (SUCURSAL)"
                            EMPRESA = $"{g.Key.EmpresaReal} ({g.Key.SucursalMap})",
                            TOTALPESOS = Formato(sumaPesos),
                            TOTALDOLARES = Formato(sumaDolares),
                            TOTALARG = Formato(sumaArg),
                            TOTALREALES = Formato(sumaReales),
                            TOTALEUROS = Formato(sumaEuros),
                        };
                    })
                    .ToList();

                // Fila TOTAL
                double totalPesos = acreditacionesProy.Where(a => a.Divisa == 1).Sum(a => (double)a.Monto);
                double totalDolares = acreditacionesProy.Where(a => a.Divisa == 2).Sum(a => (double)a.Monto);
                double totalArg = acreditacionesProy.Where(a => a.Divisa == 3).Sum(a => (double)a.Monto);
                double totalReales = acreditacionesProy.Where(a => a.Divisa == 4).Sum(a => (double)a.Monto);
                double totalEuros = acreditacionesProy.Where(a => a.Divisa == 5).Sum(a => (double)a.Monto);

                totales.Add(new TotalesImprimir
                {
                    EMPRESA = "TOTAL",
                    TOTALPESOS = totalPesos.ToString("N2"),
                    TOTALDOLARES = totalDolares.ToString("N2"),
                    TOTALARG = totalArg.ToString("N2"),
                    TOTALREALES = totalReales.ToString("N2"),
                    TOTALEUROS = totalEuros.ToString("N2"),
                });

                // --- DETALLE DEPOSITS ---
                // EMPRESA = empresa real; SUCURSAL = sucursal mapeada
                var depositos = acreditacionesProy
                    .OrderByDescending(a => a.IdOperacion)
                    .Select(a => new DepositosCCImprimir
                    {
                        OPERACION = a.IdOperacion.ToString(),
                        MONEDA = a.Divisa switch
                        {
                            1 => "UYU",
                            2 => "USD",
                            3 => "ARS",
                            4 => "BRL",
                            5 => "EUR",
                            _ => "N/A"
                        },
                        FECHA = a.FechaDep.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
                        TOTAL = a.Monto.ToString("N2"),
                        USUARIO = a.Usuario,
                        EMPRESA = a.EmpresaReal,
                        SUCURSAL = a.SucursalMap
                    })
                    .ToList();

                // ===== 5) Render RDLC =====
                string exeFolder = AppContext.BaseDirectory;
                string reportPath = Path.Combine(exeFolder, "Reports", "TotalesyDepositosCC.rdlc");
                if (!File.Exists(reportPath))
                    throw new FileNotFoundException($"No se encontró el informe en: {reportPath}");

                var report = new LocalReport();
                using var streamRdlc = File.OpenRead(reportPath);
                report.LoadReportDefinition(streamRdlc);
                report.DisplayName = Path.GetFileNameWithoutExtension(reportPath);

                report.DataSources.Clear();
                report.DataSources.Add(new ReportDataSource("DataSet1", totales));
                report.DataSources.Add(new ReportDataSource("DataSet2", depositos));

                // Solo los parámetros existentes en el RDLC
                report.SetParameters(new ReportParameter("FECHA1", fechaRango));
                report.SetParameters(new ReportParameter("SUCURSAL", buzonDTO.NN));

                var deviceInfo = @"
<DeviceInfo>
  <SimplePageHeaders>True</SimplePageHeaders>
</DeviceInfo>";

                byte[] excelBytes = report.Render(
                    format: "EXCEL",
                    deviceInfo: deviceInfo,
                    out _, out _, out string fileExt, out _, out _
                );

                using var inStream = new MemoryStream(excelBytes);
                var hssf = new NPOI.HSSF.UserModel.HSSFWorkbook(inStream);
                hssf.SetSheetName(0, "TotalesyDepositosCC");

                var outStream = new MemoryStream();
                hssf.Write(outStream);
                outStream.Position = 0;

                // ===== 6) Mail =====
                fileName = $"Acreditacion{buzonDTO.NN}_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.{fileExt}";
                subject = $"Acreditaciones Buzón Inteligente [{buzonDTO.NN}] - {cierreStr}";
                body = $"Acreditaciones del Buzón Inteligente {buzonDTO.NN} del <strong>{fechaRango}</strong>";

                // Agregar información de última conexión del buzón si está disponible
                if (buzonDTO.UltimaFechaConexion != DateTime.MinValue)
                {
                    string fechaUltimaConexionStr = buzonDTO.UltimaFechaConexion.ToString("dd/MM/yyyy HH:mm");
                    body += $"<br/><br/>Por favor, tener en cuenta fecha y hora de última conexión del buzón registrada a las: <strong>{fechaUltimaConexionStr}</strong>";
                }

                return outStream;
            }
            catch
            {
                throw;
            }
        }

        public Stream ArmarExcelSemanalFrog(
            IEnumerable<FrogAcreditacionSucursalDto> datos,
            DateTime fechaDesde,
            DateTime fechaHasta,
            string buzonNombre,
            DateTime ultimaFechaConexion,
            out string subject,
            out string body,
            out string fileName)
        {
            var lista = datos?.ToList() ?? new List<FrogAcreditacionSucursalDto>();

            // Ordenar por fecha de acreditación y luego por fecha de operación
            lista = lista
                .OrderBy(d => d.FechaAcreditacion)
                .ThenBy(d => d.FechaOperacion)
                .ToList();

            // Crear workbook y hoja
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("AcreditacionesFrog");

            int rowIndex = 0;

            // Estilos básicos
            var headerStyle = workbook.CreateCellStyle();
            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerStyle.SetFont(headerFont);

            var dateStyle = workbook.CreateCellStyle();
            short dateFormatId = workbook.CreateDataFormat().GetFormat("dd/MM/yyyy HH:mm");
            dateStyle.DataFormat = dateFormatId;

            var numberStyle = workbook.CreateCellStyle();
            short numberFormatId = workbook.CreateDataFormat().GetFormat("#,##0.00");
            numberStyle.DataFormat = numberFormatId;

            // Encabezados
            IRow header = sheet.CreateRow(rowIndex++);
            string[] headers = new[]
            {
                "Buzón",
                "Sucursal",
                "Empresa",
                "Operación",
                "Moneda",
                "Fecha Depósito",
                "Fecha Acreditación",
                "Importe Operación",
                "Monto Total Día",
                "Usuario"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = header.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }

            // Filas de datos
            foreach (var d in lista)
            {
                IRow row = sheet.CreateRow(rowIndex++);
                int col = 0;

                row.CreateCell(col++).SetCellValue(d.Buzon ?? string.Empty);
                row.CreateCell(col++).SetCellValue(d.Sucursal ?? string.Empty);
                row.CreateCell(col++).SetCellValue(d.Empresa ?? string.Empty);
                row.CreateCell(col++).SetCellValue(d.ImporteOperacion == 0 ? "" : d.IdOperacion.ToString());

                row.CreateCell(col++).SetCellValue(d.Moneda ?? string.Empty);

                var fechaDepCell = row.CreateCell(col++);
                fechaDepCell.SetCellValue(d.FechaOperacion);
                fechaDepCell.CellStyle = dateStyle;

                var fechaAcredCell = row.CreateCell(col++);
                fechaAcredCell.SetCellValue(d.FechaAcreditacion);
                fechaAcredCell.CellStyle = dateStyle;

                var importeCell = row.CreateCell(col++);
                importeCell.SetCellValue((double)d.ImporteOperacion);
                importeCell.CellStyle = numberStyle;

                var totalDiaCell = row.CreateCell(col++);
                totalDiaCell.SetCellValue((double)d.MontoTotalDia);
                totalDiaCell.CellStyle = numberStyle;

                row.CreateCell(col++).SetCellValue(d.Usuario ?? string.Empty);
            }

            // Autoajustar columnas
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            // Preparar stream
            var stream = new MemoryStream();
            workbook.Write(stream);
            stream.Position = 0;

            string inicioStr = fechaDesde.ToString("dd/MM/yyyy HH:mm");
            string cierreStr = fechaHasta.ToString("dd/MM/yyyy HH:mm");
            string fechaRango = $"DEL {inicioStr} AL {cierreStr}";

            fileName = $"FROG_{buzonNombre}_{fechaDesde:yyyyMMdd}_a_{fechaHasta:yyyyMMdd}.xlsx";

            subject = $"Acreditaciones Buzón Inteligente FROG [{buzonNombre}] - Semana {fechaDesde:dd/MM/yyyy} al {fechaHasta:dd/MM/yyyy}";

            body = $"Acreditaciones del Buzón Inteligente FROG {buzonNombre} del <strong>{fechaRango}</strong>";

            if (ultimaFechaConexion != DateTime.MinValue)
            {
                string fechaUltimaConexionStr = ultimaFechaConexion.ToString("dd/MM/yyyy HH:mm");
                body += $"<br/><br/>Por favor, tener en cuenta fecha y hora de última conexión del buzón registrada a las: <strong>{fechaUltimaConexionStr}</strong>";
            }

            return stream;
        }

    }
}
