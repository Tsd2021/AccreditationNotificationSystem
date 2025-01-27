using ANS.Model.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class SantanderFileGenerator : IBancoModoAcreditacion
    {
        private ConfiguracionAcreditacion _config { get; set; }
        private string _tipoRegistro = "R";
        private string _tipoOperacion = "C";
        private string _tipoMovimiento = "D";
        private string _tipoDetalle = "MAE";
        private string _sucTecnisegurPesosMon = "004";
        private string _sucTecnisegurDolaresMon = "005";
        private string _sucTecnisegurPesosMald = "137";
        private string _sucTecnisegurDolaresMald = "138";
        private string _cashOfficeRutaDolaresP2P = @"\\172.16.10.20\cashoffice$\CashSantander\DOLARES\";
        private string _cashOfficeRutaPesosP2P = @"\\172.16.10.20\cashoffice$\CashSantander\PESOS\";
        private string _rutaDolaresP2P = @"\\172.16.10.20\puntoapuntocsvstdr$\DOLARES\";
        private string _rutaPesosP2P = @"\\172.16.10.20\puntoapuntocsvstdr$\PESOS\";
        private Dictionary<int, int> CuentasTata = new Dictionary<int, int>
                {
                { 67, 1 },
                { 68, 1 },
                { 69, 2 },
                { 70, 2 }
                };
        public SantanderFileGenerator(ConfiguracionAcreditacion config)
        {
            _config = config;
        }
        public string getRutaArchivoDAD(string ciudad, string divisa)
        {

            if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.pesos)
            {
                return @"D:\CSVSANTANDER\" + "TEC_" + _sucTecnisegurPesosMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
            }
            else if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.dolares)
            {
                return @"D:\CSVSANTANDER\" + "TEC_" + _sucTecnisegurDolaresMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
            }
            else if (ciudad.ToUpper() == VariablesGlobales.montevideo && divisa == VariablesGlobales.pesos)
            {
                return @"D:\CSVSANTANDER\" + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
            }
            else
                return @"D:\CSVSANTANDER\" + "TEC_" + _sucTecnisegurDolaresMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";

        }
        public void GenerarArchivo(List<CuentaBuzon> cb)
        {

            if (_config.TipoAcreditacion == VariablesGlobales.p2p)
            {
                //Generar archivo P2P
                GenerarLineasPorTotales(cb);
            }

            else if (_config.TipoAcreditacion == VariablesGlobales.tanda)
            {
                GenerarLineasPorCuentasBuzones(cb);
            }

            else if (_config.TipoAcreditacion == VariablesGlobales.diaxdia)
            {
                GenerarLineasPorCuentasBuzones(cb);
            }

            else
            {
                throw new Exception("Tipo de acreditación no soportado");
            }

        }
        private void GenerarLineasPorTotales(List<CuentaBuzon> cb)
        {
            StringBuilder maldonadoPesos = new StringBuilder();
            StringBuilder maldonadoDolares = new StringBuilder();
            StringBuilder montevideoPesos = new StringBuilder();
            StringBuilder montevideoDolares = new StringBuilder();

            if (cb != null && cb.Count > 0)
            {
                foreach (var unaCuenta in cb)
                {
                    if (unaCuenta.Depositos != null && unaCuenta.Depositos.Count > 0)
                    {
                        foreach (var unDeposito in unaCuenta.Depositos)
                        {
                            if (unDeposito.Totales != null && unDeposito.Totales.Count > 0)
                            {
                                foreach (Total unTotal in unDeposito.Totales)
                                {
                                    if (unaCuenta.Ciudad == VariablesGlobales.maldonado)
                                    {
                                        if (unaCuenta.Divisa == VariablesGlobales.pesos)
                                        {
                                            agregarLineaAlStringBuilder_Individual(maldonadoPesos, unaCuenta, unDeposito, unTotal);
                                        }
                                        else if (unaCuenta.Divisa == VariablesGlobales.dolares)
                                        {
                                            agregarLineaAlStringBuilder_Individual(maldonadoDolares, unaCuenta, unDeposito, unTotal);
                                        }
                                    }
                                    else if (unaCuenta.Ciudad == VariablesGlobales.montevideo)
                                    {
                                        if (unaCuenta.Divisa == VariablesGlobales.pesos)
                                        {
                                            agregarLineaAlStringBuilder_Individual(montevideoPesos, unaCuenta, unDeposito, unTotal);
                                        }
                                        else if (unaCuenta.Divisa == VariablesGlobales.dolares)
                                        {
                                            agregarLineaAlStringBuilder_Individual(montevideoDolares, unaCuenta, unDeposito, unTotal);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            CrearArchivo(maldonadoPesos, maldonadoDolares, montevideoPesos, montevideoDolares);
        }
        private void GenerarLineasPorCuentasBuzones(List<CuentaBuzon> cb)
        {
            StringBuilder maldonadoPesos = new StringBuilder();
            StringBuilder maldonadoDolares = new StringBuilder();
            StringBuilder montevideoPesos = new StringBuilder();
            StringBuilder montevideoDolares = new StringBuilder();

            if (cb != null && cb.Count > 0)
            {
                foreach (var unaCuenta in cb)
                {
                    if (unaCuenta.Depositos != null && unaCuenta.Depositos.Count > 0)
                    {
                        double sumaMontos = unaCuenta.Depositos.Sum(dep => dep.Totales.Sum(t => t.ImporteTotal));

                        if (sumaMontos > 0)
                        {
                            if (unaCuenta.Ciudad == VariablesGlobales.maldonado)
                            {
                                if (unaCuenta.Divisa == VariablesGlobales.pesos)
                                {
                                    agregarLineaAlStringBuilder_Agrupado(maldonadoPesos, unaCuenta, sumaMontos);
                                }
                                else if (unaCuenta.Divisa == VariablesGlobales.dolares)
                                {
                                    agregarLineaAlStringBuilder_Agrupado(maldonadoDolares, unaCuenta, sumaMontos);
                                }
                            }
                            else if (unaCuenta.Ciudad == VariablesGlobales.montevideo)
                            {
                                if (unaCuenta.Divisa == VariablesGlobales.pesos)
                                {
                                    agregarLineaAlStringBuilder_Agrupado(montevideoPesos, unaCuenta, sumaMontos);
                                }
                                else if (unaCuenta.Divisa == VariablesGlobales.dolares)
                                {
                                    agregarLineaAlStringBuilder_Agrupado(montevideoDolares, unaCuenta, sumaMontos);
                                }
                            }
                        }
                    }
                }
            }
            CrearArchivo(maldonadoPesos, maldonadoDolares, montevideoPesos, montevideoDolares);
        }
        private void CrearArchivo(StringBuilder maldonadoPesos, StringBuilder maldonadoDolares, StringBuilder montevideoPesos, StringBuilder montevideoDolares)
        {
            CrearArchivoPorCiudadYDivisa(maldonadoPesos, VariablesGlobales.maldonado, VariablesGlobales.pesos);
            CrearArchivoPorCiudadYDivisa(maldonadoDolares, VariablesGlobales.maldonado, VariablesGlobales.dolares);
            CrearArchivoPorCiudadYDivisa(montevideoPesos, VariablesGlobales.montevideo, VariablesGlobales.pesos);
            CrearArchivoPorCiudadYDivisa(montevideoDolares, VariablesGlobales.montevideo, VariablesGlobales.dolares);
        }
        private void CrearArchivoPorCiudadYDivisa(StringBuilder contenido, string ciudad, string divisa)
        {

            if (contenido.Length == 0) return; // No crear archivos vacíos

            string ruta = getRutaArchivoDAD(ciudad, divisa);

            string directorio = Path.GetDirectoryName(ruta);

            if (!Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio); // Crear directorio si no existe
            }

            File.WriteAllText(ruta, contenido.ToString());

        }
        //METODO PARA CREAR LINEAS EN ARCHIVOS DIA A DIA Y TANDA!
        private void agregarLineaAlStringBuilder_Agrupado(StringBuilder lineas, CuentaBuzon unaCuenta, double totalPorCuenta)
        {

            string referenciaDetalle = "";

            string referencia = unaCuenta.IdReferenciaAlCliente;

            if (CuentasTata.ContainsKey(unaCuenta.IdCliente))
            {
                referencia = ReemplazarPrimerCaracter(unaCuenta.IdReferenciaAlCliente, CuentasTata[unaCuenta.CuentasBuzonesId]);
            }

            lineas.AppendLine($"{_tipoRegistro};{_tipoOperacion};{unaCuenta.SucursalCuenta};{unaCuenta.Cuenta};{unaCuenta.Divisa};{totalPorCuenta}00;{_tipoMovimiento};{_tipoDetalle};{referenciaDetalle}");

        }
        //METODO PARA CREAR LINEAS EN ARCHIVOS PUNTO A PUNTO!
        private void agregarLineaAlStringBuilder_Individual(StringBuilder sb, CuentaBuzon cb, Deposito depo, Total tot)
        {

            string referenciaDetalle = "";

            string referencia = cb.IdReferenciaAlCliente;

            if (CuentasTata.ContainsKey(cb.IdCliente))
            {
                referencia = ReemplazarPrimerCaracter(cb.IdReferenciaAlCliente, CuentasTata[cb.CuentasBuzonesId]);
            }

            referenciaDetalle = $"{referencia}-{depo.IdOperacion}";


            sb.AppendLine($"{_tipoRegistro};{_tipoOperacion};{cb.SucursalCuenta};{cb.Cuenta};{cb.Divisa};{tot.ImporteTotal}00;{_tipoMovimiento};{_tipoDetalle};{referenciaDetalle}");
        }
        private string ReemplazarPrimerCaracter(string input, int newNumber)
        {

            if (string.IsNullOrEmpty(input))
            {
                return newNumber.ToString();
            }

            return newNumber.ToString() + input.Substring(1);
        }
    }
}
