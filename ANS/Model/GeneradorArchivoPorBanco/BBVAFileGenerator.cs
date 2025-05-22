using ANS.Model.Interfaces;
using System.IO;
using System.Windows;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class BBVAFileGenerator : IBancoModoAcreditacion
    {
        private const string CuentaTransportadora = "007584652";
        private readonly string rutaBase = @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\BBVA";
        private readonly ConfiguracionAcreditacion configActual;

        private List<CuentaBuzon> buzonesMontevideo = new();
        private List<CuentaBuzon> buzonesMaldonado = new();

        public BBVAFileGenerator(ConfiguracionAcreditacion config)
        {
            configActual = config;
        }

        public async Task GenerarArchivo(List<CuentaBuzon> cuentas)
        {
            OrdenarListasPorCiudad(cuentas);

            int corrMV = ObtenerNumeroCorrelativo(rutaBase, DateTime.Now, "MONTEVIDEO");
            int corrMD = ObtenerNumeroCorrelativo(rutaBase, DateTime.Now, "MALDONADO");

            if (buzonesMontevideo.Any())
            {
                if (configActual.TipoAcreditacion == VariablesGlobales.p2p)
                    await Exporta_Reme(rutaBase, DateTime.Now, corrMV, buzonesMontevideo, "MONTEVIDEO");
                else
                    await Exporta_Reme_Agrupado(rutaBase, DateTime.Now, corrMV, buzonesMontevideo, "MONTEVIDEO");
            }

            if (buzonesMaldonado.Any())
            {
                if (configActual.TipoAcreditacion == VariablesGlobales.p2p)
                    await Exporta_Reme(rutaBase, DateTime.Now, corrMD, buzonesMaldonado, "MALDONADO");
                else
                    await Exporta_Reme_Agrupado(rutaBase, DateTime.Now, corrMD, buzonesMaldonado, "MALDONADO");
            }
        }

        public async Task Exporta_Reme(string ruta, DateTime fecha, int correlativo, List<CuentaBuzon> cuentas, string ciudad)
        {
            try
            {
                if (!Directory.Exists(ruta))
                    Directory.CreateDirectory(ruta);

                double totalUYU = 0, totalUSD = 0, totalEUR = 0, totalARS = 0, totalBRL = 0;
                int cUYU = 0, cUSD = 0, cEUR = 0, cARS = 0, cBRL = 0;
                var lines = new List<string>();

                foreach (var buz in cuentas)
                {
                    if (buz.Depositos == null) continue;
                    foreach (var dep in buz.Depositos)
                    {
                        string suc = FormatString(buz.SucursalCuenta, 3);
                        var parts = buz.Cuenta.Split('-');
                        string cuenta = FormatString(parts[0].Trim(), 9);
                        string sub = FormatString(parts[1].Trim(), 3);
                        string mon = buz.Divisa;
                        string prod = FormatString(buz.Producto.ToString(), 3);
                        string trans = FormatString(CuentaTransportadora, 9);
                        string remito = FormatString(buz.IdReferenciaAlCliente + "X" + dep.IdOperacion, 12);
                        remito = remito.Length > 12 ? remito.Substring(0, 12) : remito;
                        double suma = dep.Totales?.Sum(t => t.ImporteTotal) ?? 0;
                        string monto = FormatAmount(suma.ToString("F2"));

                        lines.Add($"{suc}{cuenta}{mon}{sub}{prod}{trans}{monto}{remito}{mon}");

                        switch (mon)
                        {
                            case "UYU": totalUYU += suma; cUYU++; break;
                            case "USD": totalUSD += suma; cUSD++; break;
                            case "EUR": totalEUR += suma; cEUR++; break;
                            case "ARS": totalARS += suma; cARS++; break;
                            case "BRL": totalBRL += suma; cBRL++; break;
                        }
                    }
                }

                string f = fecha.ToString("yyyyMMdd");
                string suf = ciudad.ToUpper() == "MALDONADO" ? "MAL" : string.Empty;
                string nombreA = $"REME{f}{correlativo:D3}{suf}.txt";
                string nombreB = $"FREME{f}{correlativo:D3}{suf}.txt";
                string pathA = Path.Combine(ruta, nombreA);
                string pathB = Path.Combine(ruta, nombreB);

                using (var sw = new StreamWriter(pathA))
                {
                    foreach (var ln in lines)
                        sw.WriteLine(ln);
                    sw.WriteLine(GenerateTotalLine("UYU", cUYU, totalUYU));
                    sw.WriteLine(GenerateTotalLine("USD", cUSD, totalUSD));
                    sw.WriteLine(GenerateTotalLine("EUR", cEUR, totalEUR));
                    sw.WriteLine(GenerateTotalLine("ARS", cARS, totalARS));
                    sw.WriteLine(GenerateTotalLine("BRL", cBRL, totalBRL));
                }
                using (var sw2 = new StreamWriter(pathB)) { }

              //  MessageBox.Show("Datos exportados correctamente.", "EXPORTAR", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"ERROR, VERIFIQUE: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<bool> Exporta_Reme_Agrupado(string rutaBase, DateTime fecha, int correlativo, List<CuentaBuzon> cuentas, string ciudad)
        {
            try
            {
                if (!Directory.Exists(rutaBase))
                    Directory.CreateDirectory(rutaBase);

                string f = fecha.ToString("yyyyMMdd");
                string suf = ciudad.ToUpper() == "MALDONADO" ? "MAL" : string.Empty;
                string nombreA = $"REME{f}{correlativo:D3}{suf}.txt";
                string nombreB = $"FREME{f}{correlativo:D3}{suf}.txt";
                string pathA = Path.Combine(rutaBase, nombreA);
                string pathB = Path.Combine(rutaBase, nombreB);

                // Agrupación y orden similar al código antiguo
                var grupos = cuentas
                    .SelectMany(b => b.Depositos ?? new List<Deposito>(), (b, dep) => new { b, dep, parts = b.Cuenta.Split('-') })
                    .GroupBy(x => new {
                        x.b.SucursalCuenta,
                        Cuenta = x.parts[0].Trim(),
                        x.b.Divisa,
                        CuentaTransportadora,
                        SubCuenta = x.parts[1].Trim(),
                        Remito = x.dep.IdOperacion.ToString(),
                        x.b.Producto
                    })
                    .Select(g => new {
                        Sucursal = g.Key.SucursalCuenta,
                        Cuenta = g.Key.Cuenta,
                        Moneda = g.Key.Divisa,
                        CuentaTransportadora = g.Key.CuentaTransportadora,
                        SubCuenta = g.Key.SubCuenta,
                        RemitoOriginal = g.Key.Remito,
                        Producto = g.Key.Producto,
                        SumaMontos = g.Sum(x => x.dep.Totales.Sum(t => t.ImporteTotal))
                    })
                    .OrderBy(x => x.Sucursal)
                    .ThenBy(x => x.Cuenta)
                    .ThenBy(x => x.Moneda)
                    .ThenBy(x => x.SubCuenta)
                    .ThenBy(x => x.Producto)
                    .ThenBy(x => x.RemitoOriginal)
                    .ToList();

                var lines = new List<string>();
                double totalUYU = 0, totalUSD = 0, totalEUR = 0, totalARS = 0, totalBRL = 0;
                int cUYU = 0, cUSD = 0, cEUR = 0, cARS = 0, cBRL = 0;

                foreach (var g in grupos)
                {
                    string suc = FormatString(g.Sucursal, 3);
                    string cue = FormatString(g.Cuenta, 9);
                    string mon = g.Moneda;
                    string sub = FormatString(g.SubCuenta, 3);
                    string trans = FormatString(g.CuentaTransportadora, 9);
                    string rem = FormatString(g.RemitoOriginal, 4);
                    string hora = DateTime.Now.ToString("HHmmssff");
                    rem = (rem + hora).Length > 12 ? (rem + hora).Substring(0, 12) : (rem + hora);
                    string monto = FormatAmount(g.SumaMontos.ToString("F2"));

                    lines.Add($"{suc}{cue}{mon}{sub}{g.Producto}{trans}{monto}{rem}{mon}");
                    switch (mon)
                    {
                        case "UYU": totalUYU += g.SumaMontos; cUYU++; break;
                        case "USD": totalUSD += g.SumaMontos; cUSD++; break;
                        case "EUR": totalEUR += g.SumaMontos; cEUR++; break;
                        case "ARS": totalARS += g.SumaMontos; cARS++; break;
                        case "BRL": totalBRL += g.SumaMontos; cBRL++; break;
                    }
                }

                lines.Add(GenerateTotalLine("UYU", cUYU, totalUYU));
                lines.Add(GenerateTotalLine("USD", cUSD, totalUSD));
                lines.Add(GenerateTotalLine("EUR", cEUR, totalEUR));
                lines.Add(GenerateTotalLine("ARS", cARS, totalARS));
                lines.Add(GenerateTotalLine("BRL", cBRL, totalBRL));

                using (var sw = new StreamWriter(pathA))
                    foreach (var ln in lines) sw.WriteLine(ln);

                using (var sw2 = new StreamWriter(pathB)) { }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR en acreditación BBVA, VERIFIQUE: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        #region Métodos auxiliares
        private string FormatString(string input, int len) => input?.Replace(".", "").Replace(",", "").Replace("-", "").PadLeft(len, '0');
        private string FormatAmount(string amt) => amt.Replace(".", "").Replace(",", "").PadLeft(15, '0');
        private string GenerateTotalLine(string cur, int cnt, double tot)
            => $"T{cnt.ToString().PadLeft(4, '0')}{tot.ToString("N").Replace(".", "").Replace(",", "").PadLeft(15, '0')}{cur}";
        private void OrdenarListasPorCiudad(IEnumerable<CuentaBuzon> list)
        {
            buzonesMontevideo.Clear(); buzonesMaldonado.Clear();
            foreach (var cb in list)
            {
                if (cb.Ciudad?.Equals("MONTEVIDEO", StringComparison.OrdinalIgnoreCase) == true)
                    buzonesMontevideo.Add(cb);
                else if (cb.Ciudad?.Equals("MALDONADO", StringComparison.OrdinalIgnoreCase) == true)
                    buzonesMaldonado.Add(cb);
            }
        }
        public int ObtenerNumeroCorrelativo(string ruta, DateTime fecha, string ciudad)
        {
            if (!Directory.Exists(ruta)) Directory.CreateDirectory(ruta);
            string f = fecha.ToString("yyyyMMdd");
            string pat = ciudad.ToUpper() == "MALDONADO" ? $"FREME{f}*MAL.txt" : $"FREME{f}*.txt";
            int max = 0;
            foreach (var file in Directory.GetFiles(ruta, pat))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (name.Length >= 16 && int.TryParse(name.Substring(13, 3), out int c) && c > max) max = c;
            }
            return max + 1;
        }
        #endregion
    }
}