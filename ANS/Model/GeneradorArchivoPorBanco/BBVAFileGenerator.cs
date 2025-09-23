//using ANS.Model.Interfaces;
//using System.IO;
//using System.Windows;
//using System.Linq;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace ANS.Model.GeneradorArchivoPorBanco
//{
//    public class BBVAFileGenerator : IBancoModoAcreditacion
//    {
//        private const string CuentaTransportadora = "7584652";
//        private readonly string rutaBase = @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\BBVA";
//        //private readonly string rutaBase = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\BBVA";
//        private readonly ConfiguracionAcreditacion configActual;

//        private List<CuentaBuzon> buzonesMontevideo = new();
//        private List<CuentaBuzon> buzonesMaldonado = new();

//        public BBVAFileGenerator(ConfiguracionAcreditacion config)
//        {
//            configActual = config;
//        }

//        public async Task GenerarArchivo(List<CuentaBuzon> cuentas)
//        {
//            try
//            {


//            OrdenarListasPorCiudad(cuentas);

//            // Montevideo
//            if (buzonesMontevideo.Any())
//            {
//                bool generated;
//                if (configActual.TipoAcreditacion == VariablesGlobales.p2p)
//                    generated = await Exporta_Reme(rutaBase, DateTime.Now, buzonesMontevideo, "MONTEVIDEO");
//                else
//                    generated = await Exporta_Reme_Agrupado(rutaBase, DateTime.Now, buzonesMontevideo, "MONTEVIDEO");
//                // Si no generó, no se consumió correlativo (por diseño)
//            }

//            // Maldonado
//            if (buzonesMaldonado.Any())
//            {
//                bool generated;
//                if (configActual.TipoAcreditacion == VariablesGlobales.p2p)
//                    generated = await Exporta_Reme(rutaBase, DateTime.Now, buzonesMaldonado, "MALDONADO");
//                else
//                    generated = await Exporta_Reme_Agrupado(rutaBase, DateTime.Now, buzonesMaldonado, "MALDONADO");
//                // Si no generó, no se consumió correlativo (por diseño)
//            }
//            }
//            catch(Exception e)
//            {
//                throw e;
//            }
//        }

//        // ======================
//        // 1) Exporta_Reme  -> ahora retorna bool y pide el correlativo SOLO si hay líneas
//        // ======================
//        public async Task<bool> Exporta_Reme(string ruta, DateTime fecha, List<CuentaBuzon> cuentas, string ciudad)
//        {
//            try
//            {
//                if (!Directory.Exists(ruta))
//                    Directory.CreateDirectory(ruta);

//                // Validar que todas las cuentas pertenezcan a UNA sola planta y obtener su código
//                string plantCode = ValidateSinglePlantAndGetCode(cuentas);
//                if (plantCode == null)
//                {
//                    // No generamos archivos
//                    return false;
//                }

//                double totalUYU = 0, totalUSD = 0, totalEUR = 0, totalARS = 0, totalBRL = 0;
//                int cUYU = 0, cUSD = 0, cEUR = 0, cARS = 0, cBRL = 0;
//                var lines = new List<string>();

//                foreach (var buz in cuentas)
//                {
//                    if (buz.Depositos == null) continue;

//                    foreach (var dep in buz.Depositos)
//                    {
//                        string suc = FormatString(buz.SucursalCuenta, 3);

//                        var parts = (buz.Cuenta ?? "").Split('-');
//                        if (parts.Length < 2) continue; // seguridad

//                        string cuenta = FormatString(parts[0].Trim(), 9);
//                        string sub = FormatString(parts[1].Trim(), 3);
//                        string mon = buz.Divisa;
//                        string prod = FormatString(buz.Producto.ToString(), 3);
//                        string trans = FormatString(CuentaTransportadora, 7); // mantener 7 como en tu versión original de Exporta_Reme

//                        string remito = FormatString((buz.IdReferenciaAlCliente ?? "") + "X" + dep.IdOperacion, 12);
//                        remito = remito.Length > 12 ? remito.Substring(0, 12) : remito;

