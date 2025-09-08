using ANS.Model.Interfaces;
using System.IO;
using System.Windows;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class BBVAFileGenerator : IBancoModoAcreditacion
    {
        private const string CuentaTransportadora = "7584652";
        private readonly string rutaBase = @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\BBVA";
        //private readonly string rutaBase = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\BBVA";
        private readonly ConfiguracionAcreditacion configActual;

        private List<CuentaBuzon> buzonesMontevideo = new();
        private List<CuentaBuzon> buzonesMaldonado = new();

        public BBVAFileGenerator(ConfiguracionAcreditacion config)
        {
            configActual = config;
        }

        public async Task GenerarArchivo(List<CuentaBuzon> cuentas)
        {
            try
            {

           
            OrdenarListasPorCiudad(cuentas);

            // Montevideo
            if (buzonesMontevideo.Any())
            {
                bool generated;
                if (configActual.TipoAcreditacion == VariablesGlobales.p2p)
                    generated = await Exporta_Reme(rutaBase, DateTime.Now, buzonesMontevideo, "MONTEVIDEO");
                else
                    generated = await Exporta_Reme_Agrupado(rutaBase, DateTime.Now, buzonesMontevideo, "MONTEVIDEO");
                // Si no generó, no se consumió correlativo (por diseño)
            }

            // Maldonado
            if (buzonesMaldonado.Any())
            {
                bool generated;
                if (configActual.TipoAcreditacion == VariablesGlobales.p2p)
                    generated = await Exporta_Reme(rutaBase, DateTime.Now, buzonesMaldonado, "MALDONADO");
                else
                    generated = await Exporta_Reme_Agrupado(rutaBase, DateTime.Now, buzonesMaldonado, "MALDONADO");
                // Si no generó, no se consumió correlativo (por diseño)
            }
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        // ======================
        // 1) Exporta_Reme  -> ahora retorna bool y pide el correlativo SOLO si hay líneas
        // ======================
        public async Task<bool> Exporta_Reme(string ruta, DateTime fecha, List<CuentaBuzon> cuentas, string ciudad)
        {
            try
            {
                if (!Directory.Exists(ruta))
                    Directory.CreateDirectory(ruta);

                // Validar que todas las cuentas pertenezcan a UNA sola planta y obtener su código
                string plantCode = ValidateSinglePlantAndGetCode(cuentas);
                if (plantCode == null)
                {
                    // No generamos archivos
                    return false;
                }

                double totalUYU = 0, totalUSD = 0, totalEUR = 0, totalARS = 0, totalBRL = 0;
                int cUYU = 0, cUSD = 0, cEUR = 0, cARS = 0, cBRL = 0;
                var lines = new List<string>();

                foreach (var buz in cuentas)
                {
                    if (buz.Depositos == null) continue;

                    foreach (var dep in buz.Depositos)
                    {
                        string suc = FormatString(buz.SucursalCuenta, 3);

                        var parts = (buz.Cuenta ?? "").Split('-');
                        if (parts.Length < 2) continue; // seguridad

                        string cuenta = FormatString(parts[0].Trim(), 9);
                        string sub = FormatString(parts[1].Trim(), 3);
                        string mon = buz.Divisa;
                        string prod = FormatString(buz.Producto.ToString(), 3);
                        string trans = FormatString(CuentaTransportadora, 7); // mantener 7 como en tu versión original de Exporta_Reme

                        string remito = FormatString((buz.IdReferenciaAlCliente ?? "") + "X" + dep.IdOperacion, 12);
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

                if (!lines.Any())
                    return false; // no hay acreditaciones -> NO consumimos correlativo

                // Agregar líneas de totales (con código de planta en pos. 24–26)
                lines.Add(GenerateTotalLine("UYU", cUYU, totalUYU, plantCode));
                lines.Add(GenerateTotalLine("USD", cUSD, totalUSD, plantCode));
                lines.Add(GenerateTotalLine("EUR", cEUR, totalEUR, plantCode));
                lines.Add(GenerateTotalLine("ARS", cARS, totalARS, plantCode));
                lines.Add(GenerateTotalLine("BRL", cBRL, totalBRL, plantCode));

                // AQUI recien reservamos correlativo global por fecha (atómico y sin “saltos”)
                int correlativo = ReservarSiguienteCorrelativoPorCiudad(ruta, fecha, ciudad);


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
                }

                using (var sw2 = new StreamWriter(pathB)) { }

                // Opcional: borrar marcador .reseq (si preferís mantener historial, comentá esto)
                BorrarMarcadorPorCiudad(ruta, fecha, correlativo, ciudad);


                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR en acreditación BBVA, VERIFIQUE: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw ex;
            }
        }

        // ===================================
        // 2) Exporta_Reme_Agrupado  -> pide correlativo SOLO si hay líneas
        // ===================================
        public async Task<bool> Exporta_Reme_Agrupado(string rutaBase, DateTime fecha, List<CuentaBuzon> cuentas, string ciudad)
        {
            try
            {
                if (!Directory.Exists(rutaBase))
                    Directory.CreateDirectory(rutaBase);

                // Validar planta única y obtener código
                string plantCode = ValidateSinglePlantAndGetCode(cuentas);
                if (plantCode == null)
                {
                    return false; // no generamos archivos
                }

                // Agrupación (con null-safe en Totales)
                var grupos = cuentas
                    .SelectMany(b => b.Depositos ?? new List<Deposito>(), (b, dep) => new { b, dep, parts = (b.Cuenta ?? "").Split('-') })
                    .Where(x => x.parts.Length >= 2) // seguridad
                    .GroupBy(x => new
                    {
                        x.b.SucursalCuenta,
                        Cuenta = x.parts[0].Trim(),
                        x.b.Divisa,
                        CuentaTransportadora,
                        SubCuenta = x.parts[1].Trim(),
                        Remito = x.dep.IdOperacion.ToString(),
                        x.b.Producto
                    })
                    .Select(g => new
                    {
                        Sucursal = g.Key.SucursalCuenta,
                        Cuenta = g.Key.Cuenta,
                        Moneda = g.Key.Divisa,
                        CuentaTransportadora = g.Key.CuentaTransportadora,
                        SubCuenta = g.Key.SubCuenta,
                        RemitoOriginal = g.Key.Remito,
                        Producto = g.Key.Producto,
                        SumaMontos = g.Sum(x => (x.dep.Totales?.Sum(t => t.ImporteTotal) ?? 0))
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

                    // EN AGRUPADO usabas 9 para transportadora y Producto directo (sin format fijo)
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

                // Si no hay líneas de detalle, NO generamos archivos ni consumimos correlativo
                if (!lines.Any())
                    return false;

                // Totales (con código de planta en pos. 24–26)
                lines.Add(GenerateTotalLine("UYU", cUYU, totalUYU, plantCode));
                lines.Add(GenerateTotalLine("USD", cUSD, totalUSD, plantCode));
                lines.Add(GenerateTotalLine("EUR", cEUR, totalEUR, plantCode));
                lines.Add(GenerateTotalLine("ARS", cARS, totalARS, plantCode));
                lines.Add(GenerateTotalLine("BRL", cBRL, totalBRL, plantCode));

                // AQUI recien reservamos correlativo (atómico)
                int correlativo = ReservarSiguienteCorrelativoPorCiudad(rutaBase, fecha, ciudad);


                string f = fecha.ToString("yyyyMMdd");
                string suf = ciudad.ToUpper() == "MALDONADO" ? "MAL" : string.Empty;
                string nombreA = $"REME{f}{correlativo:D3}{suf}.txt";
                string nombreB = $"FREME{f}{correlativo:D3}{suf}.txt";
                string pathA = Path.Combine(rutaBase, nombreA);
                string pathB = Path.Combine(rutaBase, nombreB);

                using (var sw = new StreamWriter(pathA))
                    foreach (var ln in lines) sw.WriteLine(ln);

                using (var sw2 = new StreamWriter(pathB)) { }

                // Opcional: borrar marcador
                BorrarMarcadorPorCiudad(rutaBase, fecha, correlativo, ciudad);

           

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR en acreditación BBVA, VERIFIQUE: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                throw ex;
            }
        }

        #region Métodos auxiliares

        private string FormatString(string input, int len) =>
            (input ?? string.Empty).Replace(".", "").Replace(",", "").Replace("-", "").PadLeft(len, '0');

        private string FormatAmount(string amt) => amt.Replace(".", "").Replace(",", "").PadLeft(15, '0');

        // Inserta código planta en posiciones 24–26 (índices 23-25) manteniendo longitud
        private string GenerateTotalLine(string currency, int count, double total, string plantCode)
        {
            string baseLine =
                $"T{count.ToString().PadLeft(4, '0')}{total.ToString("N").Replace(".", "").Replace(",", "").PadLeft(15, '0')}{currency}";
            int originalLen = baseLine.Length;

            string code = (plantCode ?? string.Empty).PadLeft(3, '0').Substring(0, 3);
            int workingLen = Math.Max(originalLen, 26);
            char[] buffer = baseLine.PadRight(workingLen, ' ').ToCharArray();

            buffer[23] = code[0];
            buffer[24] = code[1];
            buffer[25] = code[2];

            string withPlant = new string(buffer);
            if (originalLen >= 26)
                return withPlant.Substring(0, originalLen);
            else
                return withPlant.Substring(0, 26);
        }

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

            return codigos[0]; // único código
        }

        private static string NormalizeCity(string raw)
        {
            string s = (raw ?? "").ToUpper().Trim();
            if (s == "MALDONADO") s = "PUNTA DEL ESTE";
            return s;
        }

        private static string GetPlantCodeFromCity(string cityUpper)
        {
            return cityUpper switch
            {
                "COLONIA" => "019",
                "PUNTA DEL ESTE" => "026",
                "MONTEVIDEO" => "050",
                _ => null
            };
        }

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
        // 3) Correlativo global por fecha con "reserva" atómica mediante archivo marcador
        //    - No se consume número hasta que hay líneas y estamos por escribir.
        //    - Considera tanto archivos FREME/REME existentes como marcadores previos.
        // ======================
        private int ReservarSiguienteCorrelativoPorCiudad(string ruta, DateTime fecha, string ciudad)
        {
            if (!Directory.Exists(ruta)) Directory.CreateDirectory(ruta);

            string f = fecha.ToString("yyyyMMdd");
            // Sufijo exacto en el nombre de archivo
            string suf = ciudad?.ToUpper() == "MALDONADO" ? "MAL" : "";
            // Prefijo amigable para el marcador (evitamos vacío)
            string cityTag = suf == "MAL" ? "MAL" : "MV";

            // Solo miramos los archivos de ESA ciudad
            string pattern = $"FREME{f}*{suf}.txt";
            int max = 0;

            foreach (var file in Directory.EnumerateFiles(ruta, pattern))
            {
                var name = Path.GetFileNameWithoutExtension(file); // FREMEyyyymmddNNN[ MAL]
                if (name.Length >= 16 && int.TryParse(name.Substring(13, 3), out int c))
                    if (c > max) max = c;
            }

            // Considerar marcadores de ESA ciudad
            string markerPattern = $".reseq_{f}_{cityTag}_*";
            foreach (var mk in Directory.EnumerateFiles(ruta, markerPattern))
            {
                var baseName = Path.GetFileName(mk); // .reseq_yyyyMMdd_CITY_NNN
                var parts = baseName.Split('_');
                if (parts.Length >= 4 && int.TryParse(parts[3], out int c))
                    if (c > max) max = c;
            }

            // Reservar atómicamente el siguiente NNN para ESA ciudad
            while (true)
            {
                int next = max + 1;
                string marker = Path.Combine(ruta, $".reseq_{f}_{cityTag}_{next:D3}");
                try
                {
                    using (File.Open(marker, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                    { /* creado: reservado */ }
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
            catch { /* opcional */ }
        }


        #endregion
    }
}
