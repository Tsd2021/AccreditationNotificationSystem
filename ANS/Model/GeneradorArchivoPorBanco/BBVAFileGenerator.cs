using ANS.Model.Interfaces;
using System.IO;
using System.Windows;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class BBVAFileGenerator : IBancoModoAcreditacion
    {
        private readonly string CuentaTransportadora = "007584652";
        private string ruta = @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\BBVA";
        public List<CuentaBuzon> buzonesMontevideo = new List<CuentaBuzon>();
        public List<CuentaBuzon> buzonesMaldonado = new List<CuentaBuzon>();
        public ConfiguracionAcreditacion configActual;
        public BBVAFileGenerator(ConfiguracionAcreditacion config)
        {
            this.configActual = config;
        }
        public async Task GenerarArchivo(List<CuentaBuzon> cb)
        {
            //primero: ordenar las listas por ciudad
            OrdenarListasPorCiudad(cb);

            //segundo: obtener el correlativo para cada ciudad
            int correlativoMontevideo = ObtenerNumeroCorrelativo(ruta, DateTime.Now, "MONTEVIDEO");

            int correlativoMaldonado = ObtenerNumeroCorrelativo(ruta, DateTime.Now, "MALDONADO");

            //tercero: exportar los archivos
            if (buzonesMontevideo.Count > 0)
            {
                if (correlativoMontevideo > 0)
                {
                    if (configActual.TipoAcreditacion == VariablesGlobales.p2p)
                    {
                       await Exporta_Reme(ruta, DateTime.Now, correlativoMontevideo, buzonesMontevideo, "MONTEVIDEO");

                    }
                    else if (configActual.TipoAcreditacion == VariablesGlobales.diaxdia)
                    {
                        await Exporta_Reme_Agrupado(ruta, DateTime.Now, correlativoMontevideo, buzonesMontevideo, "MONTEVIDEO");
                    }

                }
            }
            if (buzonesMaldonado.Count > 0)
            {
                if (correlativoMaldonado > 0)
                {
                    if (configActual.TipoAcreditacion == VariablesGlobales.p2p)
                    {
                        await Exporta_Reme(ruta, DateTime.Now, correlativoMontevideo, buzonesMaldonado, "MALDONADO");
                    }
                    else if (configActual.TipoAcreditacion == VariablesGlobales.diaxdia)
                    {
                        await Exporta_Reme_Agrupado(ruta, DateTime.Now, correlativoMontevideo, buzonesMontevideo, "MADLONADO");
                    }
                }
            }
        }
        //EXPORTA_REME ES PARA PUNTO A PUNTO
        public async Task Exporta_Reme(string ruta, DateTime fecha, int correlativo, List<CuentaBuzon> cuentaBuzones, string ciudad)
        {
            try
            {
                // Variables para acumular totales y contadores
                double totalPesos = 0, totalDolares = 0, totalEuros = 0, totalArgentinos = 0, totalReales = 0;
                int countPesos = 0, countDolares = 0, countEuros = 0, countArgentinos = 0, countReales = 0;

                // Lista para almacenar las líneas de depósitos
                List<string> depositLines = new List<string>();

                // Recorremos todas las cuentas y depósitos para acumular totales y preparar las líneas
                foreach (var unaCuentaBuzon in cuentaBuzones)
                {
                    if (unaCuentaBuzon.Depositos != null && unaCuentaBuzon.Depositos.Count > 0)
                    {
                        foreach (var unDeposito in unaCuentaBuzon.Depositos)
                        {
                            string sucursal = FormatString(unaCuentaBuzon.SucursalCuenta, 3);
                            string[] splitParts = unaCuentaBuzon.Cuenta.Split('-');
                            string cuenta = FormatString(splitParts[0].Trim(), 9);
                            string subcuenta = FormatString(splitParts[1].Trim(), 3);
                            string moneda = unaCuentaBuzon.Divisa;
                            string producto = FormatString(unaCuentaBuzon.Producto.ToString(), 3);
                            string cuentaTransportadora = FormatString(CuentaTransportadora, 9);
                            string horaActual = DateTime.Now.ToString("HHmmssff");
                            string remito = FormatString(unaCuentaBuzon.IdReferenciaAlCliente + "X" + unDeposito.IdOperacion, 12);
                            remito = remito.Length > 12 ? remito.Substring(0, 12) : remito;
                            double sumaMontos = sumarMontos(unDeposito.Totales);
                            string monto = FormatAmount(sumaMontos.ToString("F2"));

                            // Agregamos la línea de depósito a la lista
                            depositLines.Add($"{sucursal}{cuenta}{moneda}{subcuenta}{producto}{cuentaTransportadora}{monto}{remito}{moneda}");

                            // Acumulamos los totales según la divisa
                            switch (moneda)
                            {
                                case "UYU":
                                    totalPesos += sumaMontos;
                                    countPesos++;
                                    break;
                                case "USD":
                                    totalDolares += sumaMontos;
                                    countDolares++;
                                    break;
                                case "EUR":
                                    totalEuros += sumaMontos;
                                    countEuros++;
                                    break;
                                case "ARS":
                                    totalArgentinos += sumaMontos;
                                    countArgentinos++;
                                    break;
                                case "BRL":
                                    totalReales += sumaMontos;
                                    countReales++;
                                    break;
                            }
                        }
                    }
                }

                // Verificamos que al menos uno de los totales tenga un valor mayor a cero
                if (totalPesos > 0 || totalDolares > 0 || totalEuros > 0 || totalArgentinos > 0 || totalReales > 0)
                {
                    if (!Directory.Exists(ruta))
                    {
                        Directory.CreateDirectory(ruta);
                    }

                    string fechaStr = fecha.ToString("yyyyMMdd");
                    string suffix = ciudad.ToUpper() == "MALDONADO" ? "MAL" : string.Empty;

                    string nombreArchivo = $"REME{fechaStr}{correlativo:D3}{suffix}.txt";
                    string nombreArchivo2 = $"FREME{fechaStr}{correlativo:D3}{suffix}.txt";

                    string filePath = Path.Combine(ruta, nombreArchivo);
                    string filePath2 = Path.Combine(ruta, nombreArchivo2);

                    // Creación del archivo principal
                    using (TextWriter sw = new StreamWriter(filePath))
                    {
                        // Escribimos todas las líneas de depósitos
                        foreach (var linea in depositLines)
                        {
                            sw.WriteLine(linea);
                        }

                        // Escribimos las líneas de totales
                        sw.WriteLine(GenerateTotalLine("UYU", countPesos, totalPesos));
                        sw.WriteLine(GenerateTotalLine("USD", countDolares, totalDolares));
                        sw.WriteLine(GenerateTotalLine("EUR", countEuros, totalEuros));
                        sw.WriteLine(GenerateTotalLine("ARS", countArgentinos, totalArgentinos));
                        sw.WriteLine(GenerateTotalLine("BRL", countReales, totalReales));
                    }

                    // Si es necesario, creamos el segundo archivo
                    using (TextWriter sw2 = new StreamWriter(filePath2))
                    {
                        // Implementar el contenido para el archivo FREME según corresponda.
                    }
                }
                // Si ninguno de los totales tiene valor, el archivo no se crea.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR, VERIFIQUE: {ex.Message}", "ERROR");
            }
        }
        //EXPORTA_REME_AGRUPADO ES PARA DIA A DIA BBVA!
        public async Task<bool> Exporta_Reme_Agrupado(string ruta, DateTime fecha, int correlativo, List<CuentaBuzon> cuentaBuzones, string ciudad)
        {

            ruta = @"C:\Users\dchiquiar\Desktop\pruebas txt dia a dia BBVA\DIAADIA";
            try
            {
                if (!Directory.Exists(ruta))
                {
                    Directory.CreateDirectory(ruta);
                }

                string fechaStr = fecha.ToString("yyyyMMdd");

                string suffix = ciudad.ToUpper() == "MALDONADO" ? "MAL" : string.Empty;

                string nombreArchivo = $"REMETANDA{fechaStr}{correlativo:D3}{suffix}.txt";

                string nombreArchivo2 = $"FREMETANDA{fechaStr}{correlativo:D3}{suffix}.txt";

                string filePath = Path.Combine(ruta, nombreArchivo);

                string filePath2 = Path.Combine(ruta, nombreArchivo2);

                TextWriter sw2 = new StreamWriter(filePath2);

                using (TextWriter sw = new StreamWriter(filePath))
                {
                    double totalPesos = 0, totalDolares = 0, totalEuros = 0, totalArgentinos = 0, totalReales = 0;
                    int countPesos = 0, countDolares = 0, countEuros = 0, countArgentinos = 0, countReales = 0;



                    foreach (var unaCuenta in cuentaBuzones)
                    {

                        if (unaCuenta.Depositos != null && unaCuenta.Depositos.Count > 0)
                        {
                            double sumaMontos = unaCuenta.Depositos.Sum(dep => dep.Totales.Sum(t => t.ImporteTotal));

                            string sucursal = FormatString(unaCuenta.SucursalCuenta, 3);

                            string[] splitParts = unaCuenta.Cuenta.Split('-');

                            string cuenta = CleanString(FormatString(splitParts[0].Trim(), 9));

                            string subcuenta = CleanString(FormatString(splitParts[1].Trim(), 3));

                            string moneda = unaCuenta.Divisa;

                            string cuentaTransportadora = CleanString(CuentaTransportadora);

                            string horaActual = DateTime.Now.ToString("HHmmssff");

                            string Producto = FormatString(unaCuenta.Producto.ToString(), 3);

                            string remito = horaActual;

                            remito = remito.Length > 12 ? remito.Substring(0, 12) : remito;

                            string monto = FormatAmount(sumaMontos.ToString("F2"));

                            sw.WriteLine($"{sucursal}{cuenta}{moneda}{subcuenta}{Producto}{cuentaTransportadora}{monto}{remito}{moneda}");

                            switch (moneda)
                            {
                                case "UYU":
                                    totalPesos += sumaMontos;
                                    countPesos++;
                                    break;
                                case "USD":
                                    totalDolares += sumaMontos;
                                    countDolares++;
                                    break;
                                case "EUR":
                                    totalEuros += sumaMontos;
                                    countEuros++;
                                    break;
                                case "ARS":
                                    totalArgentinos += sumaMontos;
                                    countArgentinos++;
                                    break;
                                case "BRL":
                                    totalReales += sumaMontos;
                                    countReales++;
                                    break;
                            }
                            sw.WriteLine(GenerateTotalLine("UYU", countPesos, totalPesos));
                            sw.WriteLine(GenerateTotalLine("USD", countDolares, totalDolares));
                            sw.WriteLine(GenerateTotalLine("EUR", countEuros, totalEuros));
                            sw.WriteLine(GenerateTotalLine("ARS", countArgentinos, totalArgentinos));
                            sw.WriteLine(GenerateTotalLine("BRL", countReales, totalReales));
                            return true;

                        }
                    }

                    return false;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR en acreditacion BBVA, VERIFIQUE: {ex.Message}", "ERROR");
                return false;
            }

        }
        #region Métodos privados auxiliares a los métodos importantes
        private string FormatAmount(string amount)
        {
            string formatted = amount.Replace(".", "").Replace(",", "");
            return formatted.PadLeft(15, '0');
        }
        private string FormatString(string input, int length)
        {
            string formatted = input.Replace(".", "").Replace(",", "").Replace("-", "");
            return formatted.PadLeft(length, '0');
        }
        private string CleanString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return input.Replace("\r", "").Replace("\n", "").Trim();
        }
        private string GenerateTotalLine(string currency, int count, double total)
        {
            string formattedCount = count.ToString().PadLeft(4, '0');
            string amountString = total.ToString("N").Replace(".", "").Replace(",", "").PadLeft(15, '0');
            return $"T{formattedCount}{amountString}{currency}";
        }
        private void OrdenarListasPorCiudad(List<CuentaBuzon> cb)
        {
            foreach (var cuentaBuzon in cb)
            {
                if (cuentaBuzon.Ciudad.ToUpper() == "MONTEVIDEO")
                {
                    buzonesMontevideo.Add(cuentaBuzon);
                }
                else if (cuentaBuzon.Ciudad.ToUpper() == "MALDONADO")
                {
                    buzonesMaldonado.Add(cuentaBuzon);
                }
            }
        }
        public int ObtenerNumeroCorrelativo(string ruta, DateTime fecha, string ciudad)
        {
            try
            {
                if (!Directory.Exists(ruta))
                {
                    Directory.CreateDirectory(ruta);
                }

                string fechaStr = fecha.ToString("yyyyMMdd");
                string pattern = ciudad.ToUpper() == "MALDONADO" ? $"FREME{fechaStr}*MAL.txt" : $"FREME{fechaStr}*.txt";
                var archivos = Directory.GetFiles(ruta, pattern);
                int maxCorrelativo = 0;

                foreach (var archivo in archivos)
                {
                    string nombreArchivo = Path.GetFileNameWithoutExtension(archivo);
                    string correlativoStr = nombreArchivo.Substring(13, 3); // Asumiendo que el correlativo siempre tiene 3 dígitos y está en la posición 11-13 del nombre del archivo
                    if (int.TryParse(correlativoStr, out int correlativo))
                    {
                        if (correlativo > maxCorrelativo)
                        {
                            maxCorrelativo = correlativo;
                        }
                    }
                }

                return maxCorrelativo + 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener el número correlativo en Archivo BBVA: {ex.Message}");
                return -1; // En caso de error, devolver 1 como valor por defecto
            }
        }
        private double sumarMontos(List<Total> totales)
        {
            double total = 0;
            if (totales != null && totales.Count > 0)
            {
                foreach (Total unTotal in totales)
                {
                    total += unTotal.ImporteTotal;
                }
            }
            return total;
        }
    }
    #endregion
}