//                        double suma = dep.Totales?.Sum(t => t.ImporteTotal) ?? 0;
//                        string monto = FormatAmount(suma.ToString("F2"));

//                        lines.Add($"{suc}{cuenta}{mon}{sub}{1}{trans}{monto}{remito}{mon}");

//                        switch (mon)
//                        {
//                            case "UYU": totalUYU += suma; cUYU++; break;
//                            case "USD": totalUSD += suma; cUSD++; break;
//                            case "EUR": totalEUR += suma; cEUR++; break;
//                            case "ARS": totalARS += suma; cARS++; break;
//                            case "BRL": totalBRL += suma; cBRL++; break;
//                        }
//                    }
//                }

//                if (!lines.Any())
//                    return false; // no hay acreditaciones -> NO consumimos correlativo

//                // Agregar líneas de totales (con código de planta en pos. 24–26)
//                lines.Add(GenerateTotalLine("UYU", cUYU, totalUYU, plantCode));
//                lines.Add(GenerateTotalLine("USD", cUSD, totalUSD, plantCode));
//                lines.Add(GenerateTotalLine("EUR", cEUR, totalEUR, plantCode));
//                lines.Add(GenerateTotalLine("ARS", cARS, totalARS, plantCode));
//                lines.Add(GenerateTotalLine("BRL", cBRL, totalBRL, plantCode));

//                // AQUI recien reservamos correlativo global por fecha (atómico y sin “saltos”)
//                int correlativo = ReservarSiguienteCorrelativoPorCiudad(ruta, fecha, ciudad);


//                string f = fecha.ToString("yyyyMMdd");
//                string suf = ciudad.ToUpper() == "MALDONADO" ? "MAL" : string.Empty;
//                string nombreA = $"REME{f}{correlativo:D3}{suf}.txt";
//                string nombreB = $"FREME{f}{correlativo:D3}{suf}.txt";
//                string pathA = Path.Combine(ruta, nombreA);
//                string pathB = Path.Combine(ruta, nombreB);

//                using (var sw = new StreamWriter(pathA))
//                {
//                    foreach (var ln in lines)
//                        sw.WriteLine(ln);
//                }

//                using (var sw2 = new StreamWriter(pathB)) { }

//                // Opcional: borrar marcador .reseq (si preferís mantener historial, comentá esto)
//                BorrarMarcadorPorCiudad(ruta, fecha, correlativo, ciudad);


//                return true;
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"ERROR en acreditación BBVA, VERIFIQUE: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
//                throw ex;
//            }
//        }

//        // ===================================
//        // 2) Exporta_Reme_Agrupado  -> pide correlativo SOLO si hay líneas
//        // ===================================
//        public async Task<bool> Exporta_Reme_Agrupado(string rutaBase, DateTime fecha, List<CuentaBuzon> cuentas, string ciudad)
//        {
//            try
//            {
//                if (!Directory.Exists(rutaBase))
//                    Directory.CreateDirectory(rutaBase);

//                // Validar planta única y obtener código
//                string plantCode = ValidateSinglePlantAndGetCode(cuentas);
//                if (plantCode == null)
//                {
//                    return false; // no generamos archivos
//                }

//                // Agrupación (con null-safe en Totales)
//                var grupos = cuentas
//                    .SelectMany(b => b.Depositos ?? new List<Deposito>(), (b, dep) => new { b, dep, parts = (b.Cuenta ?? "").Split('-') })
//                    .Where(x => x.parts.Length >= 2) // seguridad
//                    .GroupBy(x => new
//                    {
//                        x.b.SucursalCuenta,
//                        Cuenta = x.parts[0].Trim(),
//                        x.b.Divisa,
//                        CuentaTransportadora,
//                        SubCuenta = x.parts[1].Trim(),
//                        Remito = x.dep.IdOperacion.ToString(),
//                        x.b.Producto
//                    })
//                    .Select(g => new
//                    {
//                        Sucursal = g.Key.SucursalCuenta,
//                        Cuenta = g.Key.Cuenta,
//                        Moneda = g.Key.Divisa,
//                        CuentaTransportadora = g.Key.CuentaTransportadora,
//                        SubCuenta = g.Key.SubCuenta,
//                        RemitoOriginal = g.Key.Remito,
//                        Producto = g.Key.Producto,
//                        SumaMontos = g.Sum(x => (x.dep.Totales?.Sum(t => t.ImporteTotal) ?? 0))
//                    })
//                    .OrderBy(x => x.Sucursal)
//                    .ThenBy(x => x.Cuenta)
//                    .ThenBy(x => x.Moneda)
//                    .ThenBy(x => x.SubCuenta)
//                    .ThenBy(x => x.Producto)
//                    .ThenBy(x => x.RemitoOriginal)
//                    .ToList();

