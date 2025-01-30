﻿using ANS.Model.Interfaces;
using ANS.Model.Services;
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
        /*
        private string _cashOfficeRutaDolaresP2P = @"\\172.16.10.20\cashoffice$\CashSantander\DOLARES\";
        private string _cashOfficeRutaPesosP2P = @"\\172.16.10.20\cashoffice$\CashSantander\PESOS\";
        private string _rutaDolaresP2P = @"\\172.16.10.20\puntoapuntocsvstdr$\DOLARES\";
        private string _rutaPesosP2P = @"\\172.16.10.20\puntoapuntocsvstdr$\PESOS\";
        */
        private string _cashOfficeRutaDolaresP2P = @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SANTANDER\cashoffice$\CashSantander\DOLARES\";
        private string _cashOfficeRutaPesosP2P = @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SANTANDER\cashoffice$\CashSantander\PESOS\";
        private string _rutaDolaresP2P = @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SANTANDER\puntoapuntocsvstdr$\DOLARES\";
        private string _rutaPesosP2P = @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SANTANDER\puntoapuntocsvstdr$\PESOS\";
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

        /*
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
        */

        // TEST TEST TEST TEST TEST TEST //
        public string getRutaArchivoDAD(string ciudad, string divisa)
        {

            if (this._config.TipoAcreditacion == VariablesGlobales.p2p)
            {
                if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.pesos)
                {
                    return @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SANTANDER\puntoapuntocsvstdr$\MALDONADO\PESOS\" + "TEC_" + _sucTecnisegurPesosMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
                }
                else if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.dolares)
                {
                    return @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SANTANDER\puntoapuntocsvstdr$\MALDONADO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
                }
                else if (ciudad.ToUpper() == VariablesGlobales.montevideo && divisa == VariablesGlobales.pesos)
                {
                    return @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SANTANDER\puntoapuntocsvstdr$\MONTEVIDEO\PESOS\" + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
                }
                else if (ciudad.ToUpper() == VariablesGlobales.montevideo && divisa == VariablesGlobales.dolares)
                {
                    return @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SANTANDER\puntoapuntocsvstdr$\MONTEVIDEO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
                }
            }
            else if (this._config.TipoAcreditacion == VariablesGlobales.tanda)
            {
                if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.pesos)
                {
                    return @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SANTANDER\tanda$\MALDONADO\PESOS\" + "TEC_" + _sucTecnisegurPesosMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
                }
                else if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.dolares)
                {
                    return @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SANTANDER\tanda$\MALDONADO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
                }
                else if (ciudad.ToUpper() == VariablesGlobales.montevideo && divisa == VariablesGlobales.pesos)
                {
                    return @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SANTANDER\tanda$\MONTEVIDEO\PESOS\" + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
                }
                else
                    return @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SANTANDER\tanda$\MONTEVIDEO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
            }
            return "hola";
        }
        public async Task GenerarArchivo(List<CuentaBuzon> cb)
        {
            if (_config.TipoAcreditacion == VariablesGlobales.p2p)
            {
                //Generar archivo P2P
                await GenerarLineasPorTotales(cb);
            }
            else if (_config.TipoAcreditacion == VariablesGlobales.tanda)
            {
                await GenerarLineasPorCuentasBuzones(cb);
            }
            else if (_config.TipoAcreditacion == VariablesGlobales.diaxdia)
            {
                await GenerarLineasPorCuentasBuzones(cb);
            }
            else
            {
                throw new Exception("Tipo de acreditación no soportado");
            }
        }
        private async Task GenerarLineasPorTotales(List<CuentaBuzon> cb)
        {
            StringBuilder maldonadoPesos = new StringBuilder();
            StringBuilder maldonadoDolares = new StringBuilder();
            StringBuilder montevideoPesos = new StringBuilder();
            StringBuilder montevideoDolares = new StringBuilder();
            StringBuilder cashOfficePesos = new StringBuilder();
            StringBuilder cashOfficeDolares = new StringBuilder();

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
                                    if (unaCuenta.esCashOffice())
                                    {

                                        if (unaCuenta.Divisa == VariablesGlobales.pesos)
                                        {

                                            agregarLineaAlStringBuilder_Individual(cashOfficePesos, unaCuenta, unDeposito, unTotal);
                                        }
                                        else if (unaCuenta.Divisa == VariablesGlobales.dolares)
                                        {
                                            agregarLineaAlStringBuilder_Individual(cashOfficePesos, unaCuenta, unDeposito, unTotal);
                                        }
                                    }
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
            await CrearArchivo(maldonadoPesos, maldonadoDolares, montevideoPesos, montevideoDolares, cashOfficePesos, cashOfficeDolares);
        }
        private async Task GenerarLineasPorCuentasBuzones(List<CuentaBuzon> cb)
        {
            StringBuilder maldonadoPesos = new StringBuilder();
            StringBuilder maldonadoDolares = new StringBuilder();
            StringBuilder montevideoPesos = new StringBuilder();
            StringBuilder montevideoDolares = new StringBuilder();
            StringBuilder cashOfficePesos = new StringBuilder();
            StringBuilder cashOfficeDolares = new StringBuilder();

            if (cb != null && cb.Count > 0)
            {
                foreach (var unaCuenta in cb)
                {
                    if (unaCuenta.Depositos != null && unaCuenta.Depositos.Count > 0)
                    {
                        double sumaMontos = unaCuenta.Depositos.Sum(dep => dep.Totales.Sum(t => t.ImporteTotal));

                        if (sumaMontos > 0)
                        {
                            if (unaCuenta.esCashOffice())
                            {
                                if (unaCuenta.Divisa == VariablesGlobales.pesos)
                                {
                                    agregarLineaAlStringBuilder_Agrupado(cashOfficePesos, unaCuenta, sumaMontos);
                                }
                                else if (unaCuenta.Divisa == VariablesGlobales.dolares)
                                {
                                    agregarLineaAlStringBuilder_Agrupado(cashOfficeDolares, unaCuenta, sumaMontos);
                                }
                            }
                            else
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

            await CrearArchivo(maldonadoPesos, maldonadoDolares, montevideoPesos, montevideoDolares, cashOfficePesos, cashOfficeDolares);
        }
        private async Task CrearArchivo(StringBuilder maldonadoPesos, StringBuilder maldonadoDolares, StringBuilder montevideoPesos, StringBuilder montevideoDolares, StringBuilder cashOfficePesos, StringBuilder cashOfficeDolares)
        {
            if (maldonadoPesos.Length > 0)
            {
                await CrearArchivoPorCiudadYDivisa(maldonadoPesos, VariablesGlobales.maldonado, VariablesGlobales.pesos);
            }
            if (maldonadoDolares.Length > 0)
            {
                await CrearArchivoPorCiudadYDivisa(maldonadoDolares, VariablesGlobales.maldonado, VariablesGlobales.dolares);
            }
            if (montevideoPesos.Length > 0)
            {
                await CrearArchivoPorCiudadYDivisa(montevideoPesos, VariablesGlobales.montevideo, VariablesGlobales.pesos);
            }
            if (montevideoDolares.Length > 0)
            {
                await CrearArchivoPorCiudadYDivisa(montevideoDolares, VariablesGlobales.montevideo, VariablesGlobales.dolares);
            }
            if (cashOfficePesos.Length > 0)
            {
                await CrearArchivoCashOffice(cashOfficePesos, VariablesGlobales.pesos);
            }
            if (cashOfficeDolares.Length > 0)
            {
                await CrearArchivoCashOffice(cashOfficeDolares, VariablesGlobales.dolares);
            }
        }
        private async Task CrearArchivoPorCiudadYDivisa(StringBuilder contenido, string ciudad, string divisa)
        {

            if (contenido.Length == 0) return; // No crear archivos vacíos

            int numeroLineasContenido = LineCount(contenido.ToString());
            string numRegistro = numeroLineasContenido.ToString();

            contenido.Insert(0, "H;1\n");
            contenido.AppendLine("F;" + numRegistro);

            // Enviar archivo al servicio y obtener respuesta
            bool responseTens = generarYEnviarArchivoTens(contenido, ciudad, divisa);

            // Obtener la ruta base (que ya contiene la estructura de ciudad/divisa)
            string rutaArchivoBase = getRutaArchivoDAD(ciudad, divisa);
            string directorioBase = Path.GetDirectoryName(rutaArchivoBase); // Obtiene solo el directorio
            string fecha = DateTime.Now.ToString("ddMMyyyy"); // Fecha en formato ddMMyyyy

            // Determinar si se guarda en "APPROVED" o "NOT_APPROVED"
            string subcarpetaEstado = responseTens ? $"{fecha}_NO_ENVIADOS" : $"{fecha}_APPROVED";

            string directorioFinal = Path.Combine(directorioBase, subcarpetaEstado); // Ruta completa

            // Crear la carpeta si no existe
            if (!Directory.Exists(directorioFinal))
            {
                Directory.CreateDirectory(directorioFinal);
            }

            // Generar nuevo nombre de archivo sin sobrescribir el original

            string nombreArchivo = Path.GetFileName(rutaArchivoBase); 

            string rutaFinal = Path.Combine(directorioFinal, nombreArchivo); // Ruta donde se guardará

            // Guardar archivo en la ubicación correcta
            File.WriteAllText(rutaFinal, contenido.ToString());

            await Task.Delay(250);

        }

        private bool generarYEnviarArchivoTens(StringBuilder contenido, string ciudad, string divisa)
        {
            DateTime fecha = DateTime.Now;

            string rutaArchivo = getRutaArchivoDAD(ciudad, divisa);

            byte[] archivo = Encoding.UTF8.GetBytes(contenido.ToString());

            string nombreCSV = Path.GetFileName(rutaArchivo);

            TensStdr.transactionResponse tensResponse = ServicioSantander.getInstancia().EnviarArchivoConClienteWS(nombreCSV, archivo);

            return tensResponse == null;
        }

        // CREACION ARCHIVOS ESPECIFICAMENTE DE CASHOFFICE
        private async Task CrearArchivoCashOffice(StringBuilder content, string divisa)
        {
            if (content.Length == 0) return; // No crear archivos vacíos

            string ruta = "";
            int numeroLineasPesos = LineCount(content.ToString());

            string numRegistro = numeroLineasPesos.ToString();

            content.Insert(0, "H;1\n");

            content.AppendLine("F;" + numRegistro);

            bool responseTens = generarYEnviarArchivoTens(content, VariablesGlobales.cashoffice, divisa);


            if (divisa == VariablesGlobales.pesos)
            {
                ruta = _cashOfficeRutaPesosP2P + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
            }

            if (divisa == VariablesGlobales.dolares)
            {
                ruta = _cashOfficeRutaDolaresP2P + "TEC_" + _sucTecnisegurDolaresMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
            }

            string directorio = Path.GetDirectoryName(ruta);

            if (!Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            File.WriteAllText(ruta, content.ToString());

            await Task.Delay(150);
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
        private int LineCount(string str)
        {
            return str.Split('\n').Length - 1;
            //el menos uno es para no contar el ultimo salto de linea.
        }
    }
}
