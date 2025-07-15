using Microsoft.Reporting.NETCore;
using SharedDTOs;
using System;
using System.Collections.Generic; // Del port ReportViewerCore.NETCore
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace TAAS.Reports
{
    public class ReportService
    {
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
        public Stream ArmarExcelConReportViewer(BuzonDTO2 buzonDTO, out string subject, out string body, out string fileName)
        {

            string nombreBuzonParaUsar = buzonDTO.NN;

            if(!string.IsNullOrEmpty(buzonDTO.NombreWS))
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
            string fechaRango = $"{inicioStr} al {cierreStr}";

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
                    FECHA = a.FechaDep.ToString("yyyy-MM-dd HH:mm:ss"),
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
            report.SetParameters(new ReportParameter("SUCURSAL", nombreBuzonParaUsar));

    
            byte[] excelBytes = report.Render(
                format: "EXCEL",
                deviceInfo: null,
                out string mimeType,
                out string encoding,
                out string fileExt,
                out string[] streams,
                out Warning[] warnings
            );



            fileName = $"Acreditacion{nombreBuzonParaUsar}_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.{fileExt}";

            subject = $"Acreditaciones Buzón Inteligente [{nombreBuzonParaUsar}] - {inicioStr}";

            body = $"Acreditaciones del Buzón Inteligente {nombreBuzonParaUsar} del <strong>{fechaRango}</strong>";

            return new MemoryStream(excelBytes);
        }
    }
}