//                var lines = new List<string>();
//                double totalUYU = 0, totalUSD = 0, totalEUR = 0, totalARS = 0, totalBRL = 0;
//                int cUYU = 0, cUSD = 0, cEUR = 0, cARS = 0, cBRL = 0;

//                foreach (var g in grupos)
//                {
//                    string suc = FormatString(g.Sucursal, 3);
//                    string cue = FormatString(g.Cuenta, 9);
//                    string mon = g.Moneda;
//                    string sub = FormatString(g.SubCuenta, 3);

//                    // EN AGRUPADO usabas 9 para transportadora y Producto directo (sin format fijo)
//                    string trans = FormatString(g.CuentaTransportadora, 9);
//                    string rem = FormatString(g.RemitoOriginal, 4);
//                    string hora = DateTime.Now.ToString("HHmmssff");
//                    rem = (rem + hora).Length > 12 ? (rem + hora).Substring(0, 12) : (rem + hora);

//                    string monto = FormatAmount(g.SumaMontos.ToString("F2"));

//                    lines.Add($"{suc}{cue}{mon}{sub}{1}{trans}{monto}{rem}{mon}");

//                    switch (mon)
//                    {
//                        case "UYU": totalUYU += g.SumaMontos; cUYU++; break;
//                        case "USD": totalUSD += g.SumaMontos; cUSD++; break;
//                        case "EUR": totalEUR += g.SumaMontos; cEUR++; break;
//                        case "ARS": totalARS += g.SumaMontos; cARS++; break;
//                        case "BRL": totalBRL += g.SumaMontos; cBRL++; break;
//                    }
//                }

//                // Si no hay líneas de detalle, NO generamos archivos ni consumimos correlativo
//                if (!lines.Any())
//                    return false;

//                // Totales (con código de planta en pos. 24–26)
//                lines.Add(GenerateTotalLine("UYU", cUYU, totalUYU, plantCode));
//                lines.Add(GenerateTotalLine("USD", cUSD, totalUSD, plantCode));
//                lines.Add(GenerateTotalLine("EUR", cEUR, totalEUR, plantCode));
//                lines.Add(GenerateTotalLine("ARS", cARS, totalARS, plantCode));
//                lines.Add(GenerateTotalLine("BRL", cBRL, totalBRL, plantCode));

//                // AQUI recien reservamos correlativo (atómico)
//                int correlativo = ReservarSiguienteCorrelativoPorCiudad(rutaBase, fecha, ciudad);


//                string f = fecha.ToString("yyyyMMdd");
//                string suf = ciudad.ToUpper() == "MALDONADO" ? "MAL" : string.Empty;
//                string nombreA = $"REME{f}{correlativo:D3}{suf}.txt";
//                string nombreB = $"FREME{f}{correlativo:D3}{suf}.txt";
//                string pathA = Path.Combine(rutaBase, nombreA);
//                string pathB = Path.Combine(rutaBase, nombreB);

//                using (var sw = new StreamWriter(pathA))
//                    foreach (var ln in lines) sw.WriteLine(ln);

//                using (var sw2 = new StreamWriter(pathB)) { }

//                // Opcional: borrar marcador
//                BorrarMarcadorPorCiudad(rutaBase, fecha, correlativo, ciudad);



//                return true;
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"ERROR en acreditación BBVA, VERIFIQUE: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
//                throw ex;
//            }
//        }

//        #region Métodos auxiliares

//        private string FormatString(string input, int len) =>
//            (input ?? string.Empty).Replace(".", "").Replace(",", "").Replace("-", "").PadLeft(len, '0');

