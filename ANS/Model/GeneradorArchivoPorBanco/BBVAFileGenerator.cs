using ANS.Model.Interfaces;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class BBVAFileGenerator : IBancoModoAcreditacion
    {
        // ====== Layout fijo BBVA ======
        // Detalle (58 cols)
        private const int LEN_SUC = 3;    // 1-3   Sucursal (N, 0-fill)
        private const int LEN_CTA = 9;    // 4-12  Cuenta   (N, 0-fill)
        private const int LEN_MON = 3;    // 13-15 Moneda   (texto 3)
        private const int LEN_SUB = 3;    // 16-18 Subcta   (N, 0-fill)
        private const int LEN_FLAG = 1;   // 19    Producto (fijo '1' = CC)
        private const int LEN_TRANS = 9;  // 20-28 Transportadora (N, 0-fill)
        private const int LEN_MONTO = 15; // 29-43 Monto (N(15,2) centavos)
        private const int LEN_REMITO = 12;// 44-55 Remito (alfa-num 12)
        private const int LEN_DETALLE = 58; // 56-58 Moneda saldo (3) => total 58

        // Totales (26 cols exactas)
        private const int LEN_TOTALES = 26;

        private const char PRODUCTO_FIJO = '1'; // 1 = CC (columna 19)
        private const string CuentaTransportadora = "7584652"; // se pad-left a 9

        private readonly string rutaBaseProduccion = @"\\192.168.0.9\bbva\SALIDA";
        private readonly string rutaBaseTest = @"C:\Users\dchiquiar.ABUDIL\Desktop\test local";

        public Task RunBbvaLocalTestsAsync() => correrTestBBVA();

        // centraliza la elección de la ruta
        private string GetRutaSalida(bool modoPrueba) => modoPrueba ? rutaBaseTest : rutaBaseProduccion;

        private readonly ConfiguracionAcreditacion configActual;
        private readonly List<CuentaBuzon> buzonesMontevideo = new();
        private readonly List<CuentaBuzon> buzonesMaldonado = new();

        public BBVAFileGenerator(ConfiguracionAcreditacion config) => configActual = config;
        public BBVAFileGenerator() { }

        public async Task generarArchivoTest()
        {
            string ruta = rutaBaseTest;

            if (string.IsNullOrWhiteSpace(ruta))
                throw new InvalidOperationException("rutaBaseTest no está configurada.");

            Directory.CreateDirectory(ruta);

            string baseName = "ignorar";
            string extension = ".txt";
            int n = 0;

            while (true)
            {
                string fileName = n == 0 ? $"{baseName}{extension}" : $"{baseName}{n}{extension}";
                string path = Path.Combine(ruta, fileName);

                try
                {
                    using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    using (var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
                    {
                        await sw.WriteAsync("TEST - PRUEBA").ConfigureAwait(false);
                    }
                    break; // éxito
                }
                catch (IOException)
                {
                    n++; // ya existe → probá con siguiente número
                    continue;
                }
            }
        }

        // ===== método privado de prueba =====
        private async Task correrTestBBVA()
        {
            string ruta = GetRutaSalida(modoPrueba: true);
            Directory.CreateDirectory(ruta);

            // === MONTEVIDEO: 2 cuentas UYU (=> 2 detalles) ===
            var ctaMvd1 = new CuentaBuzon
            {
                Ciudad = "MONTEVIDEO",
                SucursalCuenta = "050",
                Cuenta = "111111111-001",
                Moneda = "PESOS",
                Divisa = "UYU",
                IdReferenciaAlCliente = "TMV1",
                Depositos = new List<Deposito>
                {
                    new Deposito { IdOperacion=1001, Totales = new List<Total>{ new Total{ Divisa="UYU", ImporteTotal=150000 } } },
                    new Deposito { IdOperacion=1002, Totales = new List<Total>{ new Total{ Divisa="UYU", ImporteTotal=250000 } } },
                }
            };

            var ctaMvd2 = new CuentaBuzon
            {
                Ciudad = "MONTEVIDEO",
                SucursalCuenta = "050",
                Cuenta = "222222222-001",
                Moneda = "PESOS",
                Divisa = "UYU",
                IdReferenciaAlCliente = "TMV2",
                Depositos = new List<Deposito>
                {
                    new Deposito { IdOperacion=2001, Totales = new List<Total>{ new Total{ Divisa="UYU", ImporteTotal=300000 } } },
                    new Deposito { IdOperacion=2002, Totales = new List<Total>{ new Total{ Divisa="UYU", ImporteTotal=120000 } } },
                    new Deposito { IdOperacion=2003, Totales = new List<Total>{ new Total{ Divisa="UYU", ImporteTotal= 80000 } } },
                }
            };
            var cuentasMvd = new List<CuentaBuzon> { ctaMvd1, ctaMvd2 };

            // === MALDONADO: 1 cuenta UYU y otra USD (=> 2 detalles) ===
            var ctaMalUYU = new CuentaBuzon
            {
                Ciudad = "MALDONADO",
                SucursalCuenta = "026",
                Cuenta = "111111111-001",
                Moneda = "PESOS",
                Divisa = "UYU",
                IdReferenciaAlCliente = "TMAU",
                Depositos = new List<Deposito>
                {
                    new Deposito { IdOperacion=3001, Totales = new List<Total> { new Total{ Divisa="UYU", ImporteTotal= 50000 } } },
                    new Deposito { IdOperacion=3002, Totales = new List<Total> { new Total{ Divisa="UYU", ImporteTotal= 80000 } } },
                }
            };

            var ctaMalUSD = new CuentaBuzon
            {
                Ciudad = "MALDONADO",
                SucursalCuenta = "026",
                Cuenta = "111111111-001",
                Moneda = "DOLARES",
                Divisa = "USD",
                IdReferenciaAlCliente = "TMAU",
                Depositos = new List<Deposito>
                {
                    new Deposito { IdOperacion=3001, Totales = new List<Total> { new Total{ Divisa="USD", ImporteTotal=  700 } } },
                    new Deposito { IdOperacion=3002, Totales = new List<Total> { new Total{ Divisa="USD", ImporteTotal= 1300 } } },
                }
            };
            var cuentasMal = new List<CuentaBuzon> { ctaMalUYU, ctaMalUSD };

            var ahora = DateTime.Now;

            // Genera REME/FREME de Montevideo
            await Exporta_Reme_Agrupado(ruta, ahora, cuentasMvd, "MONTEVIDEO");

            // Genera REME/FREME de Maldonado
            await Exporta_Reme_Agrupado(ruta, ahora, cuentasMal, "MALDONADO");

            // Archivo ignora*.txt de smoke test
            await generarArchivoTest();
        }

        public async Task GenerarArchivoPrueba(List<CuentaBuzon> cuentas)
        {
            string ruta = GetRutaSalida(modoPrueba: true);

            OrdenarListasPorCiudad(cuentas);

            if (buzonesMontevideo.Any())
            {
                if (configActual?.TipoAcreditacion == VariablesGlobales.p2p)
                    await Exporta_Reme(ruta, DateTime.Now, buzonesMontevideo, "MONTEVIDEO");
                else
                    await Exporta_Reme_Agrupado(ruta, DateTime.Now, buzonesMontevideo, "MONTEVIDEO");
            }

            if (buzonesMaldonado.Any())
            {
                if (configActual?.TipoAcreditacion == VariablesGlobales.p2p)
                    await Exporta_Reme(ruta, DateTime.Now, buzonesMaldonado, "MALDONADO");
                else
                    await Exporta_Reme_Agrupado(ruta, DateTime.Now, buzonesMaldonado, "MALDONADO");
            }
        }

        public async Task GenerarArchivo(List<CuentaBuzon> cuentas)
        {
            try
            {
                OrdenarListasPorCiudad(cuentas);

                if (buzonesMontevideo.Any())
                {
                    if (configActual.TipoAcreditacion == VariablesGlobales.p2p)
                        await Exporta_Reme(rutaBaseProduccion, DateTime.Now, buzonesMontevideo, "MONTEVIDEO");
                    else
                        await Exporta_Reme_Agrupado(rutaBaseProduccion, DateTime.Now, buzonesMontevideo, "MONTEVIDEO");
                }

                if (buzonesMaldonado.Any())
                {
                    if (configActual.TipoAcreditacion == VariablesGlobales.p2p)
                        await Exporta_Reme(rutaBaseProduccion, DateTime.Now, buzonesMaldonado, "MALDONADO");
                    else
                        await Exporta_Reme_Agrupado(rutaBaseProduccion, DateTime.Now, buzonesMaldonado, "MALDONADO");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        // ======================
        // 1) Exporta_Reme
        // ======================
        public async Task<bool> Exporta_Reme(string ruta, DateTime fecha, List<CuentaBuzon> cuentas, string ciudad)
        {
            try
            {
                if (!Directory.Exists(ruta))
                    Directory.CreateDirectory(ruta);

                string plantCode = ValidateSinglePlantAndGetCode(cuentas);
                if (plantCode == null)
                    return false;

                decimal totalUYU = 0, totalUSD = 0, totalEUR = 0, totalARS = 0, totalBRL = 0;
                int cUYU = 0, cUSD = 0, cEUR = 0, cARS = 0, cBRL = 0;
                var lines = new List<string>();

                foreach (var buz in cuentas)
                {
                    if (buz.Depositos == null) continue;

                    foreach (var dep in buz.Depositos)
                    {
                        var parts = (buz.Cuenta ?? "").Split('-');
                        if (parts.Length < 2) continue; // seguridad

                        string suc = buz.SucursalCuenta;
                        string cuenta = parts[0];
                        string sub = parts[1];
                        string mon = buz.Divisa;

                        string remito = ((buz.IdReferenciaAlCliente ?? "") + "X" + dep.IdOperacion).Trim();
                        decimal suma = Convert.ToDecimal(dep.Totales?.Sum(t => t.ImporteTotal) ?? 0m);

                        lines.Add(BuildDetalleBbvaLine(
                            suc, cuenta, mon, sub, CuentaTransportadora, suma, remito
                        ));

                        switch (NormalizeCurrency(mon))
                        {
                            case "UYU": totalUYU += suma; cUYU++; break;
                            case "USD": totalUSD += suma; cUSD++; break;
                            case "EUR": totalEUR += suma; cEUR++; break;
                            case "ARS": totalARS += suma; cARS++; break;
                            case "BRL": totalBRL += suma; cBRL++; break;
                        }
                    }
                }

                if (!lines.Any())
                    return false;

                lines.Add(BuildTotalLine("UYU", cUYU, totalUYU, plantCode));
                lines.Add(BuildTotalLine("USD", cUSD, totalUSD, plantCode));
                lines.Add(BuildTotalLine("EUR", cEUR, totalEUR, plantCode));
                lines.Add(BuildTotalLine("ARS", cARS, totalARS, plantCode));
                lines.Add(BuildTotalLine("BRL", cBRL, totalBRL, plantCode));

                // === Correlativo GLOBAL por día (sin ciudad) ===
                int correlativo = ReservarSiguienteCorrelativoDelDia(ruta, fecha);
                string f = fecha.ToString("yyyyMMdd");

                // 26/09/2025: Unificación de nombre (sin sufijos por ciudad)
                string nombreA = $"REME{f}{correlativo:D3}.txt";
                string nombreB = $"FREME{f}{correlativo:D3}.txt";
                string pathA = Path.Combine(ruta, nombreA);
                string pathB = Path.Combine(ruta, nombreB);

                var utf8NoBom = new UTF8Encoding(false);

                using (var sw = new StreamWriter(pathA, false, utf8NoBom))
                    foreach (var ln in lines) sw.WriteLine(ln);

                using (var sw2 = new StreamWriter(pathB, false, utf8NoBom)) { }

                BorrarMarcadorDelDia(ruta, fecha, correlativo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR en acreditación BBVA, VERIFIQUE: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // ======================
        // 2) Exporta_Reme_Agrupado (backup)
        // ======================
        public async Task<bool> Exporta_Reme_Agrupado_BackUp(string rutaBase, DateTime fecha, List<CuentaBuzon> cuentas, string ciudad)
        {
            try
            {
                if (!Directory.Exists(rutaBase))
                    Directory.CreateDirectory(rutaBase);

                string plantCode = ValidateSinglePlantAndGetCode(cuentas);
                if (plantCode == null)
                    return false;

                var grupos = cuentas
                    .SelectMany(b => b.Depositos ?? new List<Deposito>(), (b, dep) => new { b, dep, parts = (b.Cuenta ?? "").Split('-') })
                    .Where(x => x.parts.Length >= 2)
                    .GroupBy(x => new
                    {
                        x.b.SucursalCuenta,
                        Cuenta = x.parts[0].Trim(),
                        x.b.Divisa,
                        SubCuenta = x.parts[1].Trim(),
                        Remito = x.dep.IdOperacion.ToString()
                    })
                    .Select(g => new
                    {
                        Sucursal = g.Key.SucursalCuenta,
                        Cuenta = g.Key.Cuenta,
                        Moneda = g.Key.Divisa,
                        SubCuenta = g.Key.SubCuenta,
                        RemitoOriginal = g.Key.Remito,
                        SumaMontos = g.Sum(x => (x.dep.Totales?.Sum(t => t.ImporteTotal) ?? 0m))
                    })
                    .OrderBy(x => x.Sucursal)
                    .ThenBy(x => x.Cuenta)
                    .ThenBy(x => x.Moneda)
                    .ThenBy(x => x.SubCuenta)
                    .ThenBy(x => x.RemitoOriginal)
                    .ToList();

                var lines = new List<string>();
                decimal totalUYU = 0, totalUSD = 0, totalEUR = 0, totalARS = 0, totalBRL = 0;
                int cUYU = 0, cUSD = 0, cEUR = 0, cARS = 0, cBRL = 0;

                foreach (var g in grupos)
                {
                    string rem = (g.RemitoOriginal + DateTime.Now.ToString("HHmmssff"));

                    lines.Add(BuildDetalleBbvaLine(
                        g.Sucursal, g.Cuenta, g.Moneda, g.SubCuenta,
                        CuentaTransportadora, g.SumaMontos, rem
                    ));

                    switch (NormalizeCurrency(g.Moneda))
                    {
                        case "UYU": totalUYU += g.SumaMontos; cUYU++; break;
                        case "USD": totalUSD += g.SumaMontos; cUSD++; break;
                        case "EUR": totalEUR += g.SumaMontos; cEUR++; break;
                        case "ARS": totalARS += g.SumaMontos; cARS++; break;
                        case "BRL": totalBRL += g.SumaMontos; cBRL++; break;
                    }
                }

                if (!lines.Any())
                    return false;

                lines.Add(BuildTotalLine("UYU", cUYU, totalUYU, plantCode));
                lines.Add(BuildTotalLine("USD", cUSD, totalUSD, plantCode));
                lines.Add(BuildTotalLine("EUR", cEUR, totalEUR, plantCode));
                lines.Add(BuildTotalLine("ARS", cARS, totalARS, plantCode));
                lines.Add(BuildTotalLine("BRL", cBRL, totalBRL, plantCode));

                // === Correlativo GLOBAL por día (sin ciudad) ===
                int correlativo = ReservarSiguienteCorrelativoDelDia(rutaBase, fecha);
                string f = fecha.ToString("yyyyMMdd");
                string nombreA = $"REME{f}{correlativo:D3}.txt";
                string nombreB = $"FREME{f}{correlativo:D3}.txt";
                string pathA = Path.Combine(rutaBase, nombreA);
                string pathB = Path.Combine(rutaBase, nombreB);

                var utf8NoBom = new UTF8Encoding(false);

                using (var sw = new StreamWriter(pathA, false, utf8NoBom))
                    foreach (var ln in lines) sw.WriteLine(ln);

                using (var sw2 = new StreamWriter(pathB, false, utf8NoBom)) { }

                BorrarMarcadorDelDia(rutaBase, fecha, correlativo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR en acreditación BBVA, VERIFIQUE: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public async Task<bool> Exporta_Reme_Agrupado(string rutaBase, DateTime fecha, List<CuentaBuzon> cuentas, string ciudad)
        {
            try
            {
                if (!Directory.Exists(rutaBase))
                    Directory.CreateDirectory(rutaBase);

                string plantCode = ValidateSinglePlantAndGetCode(cuentas);
                if (plantCode == null)
                    return false;

                // === AGRUPACIÓN ESTILO "VIEJO" ===
                // Clave: Sucursal + Cuenta + SubCuenta + Moneda (SIN Remito/IdOperacion)
                var grupos = cuentas
                    .SelectMany(cb => (cb.Depositos ?? new List<Deposito>())
                        .Select(dep => new { cb, dep }))
                    .Select(x =>
                    {
                        var (cta, sub) = SplitCuenta(x.cb.Cuenta);
                        return new
                        {
                            Sucursal = x.cb.SucursalCuenta,
                            Cuenta = cta,
                            SubCuenta = sub,
                            Moneda = NormalizeCurrency(x.cb.Divisa),
                            x.cb,
                            x.dep
                        };
                    })
                    .GroupBy(x => new
                    {
                        x.Sucursal,
                        x.Cuenta,
                        x.SubCuenta,
                        x.Moneda
                    })
                    .Select(g => new
                    {
                        g.Key.Sucursal,
                        g.Key.Cuenta,
                        g.Key.SubCuenta,
                        g.Key.Moneda,
                        SumaMontos = g.Sum(xx =>
                            (decimal)(xx.dep.Totales?
                                .Where(t => NormalizeCurrency(t.Divisa) == g.Key.Moneda)
                                .Sum(t => (decimal)t.ImporteTotal) ?? 0m)),
                        Seed = (g.Select(xx => xx.cb.IdReferenciaAlCliente).FirstOrDefault()
                                ?? g.Key.Cuenta
                                ?? "0000")
                    })
                    .OrderBy(x => x.Sucursal)
                    .ThenBy(x => x.Cuenta)
                    .ThenBy(x => x.Moneda)
                    .ThenBy(x => x.SubCuenta)
                    .ToList();

                var lines = new List<string>();
                decimal totalUYU = 0, totalUSD = 0, totalEUR = 0, totalARS = 0, totalBRL = 0;
                int cUYU = 0, cUSD = 0, cEUR = 0, cARS = 0, cBRL = 0;

                foreach (var g in grupos)
                {
                    if (string.IsNullOrWhiteSpace(g.Cuenta) || string.IsNullOrWhiteSpace(g.SubCuenta))
                        continue; // seguridad

                    // Remito "viejo": prefijo(4) + HHmmssff, luego PRIMEROS 12
                    string pref4 = new string((g.Seed ?? "").ToUpper().Where(char.IsLetterOrDigit).Take(4).ToArray());
                    if (string.IsNullOrEmpty(pref4)) pref4 = "0000";
                    string remViejoLike = pref4 + DateTime.Now.ToString("HHmmssff");
                    string remitoFinal = Remito12Left(remViejoLike);

                    // Línea detalle (58 cols) — NO CAMBIADA
                    lines.Add(BuildDetalleBbvaLineLegacy(
                        g.Sucursal, g.Cuenta, g.Moneda, g.SubCuenta,
                        CuentaTransportadora, g.SumaMontos, remitoFinal
                    ));

                    switch (g.Moneda)
                    {
                        case "UYU": totalUYU += g.SumaMontos; cUYU++; break;
                        case "USD": totalUSD += g.SumaMontos; cUSD++; break;
                        case "EUR": totalEUR += g.SumaMontos; cEUR++; break;
                        case "ARS": totalARS += g.SumaMontos; cARS++; break;
                        case "BRL": totalBRL += g.SumaMontos; cBRL++; break;
                    }
                }

                if (!lines.Any())
                    return false;

                // Totales (26 cols exactas) — NO CAMBIADA
                lines.Add(BuildTotalLine("UYU", cUYU, totalUYU, plantCode));
                lines.Add(BuildTotalLine("USD", cUSD, totalUSD, plantCode));
                lines.Add(BuildTotalLine("EUR", cEUR, totalEUR, plantCode));
                lines.Add(BuildTotalLine("ARS", cARS, totalARS, plantCode));
                lines.Add(BuildTotalLine("BRL", cBRL, totalBRL, plantCode));

                // === Correlativo GLOBAL por día (sin ciudad) ===
                int correlativo = ReservarSiguienteCorrelativoDelDia(rutaBase, fecha);
                string f = fecha.ToString("yyyyMMdd");

                // 26/09/2025: Unificación de nombre (sin sufijos por ciudad)
                string nombreA = $"REME{f}{correlativo:D3}.txt";
                string nombreB = $"FREME{f}{correlativo:D3}.txt";
                string pathA = Path.Combine(rutaBase, nombreA);
                string pathB = Path.Combine(rutaBase, nombreB);

                var utf8NoBom = new UTF8Encoding(false);
                using (var sw = new StreamWriter(pathA, false, utf8NoBom))
                    foreach (var ln in lines) sw.WriteLine(ln);
                using (var sw2 = new StreamWriter(pathB, false, utf8NoBom)) { }

                BorrarMarcadorDelDia(rutaBase, fecha, correlativo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR en acreditación BBVA, VERIFIQUE: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        #region HELPERS
        // ====== BUILDERS a posiciones fijas (NO CAMBIADOS) ======
        private static string BuildDetalleBbvaLineLegacy(
            string sucursal, string cuenta, string moneda, string subCuenta,
            string ctaTransportadora, decimal importe, string remito)
        {
            var buf = Enumerable.Repeat('0', LEN_DETALLE).ToArray();

            string suc = PadLeftNumExact(sucursal, LEN_SUC);
            string cta = PadLeftNumExact(cuenta, LEN_CTA);
            string mon = Mon3Exact(moneda);
            string sub = PadLeftNumExact(subCuenta, LEN_SUB);
            string trans = PadLeftNumExact(ctaTransportadora, LEN_TRANS);
            string monto = Monto15Exact(importe);
            string rem = Remito12Left(remito);
            string mon2 = Mon3Exact(moneda);

            Put(buf, 1, suc, LEN_SUC);
            Put(buf, 4, cta, LEN_CTA);
            Put(buf, 13, mon, LEN_MON);
            Put(buf, 16, sub, LEN_SUB);
            buf[18] = PRODUCTO_FIJO;
            Put(buf, 20, trans, LEN_TRANS);
            Put(buf, 29, monto, LEN_MONTO);
            Put(buf, 44, rem, LEN_REMITO);
            Put(buf, 56, mon2, LEN_MON);

            if (buf.Length != LEN_DETALLE) throw new InvalidOperationException("Detalle mal formado.");
            if (buf[18] != '1') throw new InvalidOperationException("Columna 19 debe ser '1'.");

            return new string(buf);
        }

        private static string Digits(string s) =>
            new string((s ?? "").Where(char.IsDigit).ToArray());

        private static string AlnumUpper(string s) =>
            new string((s ?? "").ToUpper().Where(char.IsLetterOrDigit).ToArray());

        private static string PadLeftNumExact(string s, int len)
        {
            var d = Digits(s);
            if (d.Length > len) d = d[^len..];
            return d.PadLeft(len, '0');
        }

        private static string NormalizeCurrency(string mon)
        {
            mon = (mon ?? "").ToUpper().Trim();
            return mon switch
            {
                "U$S" => "USD",
                "US$" => "USD",
                "UYS" => "UYU",
                "UY" => "UYU",
                _ => mon
            };
        }

        private static string Remito12Left(string s)
        {
            var r = AlnumUpper(s);
            if (r.Length > 12) r = r.Substring(0, 12);
            return r.PadLeft(12, '0');
        }

        private static (string cta, string sub) SplitCuenta(string cuenta)
        {
            var parts = (cuenta ?? "").Split('-');
            string cta = parts.Length >= 2 ? Digits(parts[0]) : "";
            string sub = parts.Length >= 2 ? Digits(parts[1]) : "";
            return (cta, sub);
        }

        private static string Mon3Exact(string mon)
        {
            var m = NormalizeCurrency(mon);
            if (m.Length != 3) throw new InvalidOperationException($"Moneda inválida: '{mon}' → '{m}'");
            return m;
        }

        // N(15,2) → centavos sin separadores
        private static string Monto15Exact(decimal amount)
        {
            var cents = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
            var s = Math.Abs(cents).ToString();
            if (s.Length > LEN_MONTO) s = s[^LEN_MONTO..];
            return s.PadLeft(LEN_MONTO, '0');
        }

        // Remito alfanumérico de 12 (sin espacios)
        private static string Remito12Exact(string rem)
        {
            var r = AlnumUpper(rem);
            if (r.Length > LEN_REMITO) r = r[^LEN_REMITO..];
            return r.PadLeft(LEN_REMITO, '0');
        }

        private static void Put(char[] buf, int start1, string value, int len)
        {
            if (value.Length != len)
                throw new InvalidOperationException($"Campo de largo inválido. Esperado={len}, Recibido={value.Length}");
            int start0 = start1 - 1;
            for (int i = 0; i < len; i++) buf[start0 + i] = value[i];
        }

        private static string BuildDetalleBbvaLine(
            string sucursal, string cuenta, string moneda, string subCuenta,
            string ctaTransportadora, decimal importe, string remito)
        {
            var buf = Enumerable.Repeat('0', LEN_DETALLE).ToArray();

            string suc = PadLeftNumExact(sucursal, LEN_SUC);   // 1-3
            string cta = PadLeftNumExact(cuenta, LEN_CTA);     // 4-12
            string mon = Mon3Exact(moneda);                    // 13-15
            string sub = PadLeftNumExact(subCuenta, LEN_SUB);  // 16-18
            string trans = PadLeftNumExact(ctaTransportadora, LEN_TRANS); // 20-28
            string monto = Monto15Exact(importe);              // 29-43
            string rem = Remito12Exact(remito);                // 44-55
            string mon2 = Mon3Exact(moneda);                   // 56-58

            Put(buf, 1, suc, LEN_SUC);
            Put(buf, 4, cta, LEN_CTA);
            Put(buf, 13, mon, LEN_MON);
            Put(buf, 16, sub, LEN_SUB);
            buf[18] = PRODUCTO_FIJO;                           // 19 → '1'
            Put(buf, 20, trans, LEN_TRANS);
            Put(buf, 29, monto, LEN_MONTO);
            Put(buf, 44, rem, LEN_REMITO);
            Put(buf, 56, mon2, LEN_MON);

            if (buf.Length != LEN_DETALLE)
                throw new InvalidOperationException($"Detalle mal formado. Largo={buf.Length}, esperado={LEN_DETALLE}.");
            if (buf[18] != '1')
                throw new InvalidOperationException("El dígito de Producto no quedó en columna 19 = '1'.");

            return new string(buf);
        }

        private static string BuildTotalLine(string currency, int count, decimal total, string plantCode)
        {
            string mon = Mon3Exact(currency);                           // 21-23
            string cnt = Math.Max(0, count).ToString().PadLeft(4, '0'); // 2-5
            string tot = Monto15Exact(total);                           // 6-20
            string code = PadLeftNumExact(plantCode, 3);                // 24-26

            string line = $"T{cnt}{tot}{mon}{code}";

            if (line.Length != LEN_TOTALES)
                throw new InvalidOperationException($"Totales mal formado. Largo={line.Length}, esperado={LEN_TOTALES}.");

            return line;
        }

        // ===== Planta / correlativo =====
        private string ValidateSinglePlantAndGetCode(IEnumerable<CuentaBuzon> cuentas)
        {
            var ciudades = cuentas
                .Select(c => (c.Ciudad ?? string.Empty).Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(NormalizeCity)
                .ToList();

            var codigos = ciudades
                .Select(GetPlantCodeFromCity)
                .Distinct()
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            if (codigos.Count == 0)
            {
                MessageBox.Show("No se pudo determinar la planta (ciudad) para las cuentas recibidas.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            if (codigos.Count > 1)
            {
                var detalle = string.Join(", ", codigos);
                MessageBox.Show($"No se genera archivo: hay más de una planta en el conjunto ({detalle}).", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            return codigos[0];
        }

        private static string NormalizeCity(string raw)
        {
            string s = (raw ?? "").ToUpper().Trim();
            if (s == "MALDONADO") s = "PUNTA DEL ESTE";
            return s;
        }

        private static string GetPlantCodeFromCity(string cityUpper) =>
            cityUpper switch
            {
                "COLONIA" => "019",
                "PUNTA DEL ESTE" => "026",
                "MONTEVIDEO" => "050",
                _ => null
            };

        private void OrdenarListasPorCiudad(IEnumerable<CuentaBuzon> list)
        {
            buzonesMontevideo.Clear(); buzonesMaldonado.Clear();
            foreach (var cb in list)
            {
                if (cb.Ciudad?.Equals("MONTEVIDEO", StringComparison.OrdinalIgnoreCase) == true)
                    buzonesMontevideo.Add(cb);
                else if (cb.Ciudad?.Equals("MALDONADO", StringComparison.OrdinalIgnoreCase) == true
                         || cb.Ciudad?.Equals("PUNTA DEL ESTE", StringComparison.OrdinalIgnoreCase) == true)
                    buzonesMaldonado.Add(cb);
            }
        }

        // ======================
        // Correlativo GLOBAL por día (sin ciudad)
        // ======================
        private int ReservarSiguienteCorrelativoDelDia(string ruta, DateTime fecha)
        {
            if (!Directory.Exists(ruta)) Directory.CreateDirectory(ruta);

            string f = fecha.ToString("yyyyMMdd");

            int max = 0;

            // Archivos existentes del día
            foreach (var file in Directory.EnumerateFiles(ruta, $"REME{f}???.txt")
                                          .Concat(Directory.EnumerateFiles(ruta, $"FREME{f}???.txt")))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(name) || name.Length < 3) continue;

                // últimos 3 chars son el NNN
                string nnn = name.Substring(name.Length - 3, 3);
                if (int.TryParse(nnn, out int c) && c > max) max = c;
            }

            // Marcadores pendientes del día
            foreach (var mk in Directory.EnumerateFiles(ruta, $".reseq_{f}_???"))
            {
                var baseName = Path.GetFileName(mk); // .reseq_YYYYMMDD_NNN
                if (string.IsNullOrEmpty(baseName) || baseName.Length < 3) continue;

                string nnn = baseName.Substring(baseName.Length - 3, 3);
                if (int.TryParse(nnn, out int c) && c > max) max = c;
            }

            // Reserva atómica creando un marcador exclusivo
            while (true)
            {
                int next = max + 1;
                string marker = Path.Combine(ruta, $".reseq_{f}_{next:D3}");
                try
                {
                    using (File.Open(marker, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None)) { }
                    return next;
                }
                catch (IOException)
                {
                    max++;
                    continue;
                }
            }
        }

        private void BorrarMarcadorDelDia(string ruta, DateTime fecha, int correlativo)
        {
            try
            {
                string f = fecha.ToString("yyyyMMdd");
                string marker = Path.Combine(ruta, $".reseq_{f}_{correlativo:D3}");
                if (File.Exists(marker))
                    File.Delete(marker);
            }
            catch
            {
                // Best-effort cleanup
            }
        }
        #endregion
    }
}