//        private string FormatAmount(string amt) => amt.Replace(".", "").Replace(",", "").PadLeft(15, '0');

//        // Inserta código planta en posiciones 24–26 (índices 23-25) manteniendo longitud
//        private string GenerateTotalLine(string currency, int count, double total, string plantCode)
//        {
//            string baseLine =
//                $"T{count.ToString().PadLeft(4, '0')}{total.ToString("N").Replace(".", "").Replace(",", "").PadLeft(15, '0')}{currency}";
//            int originalLen = baseLine.Length;

//            string code = (plantCode ?? string.Empty).PadLeft(3, '0').Substring(0, 3);
//            int workingLen = Math.Max(originalLen, 26);
//            char[] buffer = baseLine.PadRight(workingLen, ' ').ToCharArray();

//            buffer[23] = code[0];
//            buffer[24] = code[1];
//            buffer[25] = code[2];

//            string withPlant = new string(buffer);
//            if (originalLen >= 26)
//                return withPlant.Substring(0, originalLen);
//            else
//                return withPlant.Substring(0, 26);
//        }

//        private string ValidateSinglePlantAndGetCode(IEnumerable<CuentaBuzon> cuentas)
//        {
//            var ciudades = cuentas
//                .Select(c => (c.Ciudad ?? string.Empty).Trim())
//                .Where(s => !string.IsNullOrWhiteSpace(s))
//                .Select(NormalizeCity)
//                .ToList();

//            var codigos = ciudades
//                .Select(GetPlantCodeFromCity)
//                .Distinct()
//                .Where(c => !string.IsNullOrEmpty(c))
//                .ToList();

//            if (codigos.Count == 0)
//            {
//                MessageBox.Show("No se pudo determinar la planta (ciudad) para las cuentas recibidas.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return null;
//            }

//            if (codigos.Count > 1)
//            {
//                var detalle = string.Join(", ", codigos);
//                MessageBox.Show($"No se genera archivo: hay más de una planta en el conjunto ({detalle}).", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return null;
//            }

//            return codigos[0]; // único código
//        }

//        private static string NormalizeCity(string raw)
//        {
//            string s = (raw ?? "").ToUpper().Trim();
//            if (s == "MALDONADO") s = "PUNTA DEL ESTE";
//            return s;
//        }

//        private static string GetPlantCodeFromCity(string cityUpper)
//        {
//            return cityUpper switch
//            {
//                "COLONIA" => "019",
//                "PUNTA DEL ESTE" => "026",
//                "MONTEVIDEO" => "050",
//                _ => null
//            };
//        }

//        private void OrdenarListasPorCiudad(IEnumerable<CuentaBuzon> list)
//        {
//            buzonesMontevideo.Clear(); buzonesMaldonado.Clear();
//            foreach (var cb in list)
//            {
//                if (cb.Ciudad?.Equals("MONTEVIDEO", StringComparison.OrdinalIgnoreCase) == true)
//                    buzonesMontevideo.Add(cb);
//                else if (cb.Ciudad?.Equals("MALDONADO", StringComparison.OrdinalIgnoreCase) == true
//                         || cb.Ciudad?.Equals("PUNTA DEL ESTE", StringComparison.OrdinalIgnoreCase) == true)
//                    buzonesMaldonado.Add(cb);
//            }
//        }

//        // ======================
//        // 3) Correlativo global por fecha con "reserva" atómica mediante archivo marcador
//        //    - No se consume número hasta que hay líneas y estamos por escribir.
//        //    - Considera tanto archivos FREME/REME existentes como marcadores previos.
//        // ======================
//        private int ReservarSiguienteCorrelativoPorCiudad(string ruta, DateTime fecha, string ciudad)
//        {
//            if (!Directory.Exists(ruta)) Directory.CreateDirectory(ruta);

//            string f = fecha.ToString("yyyyMMdd");
//            // Sufijo exacto en el nombre de archivo
//            string suf = ciudad?.ToUpper() == "MALDONADO" ? "MAL" : "";
//            // Prefijo amigable para el marcador (evitamos vacío)
//            string cityTag = suf == "MAL" ? "MAL" : "MV";

//            // Solo miramos los archivos de ESA ciudad
//            string pattern = $"FREME{f}*{suf}.txt";
//            int max = 0;

//            foreach (var file in Directory.EnumerateFiles(ruta, pattern))
//            {
//                var name = Path.GetFileNameWithoutExtension(file); // FREMEyyyymmddNNN[ MAL]
//                if (name.Length >= 16 && int.TryParse(name.Substring(13, 3), out int c))
//                    if (c > max) max = c;
//            }

//            // Considerar marcadores de ESA ciudad
//            string markerPattern = $".reseq_{f}_{cityTag}_*";
//            foreach (var mk in Directory.EnumerateFiles(ruta, markerPattern))
//            {
//                var baseName = Path.GetFileName(mk); // .reseq_yyyyMMdd_CITY_NNN
//                var parts = baseName.Split('_');
//                if (parts.Length >= 4 && int.TryParse(parts[3], out int c))
//                    if (c > max) max = c;
//            }

//            // Reservar atómicamente el siguiente NNN para ESA ciudad
//            while (true)
//            {
//                int next = max + 1;
//                string marker = Path.Combine(ruta, $".reseq_{f}_{cityTag}_{next:D3}");
//                try
//                {
//                    using (File.Open(marker, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
//                    { /* creado: reservado */ }
//                    return next;
//                }
//                catch (IOException)
//                {
//                    max++;
//                    continue;
//                }
//            }
//        }

//        private void BorrarMarcadorPorCiudad(string ruta, DateTime fecha, int correlativo, string ciudad)
//        {
//            try
//            {
//                string f = fecha.ToString("yyyyMMdd");
//                string cityTag = (ciudad?.ToUpper() == "MALDONADO") ? "MAL" : "MV";
//                string marker = Path.Combine(ruta, $".reseq_{f}_{cityTag}_{correlativo:D3}");
//                if (File.Exists(marker))
//                    File.Delete(marker);
//            }
//            catch { /* opcional */ }
//        }


//        #endregion
//    }
//}

using ANS.Model.Interfaces;
using System.IO;
using System.Windows;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class BBVAFileGenerator : IBancoModoAcreditacion
    {
        // ====== Layout fijo BBVA ======
        // Detalle (58 cols)
        private const int LEN_SUC = 3;   // 1-3   Sucursal (N, 0-fill)
        private const int LEN_CTA = 9;   // 4-12  Cuenta   (N, 0-fill)
        private const int LEN_MON = 3;   // 13-15 Moneda   (texto 3)
        private const int LEN_SUB = 3;   // 16-18 Subcta   (N, 0-fill)
        private const int LEN_FLAG = 1;   // 19    Producto (fijo '1' = CC)
        private const int LEN_TRANS = 9;   // 20-28 Transportadora (N, 0-fill)
        private const int LEN_MONTO = 15;  // 29-43 Monto (N(15,2) centavos)
        private const int LEN_REMITO = 12;  // 44-55 Remito (alfa-num 12)
        private const int LEN_DETALLE = 58;  // 56-58 Moneda saldo (3) => total 58
        // Totales (27 cols)
        private const int LEN_TOTALES = 26;
        private const char PRODUCTO_FIJO = '1'; // 1 = CC (columna 19)
        private const string CuentaTransportadora = "7584652"; // se pad-left a 9
        private readonly string rutaBaseProduccion = @"\\192.168.0.9\bbva\SALIDA";
        //RUTA TEST EN 22 
        //private readonly string rutaBase = @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\BBVA";
        //RUTA TEST LOCAL
        //private readonly string rutaBase = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\BBVA";
        private readonly ConfiguracionAcreditacion configActual;
        private readonly List<CuentaBuzon> buzonesMontevideo = new();
        private readonly List<CuentaBuzon> buzonesMaldonado = new();
        public BBVAFileGenerator(ConfiguracionAcreditacion config) => configActual = config;
        public BBVAFileGenerator() { }
        public async Task generarArchivoTest()
        {

            //chequea si rutabase existe.
            if (string.IsNullOrWhiteSpace(rutaBaseProduccion))
                throw new InvalidOperationException("rutaBaseProduccion no está configurada.");

            //crea un directorio en rutaBaseProd
            Directory.CreateDirectory(rutaBaseProduccion);

            string baseName = "ignorar";   // o "test"

            string extension = ".txt";          // usa ".txt" si querés: string extension = ".txt";

            int n = 0;

            while (true)
            {
                string fileName = n == 0 ? $"{baseName}{extension}" : $"{baseName}{n}{extension}";

                string path = Path.Combine(rutaBaseProduccion, fileName);

                try
                {
                 
                    using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    using (var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
                    {
                        await sw.WriteAsync("TEST - PRUEBA").ConfigureAwait(false);
                    }
                    // éxito → salimos
                    break;
                }
                catch (IOException)
                {
                    // probablemente ya existe ese nombre → incrementamos y reintentamos
                    n++;
                    continue;
                }
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
            catch (Exception e)
            {
                throw e;
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

                int correlativo = ReservarSiguienteCorrelativoPorCiudad(ruta, fecha, ciudad);

                string f = fecha.ToString("yyyyMMdd");
                string suf = ciudad.ToUpper() == "MALDONADO" ? "MAL" : string.Empty;
                string nombreA = $"REME{f}{correlativo:D3}{suf}.txt";
                string nombreB = $"FREME{f}{correlativo:D3}{suf}.txt";
                string pathA = Path.Combine(ruta, nombreA);
                string pathB = Path.Combine(ruta, nombreB);

                var utf8NoBom = new UTF8Encoding(false);

                using (var sw = new StreamWriter(pathA, false, utf8NoBom))
                    foreach (var ln in lines) sw.WriteLine(ln);

                using (var sw2 = new StreamWriter(pathB, false, utf8NoBom)) { }

                BorrarMarcadorPorCiudad(ruta, fecha, correlativo, ciudad);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR en acreditación BBVA, VERIFIQUE: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw ex;
            }
        }

        // ======================
        // 2) Exporta_Reme_Agrupado
        // ======================
        public async Task<bool> Exporta_Reme_Agrupado(string rutaBase, DateTime fecha, List<CuentaBuzon> cuentas, string ciudad)
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

                int correlativo = ReservarSiguienteCorrelativoPorCiudad(rutaBase, fecha, ciudad);

                string f = fecha.ToString("yyyyMMdd");
                string suf = ciudad.ToUpper() == "MALDONADO" ? "MAL" : string.Empty;
                string nombreA = $"REME{f}{correlativo:D3}{suf}.txt";
                string nombreB = $"FREME{f}{correlativo:D3}{suf}.txt";
                string pathA = Path.Combine(rutaBase, nombreA);
                string pathB = Path.Combine(rutaBase, nombreB);

                var utf8NoBom = new UTF8Encoding(false);

                using (var sw = new StreamWriter(pathA, false, utf8NoBom))
                    foreach (var ln in lines) sw.WriteLine(ln);

                using (var sw2 = new StreamWriter(pathB, false, utf8NoBom)) { }

                BorrarMarcadorPorCiudad(rutaBase, fecha, correlativo, ciudad);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR en acreditación BBVA, VERIFIQUE: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw ex;
            }
        }

        // ====== BUILDERS a posiciones fijas ======
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
            return r.PadLeft(LEN_REMITO, '0'); // si falta largo, 0-left
        }

        private static void Put(char[] buf, int start1, string value, int len)
        {
            if (value.Length != len)
                throw new InvalidOperationException($"Campo de largo inválido. Esperado={len}, Recibido={value.Length}");
            int start0 = start1 - 1;
            for (int i = 0; i < len; i++) buf[start0 + i] = value[i];
        }

        // ===== Detalle: 58 columnas exactas; '1' en columna 19 =====
        private static string BuildDetalleBbvaLine(
            string sucursal, string cuenta, string moneda, string subCuenta,
            string ctaTransportadora, decimal importe, string remito)
        {
            var buf = Enumerable.Repeat('0', LEN_DETALLE).ToArray(); // relleno inicial con '0'

            string suc = PadLeftNumExact(sucursal, LEN_SUC);              // 1-3
            string cta = PadLeftNumExact(cuenta, LEN_CTA);              // 4-12
            string mon = Mon3Exact(moneda);                               // 13-15
            string sub = PadLeftNumExact(subCuenta, LEN_SUB);             // 16-18
            string trans = PadLeftNumExact(ctaTransportadora, LEN_TRANS);   // 20-28 (9)
            string monto = Monto15Exact(importe);                           // 29-43
            string rem = Remito12Exact(remito);                           // 44-55
            string mon2 = Mon3Exact(moneda);                               // 56-58

            // Colocar campos por posición (1-based de la especificación)
            Put(buf, 1, suc, LEN_SUC);         // 1-3
            Put(buf, 4, cta, LEN_CTA);         // 4-12
            Put(buf, 13, mon, LEN_MON);         // 13-15
            Put(buf, 16, sub, LEN_SUB);         // 16-18
            buf[18] = PRODUCTO_FIJO;              // 19 → '1'
            Put(buf, 20, trans, LEN_TRANS);       // 20-28
            Put(buf, 29, monto, LEN_MONTO);       // 29-43
            Put(buf, 44, rem, LEN_REMITO);      // 44-55
            Put(buf, 56, mon2, LEN_MON);         // 56-58

            // Validaciones duras
            if (buf.Length != LEN_DETALLE)
                throw new InvalidOperationException($"Detalle mal formado. Largo={buf.Length}, esperado={LEN_DETALLE}.");
            if (buf[18] != '1')
                throw new InvalidOperationException("El dígito de Producto no quedó en columna 19 = '1'.");

            return new string(buf);
        }

   
        // ===== Totales: 26 columnas exactas; planta 24–26 =====
        private static string BuildTotalLine(string currency, int count, decimal total, string plantCode)
        {
            // Campos
            string mon = Mon3Exact(currency);                           // 21-23
            string cnt = Math.Max(0, count).ToString().PadLeft(4, '0'); // 2-5
            string tot = Monto15Exact(total);                           // 6-20
            string code = PadLeftNumExact(plantCode, 3);                 // 24-26

     
            string line = $"T{cnt}{tot}{mon}{code}";

            // Validación dura
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

        // correlativo con marcador por ciudad
        private int ReservarSiguienteCorrelativoPorCiudad(string ruta, DateTime fecha, string ciudad)
        {
            if (!Directory.Exists(ruta)) Directory.CreateDirectory(ruta);

            string f = fecha.ToString("yyyyMMdd");
            string suf = ciudad?.ToUpper() == "MALDONADO" ? "MAL" : "";
            string cityTag = suf == "MAL" ? "MAL" : "MV";

            string pattern = $"FREME{f}*{suf}.txt";
            int max = 0;

            foreach (var file in Directory.EnumerateFiles(ruta, pattern))
            {
                var name = Path.GetFileNameWithoutExtension(file); // FREMEyyyymmddNNN[ MAL]
                if (name.Length >= 16 && int.TryParse(name.Substring(13, 3), out int c))
                    if (c > max) max = c;
            }

            string markerPattern = $".reseq_{f}_{cityTag}_*";
            foreach (var mk in Directory.EnumerateFiles(ruta, markerPattern))
            {
                var baseName = Path.GetFileName(mk);
                var parts = baseName.Split('_');
                if (parts.Length >= 4 && int.TryParse(parts[3], out int c))
                    if (c > max) max = c;
            }

            while (true)
            {
                int next = max + 1;
                string marker = Path.Combine(ruta, $".reseq_{f}_{cityTag}_{next:D3}");
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

        private void BorrarMarcadorPorCiudad(string ruta, DateTime fecha, int correlativo, string ciudad)
        {
            try
            {
                string f = fecha.ToString("yyyyMMdd");
                string cityTag = (ciudad?.ToUpper() == "MALDONADO") ? "MAL" : "MV";
                string marker = Path.Combine(ruta, $".reseq_{f}_{cityTag}_{correlativo:D3}");
                if (File.Exists(marker))
                    File.Delete(marker);
            }
            catch { }
        }
    }
}
