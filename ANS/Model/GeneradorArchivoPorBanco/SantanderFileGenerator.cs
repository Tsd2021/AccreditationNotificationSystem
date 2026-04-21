using ANS.Model;
using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.Runtime;
using ANS.Runtime.Guards;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

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
        
        // ✅ Lock estático por sucursal para evitar colisiones de nombres cuando se generan archivos en paralelo
        private static readonly Dictionary<string, SemaphoreSlim> _locksPorSucursal = new Dictionary<string, SemaphoreSlim>();
        private static readonly object _lockDictionary = new object();
        /*
        private string _cashOfficeRutaDolaresP2P = @"\\172.16.10.20\cashoffice$\CashSantander\DOLARES\";
        private string _cashOfficeRutaPesosP2P = @"\\172.16.10.20\cashoffice$\CashSantander\PESOS\";
        private string _rutaDolaresP2P = @"\\172.16.10.20\puntoapuntocsvstdr$\DOLARES\";
        private string _rutaPesosP2P = @"\\172.16.10.20\puntoapuntocsvstdr$\PESOS\";
        */
        // ✅ Rutas ahora se obtienen desde ConfiguracionGlobal.Rutas (App.config)
        // Se mantienen estas variables privadas solo para compatibilidad con métodos legacy
        // que no han sido actualizados aún
        private Dictionary<int, int> CuentasTata = new Dictionary<int, int>
                {
                { 67, 1 },
                { 68, 1 },
                { 69, 2 },
                { 70, 2 },
                { 373,3 },
                { 374,3 }
                };
        public SantanderFileGenerator(ConfiguracionAcreditacion config)
        {
            _config = config;
        }

        /*
        public string getRutaArchivoDAD(string ciudad, string divisa)
        {

            if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.uyu)
            {
                return @"D:\CSVSANTANDER\" + "TEC_" + _sucTecnisegurPesosMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
            }
            else if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.usd)
            {
                return @"D:\CSVSANTANDER\" + "TEC_" + _sucTecnisegurDolaresMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
            }
            else if (ciudad.ToUpper() == VariablesGlobales.montevideo && divisa == VariablesGlobales.uyu)
            {
                return @"D:\CSVSANTANDER\" + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
            }
            else
                return @"D:\CSVSANTANDER\" + "TEC_" + _sucTecnisegurDolaresMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";

        }
        */
        // TEST TEST TEST TEST TEST TEST //
        //public string getRutaArchivoDAD2(string ciudad, string divisa)
        //{

        //    if (this._config.TipoAcreditacion == VariablesGlobales.p2p)
        //    {
        //        if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.uyu)
        //        {

        //            //Testing:
        //            //return @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SANTANDER\puntoapuntocsvtdr$\MALDONADO\PESOS\" + "TEC_" + _sucTecnisegurPesosMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //            //test Produccion:
        //            return @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SANTANDER\puntoapuntocsvtdr$\MALDONADO\PESOS\" + "TEC_" + _sucTecnisegurPesosMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";

        //        }
        //        else if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.usd)
        //        {
        //            //return @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SANTANDER\puntoapuntocsvstdr$\MALDONADO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //            return @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SANTANDER\puntoapuntocsvtdr$\MALDONADO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";

        //        }
        //        else if (ciudad.ToUpper() == VariablesGlobales.montevideo && divisa == VariablesGlobales.uyu)
        //        {
        //            //return @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SANTANDER\puntoapuntocsvstdr$\MONTEVIDEO\PESOS\" + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //            return @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SANTANDER\puntoapuntocsvtdr$\MONTEVIDEO\PESOS\" + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //        }
        //        else if (ciudad.ToUpper() == VariablesGlobales.montevideo && divisa == VariablesGlobales.usd)
        //        {
        //            //return @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SANTANDER\puntoapuntocsvstdr$\MONTEVIDEO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //            return @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SANTANDER\puntoapuntocsvtdr$\MONTEVIDEO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //        }
        //    }
        //    else if (this._config.TipoAcreditacion == VariablesGlobales.tanda)
        //    {
        //        if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.uyu)
        //        {
        //            //return @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SANTANDER\tanda$\MALDONADO\PESOS\" + "TEC_" + _sucTecnisegurPesosMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //            return @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SANTANDER\tanda$\MALDONADO\PESOS\" + "TEC_" + _sucTecnisegurPesosMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //        }
        //        else if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.usd)
        //        {
        //            //return @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SANTANDER\tanda$\MALDONADO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //            return @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SANTANDER\tanda$\MALDONADO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //        }
        //        else if (ciudad.ToUpper() == VariablesGlobales.montevideo && divisa == VariablesGlobales.uyu)
        //        {
        //            //return @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SANTANDER\tanda$\MONTEVIDEO\PESOS\" + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //            return @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SANTANDER\tanda$\MONTEVIDEO\PESOS\" + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //        }
        //        else
        //            //return @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SANTANDER\tanda$\MONTEVIDEO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //            return @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SANTANDER\tanda$\MONTEVIDEO\DOLARES\" + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //    }
        //    else if (this._config.TipoAcreditacion == VariablesGlobales.diaxdia)
        //    {
        //        if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.uyu)
        //        {
        //            //return @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SANTANDER\tanda$\MALDONADO\PESOS\" + "TEC_" + _sucTecnisegurPesosMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //            return @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SANTANDER\dxd$\MALDONADO\PESOS\" + "TEC_" + _sucTecnisegurPesosMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //        }
        //        else if (ciudad.ToUpper() == VariablesGlobales.maldonado && divisa == VariablesGlobales.usd)
        //        {
        //            //return @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SANTANDER\tanda$\MALDONADO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //            return @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SANTANDER\dxd$\MALDONADO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMald + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //        }
        //        else if (ciudad.ToUpper() == VariablesGlobales.montevideo && divisa == VariablesGlobales.uyu)
        //        {
        //            //return @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SANTANDER\tanda$\MONTEVIDEO\PESOS\" + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //            return @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SANTANDER\dxd$\MONTEVIDEO\PESOS\" + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //        }
        //        else
        //            //return @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SANTANDER\tanda$\MONTEVIDEO\DOLARES\" + "TEC_" + _sucTecnisegurDolaresMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //            return @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SANTANDER\dxd$\MONTEVIDEO\DOLARES\" + "TEC_" + _sucTecnisegurPesosMon + "_" + DateTime.Now.Year.ToString() + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd") + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm") + DateTime.Now.ToString("ss") + ".dat";
        //    }
        //    return "hola";
        //}

        /// <summary>
        /// ✅ Obtiene la ruta base del directorio según tipo de acreditación
        /// IMPORTANTE: 
        /// - NUNCA se separa por divisa (PESOS/DOLARES) - todos los archivos van juntos
        /// - NUNCA se separa por ciudad - todos los archivos van juntos
        /// - Los archivos se diferencian SOLO por el nombre (TEC_004, TEC_005, TEC_137, TEC_138)
        ///   donde el número de sucursal identifica ciudad y divisa:
        ///   - 004 = Montevideo Pesos
        ///   - 005 = Montevideo Dólares
        ///   - 137 = Maldonado Pesos
        ///   - 138 = Maldonado Dólares
        /// </summary>
        private string GetRutaBaseDirectorio(string ciudad, string divisa, bool esCashOffice = false)
        {
            var tipo = _config?.TipoAcreditacion;

            // ✅ Para CashOffice P2P: NO se separa por divisa
            if (esCashOffice)
            {
                return ConfiguracionGlobal.Rutas.SantanderCashOfficeP2P;
            }

            // ✅ Para todos los tipos: NO se separa por divisa ni ciudad
            // Estructura: Solo Tipo (todos los archivos juntos)
            var carpetaTipo = tipo switch
            {
                var t when t == VariablesGlobales.p2p => ConfiguracionGlobal.Rutas.SantanderPuntoAPunto,
                var t when t == VariablesGlobales.tanda => ConfiguracionGlobal.Rutas.SantanderTanda,
                var t when t == VariablesGlobales.diaxdia => ConfiguracionGlobal.Rutas.SantanderDiaADia,
                _ => throw new InvalidOperationException($"TipoAcreditacion desconocido: {tipo}")
            };

            // Estructura: Solo Tipo (SIN divisa, SIN ciudad) - Todos los archivos juntos
            return carpetaTipo;
        }

        /// <summary>
        /// ✅ Obtiene la ruta completa del archivo (incluye directorio base + nombre archivo)
        /// IMPORTANTE: La ciudad se usa SOLO para determinar la sucursal en el nombre del archivo,
        /// pero NO se incluye en la estructura de carpetas (solo divisa: PESOS/DOLARES)
        /// </summary>
        public string getRutaArchivoDAD(string ciudad, string divisa)
        {
            var ciudadUp = ciudad?.ToUpperInvariant();
            var divisaUp = divisa?.ToUpperInvariant();

            // Carpeta ciudad (solo para determinar sucursal, NO para estructura de carpetas)
            var carpetaCiudad = ciudadUp switch
            {
                var c when c == VariablesGlobales.maldonado => "MALDONADO",
                var c when c == VariablesGlobales.montevideo => "MONTEVIDEO",
                _ => throw new ArgumentException($"Ciudad no soportada: {ciudad}", nameof(ciudad))
            };

            var carpetaDivisa = divisaUp switch
            {
                var d when d == VariablesGlobales.uyu => "PESOS",
                var d when d == VariablesGlobales.usd => "DOLARES",
                _ => throw new ArgumentException($"Divisa no soportada: {divisa}", nameof(divisa))
            };

            // Selección de sucursal por combinación ciudad/divisa (para nombre de archivo)
            var sucursal = (carpetaCiudad, carpetaDivisa) switch
            {
                ("MALDONADO", "PESOS") => _sucTecnisegurPesosMald,
                ("MALDONADO", "DOLARES") => _sucTecnisegurDolaresMald,
                ("MONTEVIDEO", "PESOS") => _sucTecnisegurPesosMon,
                ("MONTEVIDEO", "DOLARES") => _sucTecnisegurDolaresMon,
                _ => throw new InvalidOperationException("Combinación ciudad/divisa no soportada")
            };

            // Timestamp compacto
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            // ✅ Obtener ruta base (NO incluye ciudad, solo tipo/divisa)
            var directorioBase = GetRutaBaseDirectorio(ciudad, divisa);
            Directory.CreateDirectory(directorioBase); // Asegura que exista

            // Nombre de archivo incluye sucursal (que refleja la ciudad), pero carpeta no
            var nombreArchivo = $"TEC_{sucursal}_{timestamp}.dat";
            return Path.Combine(directorioBase, nombreArchivo);
        }
        public async Task<GeneracionArchivoBancoResult> GenerarArchivo(List<CuentaBuzon> cb)
        {
            if (_config.TipoAcreditacion == VariablesGlobales.p2p)
            {
                return await GenerarLineasPorTotales(cb);
            }
            if (_config.TipoAcreditacion == VariablesGlobales.tanda)
            {
                return await GenerarLineasPorCuentasBuzones(cb);
            }
            if (_config.TipoAcreditacion == VariablesGlobales.diaxdia)
            {
                return await GenerarLineasPorCuentasBuzones(cb);
            }
            throw new Exception("Tipo de acreditación no soportado");
        }
        /// <summary>
        /// Genera líneas para archivos P2P (Punto a Punto)
        /// ✅ IMPORTANTE: La validación de Cash Office se hace contra el banco del BUZÓN (CC.BANCO), 
        /// no el banco de la cuenta (cuentasbuzones.BANCO). Los buzones con CC.BANCO = 'CASHOFFICE' 
        /// se envían a la ruta de Cash Office configurada en App.config.
        /// </summary>
        private async Task<GeneracionArchivoBancoResult> GenerarLineasPorTotales(List<CuentaBuzon> cb)
        {
            StringBuilder maldonadoPesos = new StringBuilder();
            StringBuilder maldonadoDolares = new StringBuilder();
            StringBuilder montevideoPesos = new StringBuilder();
            StringBuilder montevideoDolares = new StringBuilder();
            StringBuilder cashOfficePesos = new StringBuilder();
            StringBuilder cashOfficeDolares = new StringBuilder();

            // ✅ HashSet para evitar duplicados: clave única = NC + IdOperacion + Cuenta + Divisa + Monto
            var lineasAgregadas = new HashSet<string>();

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
                                    // ✅ Crear clave única para detectar duplicados
                                    string claveUnica = $"{unaCuenta.NC ?? ""}_{unDeposito.IdOperacion}_{unaCuenta.Cuenta ?? ""}_{unaCuenta.Divisa ?? ""}_{unTotal.ImporteTotal:F2}";
                                    
                                    // ✅ Verificar si ya se agregó esta línea
                                    if (lineasAgregadas.Contains(claveUnica))
                                    {
                                        ServicioLog.instancia.WriteWarning(
                                            $"Línea DUPLICADA detectada y omitida (P2P) | IDBuzon: {unaCuenta.NC ?? "N/A"} | " +
                                            $"IDOperacion: {unDeposito.IdOperacion} | Cuenta: {unaCuenta.Cuenta} | " +
                                            $"Divisa: {unaCuenta.Divisa ?? "NULL"} | Monto: {unTotal.ImporteTotal:F2}",
                                            "SantanderFileGenerator | GenerarLineasPorTotales");
                                        continue; // Omitir esta línea duplicada
                                    }
                                    
                                    bool agregadaAlArchivo = false;
                                    
                                    // ✅ Cash Office: Los buzones con CC.BANCO = 'CASHOFFICE' van SOLO a la ruta de Cash Office
                                    // Si es CashOffice, NO se verifica ciudad (prioridad absoluta)
                                    if (unaCuenta.esCashOffice())
                                    {
                                        if (unaCuenta.Divisa == VariablesGlobales.uyu)
                                        {
                                            agregarLineaAlStringBuilder_Individual(cashOfficePesos, unaCuenta, unDeposito, unTotal);
                                            agregadaAlArchivo = true;
                                        }
                                        else if (unaCuenta.Divisa == VariablesGlobales.usd)
                                        {
                                            agregarLineaAlStringBuilder_Individual(cashOfficeDolares, unaCuenta, unDeposito, unTotal);
                                            agregadaAlArchivo = true;
                                        }
                                    }
                                    // ✅ Solo verificar ciudad si NO es CashOffice
                                    else if (unaCuenta.Ciudad == VariablesGlobales.maldonado)
                                    {
                                        if (unaCuenta.Divisa == VariablesGlobales.uyu)
                                        {
                                            agregarLineaAlStringBuilder_Individual(maldonadoPesos, unaCuenta, unDeposito, unTotal);
                                            agregadaAlArchivo = true;
                                        }
                                        else if (unaCuenta.Divisa == VariablesGlobales.usd)
                                        {
                                            agregarLineaAlStringBuilder_Individual(maldonadoDolares, unaCuenta, unDeposito, unTotal);
                                            agregadaAlArchivo = true;
                                        }
                                    }
                                    else if (unaCuenta.Ciudad == VariablesGlobales.montevideo)
                                    {
                                        if (unaCuenta.Divisa == VariablesGlobales.uyu)
                                        {
                                            agregarLineaAlStringBuilder_Individual(montevideoPesos, unaCuenta, unDeposito, unTotal);
                                            agregadaAlArchivo = true;
                                        }
                                        else if (unaCuenta.Divisa == VariablesGlobales.usd)
                                        {
                                            agregarLineaAlStringBuilder_Individual(montevideoDolares, unaCuenta, unDeposito, unTotal);
                                            agregadaAlArchivo = true;
                                        }
                                    }
                                    
                                    // ✅ Marcar como agregada solo si se agregó al archivo
                                    if (agregadaAlArchivo)
                                    {
                                        lineasAgregadas.Add(claveUnica);
                                    }
                                    
                                    // ✅ Logging: Registrar depósitos que NO se agregaron al archivo pero tienen totales
                                    if (!agregadaAlArchivo)
                                    {
                                        ServicioLog.instancia.WriteWarning(
                                            $"Depósito EXCLUIDO del archivo txt (P2P) | IDBuzon: {unaCuenta.NC ?? "N/A"} | " +
                                            $"IDOperacion: {unDeposito.IdOperacion} | Cuenta: {unaCuenta.Cuenta} | " +
                                            $"Ciudad: {unaCuenta.Ciudad ?? "NULL"} | Divisa: {unaCuenta.Divisa ?? "NULL"} | " +
                                            $"Monto: {unTotal.ImporteTotal:F2} | EsCashOffice: {unaCuenta.esCashOffice()} | " +
                                            $"BancoBuzon: {unaCuenta.BancoBuzon ?? "NULL"} | BancoCuenta: {unaCuenta.Banco ?? "NULL"} | " +
                                            $"Razón: No cumple condiciones (debe ser CashOffice, MALDONADO o MONTEVIDEO)",
                                            "SantanderFileGenerator | GenerarLineasPorTotales");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return await CrearArchivo(maldonadoPesos, maldonadoDolares, montevideoPesos, montevideoDolares, cashOfficePesos, cashOfficeDolares);
        }
        /// <summary>
        /// Genera líneas para archivos Tanda y Día a Día (agrupados por cuenta)
        /// ✅ IMPORTANTE: La validación de Cash Office se hace contra el banco del BUZÓN (CC.BANCO), 
        /// no el banco de la cuenta (cuentasbuzones.BANCO). Los buzones con CC.BANCO = 'CASHOFFICE' 
        /// se envían a la ruta de Cash Office configurada en App.config.
        /// </summary>
        private async Task<GeneracionArchivoBancoResult> GenerarLineasPorCuentasBuzones(List<CuentaBuzon> cb)
        {
            StringBuilder maldonadoPesos = new StringBuilder();
            StringBuilder maldonadoDolares = new StringBuilder();
            StringBuilder montevideoPesos = new StringBuilder();
            StringBuilder montevideoDolares = new StringBuilder();
            StringBuilder cashOfficePesos = new StringBuilder();
            StringBuilder cashOfficeDolares = new StringBuilder();

            if (cb != null && cb.Count > 0)
            {
                // ✅ AGRUPACIÓN CORRECTA: Agrupar por Empresa + Ciudad + Divisa + CashOffice
                // IMPORTANTE: NO se agrupa por Cuenta ni Sucursal, solo por Empresa
                // Esto permite que todas las empresas de la misma cuenta queden juntas en el archivo
                // pero separadas por ciudad/divisa para generar archivos diferentes (004=Montevideo, 137=Maldonado)
                var gruposAgrupados = cb
                    .Where(c => c.Depositos != null && c.Depositos.Count > 0)
                    .GroupBy(c => new
                    {
                        // Clave de agrupación: Empresa + Ciudad + Divisa + CashOffice
                        // NO incluir Cuenta ni Sucursal para que se agrupen todas las empresas
                        Empresa = (c.Empresa ?? "").Trim(),
                        Ciudad = c.Ciudad,
                        Divisa = c.Divisa,
                        EsCashOffice = c.esCashOffice()
                    })
                    .Select(g => new
                    {
                        // Tomar Cuenta y Sucursal de la primera CuentaBuzon del grupo
                        // (todas las del mismo grupo deberían tener la misma cuenta, pero no es parte de la clave)
                        Cuenta = g.First().Cuenta,
                        Sucursal = g.First().SucursalCuenta,
                        Empresa = g.Key.Empresa,
                        Ciudad = g.Key.Ciudad,
                        Divisa = g.Key.Divisa,
                        EsCashOffice = g.Key.EsCashOffice,
                        IdCuenta = g.First().IdCuenta, // Para el diccionario CuentasTata
                        IdReferenciaAlCliente = g.First().IdReferenciaAlCliente ?? "", // Para construir la referencia final
                        // Sumar TODOS los depósitos de todas las CuentaBuzon que tienen la misma Empresa+Ciudad+Divisa
                        SumaMontos = g.Sum(c => c.Depositos.Sum(d => d.Totales.Sum(t => (double)t.ImporteTotal))),
                        CuentaBuzonEjemplo = g.First() // Para obtener otros datos necesarios
                    })
                    .OrderBy(x => x.Cuenta) // Ordenar por cuenta para que queden agrupadas
                    .ThenBy(x => x.Sucursal)
                    .ThenBy(x => x.Empresa) // Ordenar por empresa dentro de la misma cuenta
                    .ToList();

                foreach (var grupo in gruposAgrupados)
                {
                    if (grupo.SumaMontos > 0)
                    {
                        bool agregadaAlArchivo = false;
                        
                        // Crear una cuenta temporal con los datos del grupo para usar el método existente
                        var cuentaTemporal = grupo.CuentaBuzonEjemplo;
                        
                        // Construir la referencia final usando IdReferenciaAlCliente (para el formato del archivo)
                        // pero la agrupación se hizo por Empresa
                        string referenciaFinal = grupo.IdReferenciaAlCliente ?? "";
                        if (CuentasTata.ContainsKey(grupo.IdCuenta))
                        {
                            referenciaFinal = ReemplazarPrimerCaracter(grupo.IdReferenciaAlCliente ?? "", CuentasTata[grupo.IdCuenta]);
                        }
                        
                        // ✅ Logging: Registrar información del grupo antes de agregarlo
                        string ciudadNormalizada = grupo.Ciudad?.ToUpperInvariant()?.Trim() ?? "";
                        string divisaNormalizada = grupo.Divisa?.ToUpperInvariant()?.Trim() ?? "";
                        
                        ServicioLog.instancia.WriteInfo(
                            $"Procesando grupo | Cuenta: {grupo.Cuenta} | Empresa: {grupo.Empresa} | " +
                            $"Ciudad: '{grupo.Ciudad}' (normalizada: '{ciudadNormalizada}') | " +
                            $"Divisa: '{grupo.Divisa}' (normalizada: '{divisaNormalizada}') | " +
                            $"Monto: {grupo.SumaMontos:F2} | EsCashOffice: {grupo.EsCashOffice} | " +
                            $"BancoBuzon: {cuentaTemporal.BancoBuzon ?? "NULL"} | BancoCuenta: {cuentaTemporal.Banco ?? "NULL"}",
                            "SantanderFileGenerator | GenerarLineasPorCuentasBuzones");
                        
                        // ✅ Cash Office: Los buzones con CC.BANCO = 'CASHOFFICE' van a la ruta de Cash Office
                        // La validación se hace contra el banco del BUZÓN (CC.BANCO), no el banco de la cuenta (cuentasbuzones.BANCO)
                        if (grupo.EsCashOffice)
                        {
                            if (divisaNormalizada == VariablesGlobales.uyu)
                            {
                                agregarLineaAlStringBuilder_AgrupadoConReferencia(cashOfficePesos, cuentaTemporal, grupo.SumaMontos, referenciaFinal);
                                agregadaAlArchivo = true;
                                ServicioLog.instancia.WriteInfo(
                                    $"Agregado a CashOffice Pesos | Ruta: {ConfiguracionGlobal.Rutas.SantanderCashOfficeP2P} | " +
                                    $"BancoBuzon: {cuentaTemporal.BancoBuzon ?? "NULL"}",
                                    "SantanderFileGenerator");
                            }
                            else if (divisaNormalizada == VariablesGlobales.usd)
                            {
                                agregarLineaAlStringBuilder_AgrupadoConReferencia(cashOfficeDolares, cuentaTemporal, grupo.SumaMontos, referenciaFinal);
                                agregadaAlArchivo = true;
                                ServicioLog.instancia.WriteInfo(
                                    $"Agregado a CashOffice Dólares | Ruta: {ConfiguracionGlobal.Rutas.SantanderCashOfficeP2P} | " +
                                    $"BancoBuzon: {cuentaTemporal.BancoBuzon ?? "NULL"}",
                                    "SantanderFileGenerator");
                            }
                        }
                        else if (ciudadNormalizada == VariablesGlobales.maldonado.ToUpperInvariant())
                        {
                            if (divisaNormalizada == VariablesGlobales.uyu)
                            {
                                agregarLineaAlStringBuilder_AgrupadoConReferencia(maldonadoPesos, cuentaTemporal, grupo.SumaMontos, referenciaFinal);
                                agregadaAlArchivo = true;
                                ServicioLog.instancia.WriteInfo($"Agregado a Maldonado Pesos (TEC_137)", "SantanderFileGenerator");
                            }
                            else if (divisaNormalizada == VariablesGlobales.usd)
                            {
                                agregarLineaAlStringBuilder_AgrupadoConReferencia(maldonadoDolares, cuentaTemporal, grupo.SumaMontos, referenciaFinal);
                                agregadaAlArchivo = true;
                                ServicioLog.instancia.WriteInfo($"Agregado a Maldonado Dólares (TEC_138)", "SantanderFileGenerator");
                            }
                        }
                        else if (ciudadNormalizada == VariablesGlobales.montevideo.ToUpperInvariant())
                        {
                            if (divisaNormalizada == VariablesGlobales.uyu)
                            {
                                agregarLineaAlStringBuilder_AgrupadoConReferencia(montevideoPesos, cuentaTemporal, grupo.SumaMontos, referenciaFinal);
                                agregadaAlArchivo = true;
                                ServicioLog.instancia.WriteInfo($"Agregado a Montevideo Pesos (TEC_004)", "SantanderFileGenerator");
                            }
                            else if (divisaNormalizada == VariablesGlobales.usd)
                            {
                                agregarLineaAlStringBuilder_AgrupadoConReferencia(montevideoDolares, cuentaTemporal, grupo.SumaMontos, referenciaFinal);
                                agregadaAlArchivo = true;
                                ServicioLog.instancia.WriteInfo($"Agregado a Montevideo Dólares (TEC_005)", "SantanderFileGenerator");
                            }
                        }
                        
                        // ✅ Logging: Registrar cuentas que NO se agregaron al archivo
                        if (!agregadaAlArchivo)
                        {
                            ServicioLog.instancia.WriteWarning(
                                $"Grupo EXCLUIDO del archivo txt | Cuenta: {grupo.Cuenta} | " +
                                $"Sucursal: {grupo.Sucursal} | Empresa: {grupo.Empresa ?? "NULL"} | " +
                                $"Ciudad: '{grupo.Ciudad ?? "NULL"}' (normalizada: '{ciudadNormalizada}') | " +
                                $"Divisa: '{grupo.Divisa ?? "NULL"}' (normalizada: '{divisaNormalizada}') | " +
                                $"Monto: {grupo.SumaMontos:F2} | EsCashOffice: {grupo.EsCashOffice} | " +
                                $"Comparación Ciudad: Maldonado='{VariablesGlobales.maldonado.ToUpperInvariant()}' == '{ciudadNormalizada}'? {ciudadNormalizada == VariablesGlobales.maldonado.ToUpperInvariant()} | " +
                                $"Montevideo='{VariablesGlobales.montevideo.ToUpperInvariant()}' == '{ciudadNormalizada}'? {ciudadNormalizada == VariablesGlobales.montevideo.ToUpperInvariant()} | " +
                                $"Comparación Divisa: UYU='{VariablesGlobales.uyu}' == '{divisaNormalizada}'? {divisaNormalizada == VariablesGlobales.uyu} | " +
                                $"USD='{VariablesGlobales.usd}' == '{divisaNormalizada}'? {divisaNormalizada == VariablesGlobales.usd} | " +
                                $"Razón: No cumple condiciones (debe ser CashOffice, MALDONADO o MONTEVIDEO)",
                                "SantanderFileGenerator | GenerarLineasPorCuentasBuzones");
                        }
                    }
                }
            }

            return await CrearArchivo(maldonadoPesos, maldonadoDolares, montevideoPesos, montevideoDolares, cashOfficePesos, cashOfficeDolares);
        }
        private async Task<GeneracionArchivoBancoResult> CrearArchivo(StringBuilder maldonadoPesos, StringBuilder maldonadoDolares, StringBuilder montevideoPesos, StringBuilder montevideoDolares, StringBuilder cashOfficePesos, StringBuilder cashOfficeDolares)
        {
            // ✅ Lista para almacenar información de archivos generados (para envío en producción)
            var archivosGenerados = new List<(string rutaFinal, string nombreArchivo, byte[] contenidoBytes, string ciudad, string divisa)>();
            
            if (maldonadoPesos.Length > 0)
            {
                var archivos = await CrearArchivoPorCiudadYDivisa(maldonadoPesos, VariablesGlobales.maldonado, VariablesGlobales.uyu, false);
                archivosGenerados.AddRange(archivos);
            }
            if (maldonadoDolares.Length > 0)
            {
                var archivos = await CrearArchivoPorCiudadYDivisa(maldonadoDolares, VariablesGlobales.maldonado, VariablesGlobales.usd, false);
                archivosGenerados.AddRange(archivos);
            }
            if (montevideoPesos.Length > 0)
            {
                var archivos = await CrearArchivoPorCiudadYDivisa(montevideoPesos, VariablesGlobales.montevideo, VariablesGlobales.uyu, false);
                archivosGenerados.AddRange(archivos);
            }
            if (montevideoDolares.Length > 0)
            {
                var archivos = await CrearArchivoPorCiudadYDivisa(montevideoDolares, VariablesGlobales.montevideo, VariablesGlobales.usd, false);
                archivosGenerados.AddRange(archivos);
            }
            if (cashOfficePesos.Length > 0)
            {
                var archivos = await CrearArchivoCashOffice(cashOfficePesos, VariablesGlobales.uyu, false);
                archivosGenerados.AddRange(archivos);
            }
            if (cashOfficeDolares.Length > 0)
            {
                var archivos = await CrearArchivoCashOffice(cashOfficeDolares, VariablesGlobales.usd, false);
                archivosGenerados.AddRange(archivos);
            }
            
            // ============================================================================
            // ✅ IMPLEMENTACIÓN PARA PRODUCCIÓN (COMENTADA - ACTIVAR CUANDO SE NECESITE)
            // ============================================================================
            // Cuando se active EnviarArchivoConClienteWS en producción, se enviará CADA archivo
            // generado al servicio Santander. El flujo será:
            //
            // 1. Se generan todos los archivos y se guardan en disco
            // 2. Se recopila información de cada archivo (ruta, nombre, contenido en bytes)
            // 3. Al final, se itera sobre todos los archivos generados
            // 4. Para cada archivo, se llama a EnviarArchivoConClienteWS con:
            //    - nombreArchivo: el nombre del archivo (ej: "TEC_137_20251125143430.dat")
            //    - archivo: el contenido del archivo en bytes
            // 5. Se actualiza el estado del archivo según la respuesta:
            //    - Si responseTens == true: se mueve a carpeta APPROVED
            //    - Si responseTens == false: se mantiene en NO_ENVIADOS
            //
            // ✅ CÓDIGO PARA PRODUCCIÓN - ACTIVADO
            int archivosEnviadosExitosamente = 0;
            int archivosConError = 0;
            int archivosBloqueadosEnTest = 0;

            if (archivosGenerados.Count > 0)
            {
                // ✅ Logging específico según modo
                if (ANS.Runtime.AppRuntime.IsTest)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"═══════════════════════════════════════════════════════════════",
                        "SantanderFileGenerator | CrearArchivo | TEST MODE");
                    ServicioLog.instancia.WriteInfo(
                        $"MODO TEST: {archivosGenerados.Count} archivo(s) generado(s) con prefijo TEST_ | " +
                        $"WebService de Santander BLOQUEADO (no se enviará) | " +
                        $"Archivos guardados localmente en: {Path.GetDirectoryName(archivosGenerados.First().rutaFinal)}",
                        "SantanderFileGenerator | CrearArchivo | TEST MODE");
                    ServicioLog.instancia.WriteInfo(
                        $"═══════════════════════════════════════════════════════════════",
                        "SantanderFileGenerator | CrearArchivo | TEST MODE");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Iniciando envío de {archivosGenerados.Count} archivo(s) al servicio Santander",
                        "SantanderFileGenerator | CrearArchivo");
                }
                
                // ✅ Agrupar archivos por nombre para detectar duplicados
                var archivosPorNombre = archivosGenerados
                    .GroupBy(a => a.nombreArchivo)
                    .ToList();
                
                // ✅ Verificar si hay duplicados para optimizar el flujo
                bool hayDuplicados = archivosPorNombre.Any(g => g.Count() > 1);
                
                foreach (var grupoArchivos in archivosPorNombre)
                {
                    var archivosDelGrupo = grupoArchivos.ToList();
                    
                    // Si hay múltiples archivos con el mismo nombre, procesar secuencialmente
                    if (archivosDelGrupo.Count > 1)
                    {
                        ServicioLog.instancia.WriteInfo(
                            $"Detectados {archivosDelGrupo.Count} archivo(s) con el mismo nombre: {grupoArchivos.Key} | " +
                            $"Se procesarán secuencialmente para evitar duplicados",
                            "SantanderFileGenerator | CrearArchivo");
                    }
                    
                    for (int i = 0; i < archivosDelGrupo.Count; i++)
                    {
                        var archivoInfo = archivosDelGrupo[i];
                        string nombreArchivoOriginal = archivoInfo.nombreArchivo;
                        string rutaFinalOriginal = archivoInfo.rutaFinal;
                        
                        // Si no es el primer archivo del grupo, regenerar nombre con nuevo timestamp
                        if (i > 0)
                        {
                            // Esperar 1 segundo para asegurar que el timestamp sea diferente
                            await Task.Delay(1000);
                            
                            // Regenerar nombre con nuevo timestamp
                            var nuevoTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                            
                            // Extraer sucursal del nombre original (formato: TEC_{sucursal}_{timestamp}.dat)
                            var partesNombre = nombreArchivoOriginal.Split('_');
                            if (partesNombre.Length >= 2)
                            {
                                string sucursal = partesNombre[1];
                                string nuevoNombreArchivo = $"TEC_{sucursal}_{nuevoTimestamp}.dat";
                                
                                // Renombrar archivo en disco
                                string directorioArchivo = Path.GetDirectoryName(rutaFinalOriginal);
                                string nuevaRutaFinal = Path.Combine(directorioArchivo, nuevoNombreArchivo);
                                
                                if (File.Exists(rutaFinalOriginal))
                                {
                                    File.Move(rutaFinalOriginal, nuevaRutaFinal, overwrite: true);
                                    
                                    // Actualizar información del archivo
                                    byte[] contenidoBytesActualizado = File.ReadAllBytes(nuevaRutaFinal);
                                    archivoInfo = (nuevaRutaFinal, nuevoNombreArchivo, contenidoBytesActualizado, archivoInfo.ciudad, archivoInfo.divisa);
                                    
                                    ServicioLog.instancia.WriteInfo(
                                        $"Archivo renombrado para evitar duplicado | Nombre original: {nombreArchivoOriginal} | " +
                                        $"Nuevo nombre: {nuevoNombreArchivo}",
                                        "SantanderFileGenerator | CrearArchivo");
                                }
                            }
                        }
                        
                        try
                        {
                            // ✅ En TEST: WebService está bloqueado, no intentar enviar
                            if (ANS.Runtime.AppRuntime.IsTest)
                            {
                                archivosBloqueadosEnTest++;
                                ServicioLog.instancia.WriteInfo(
                                    $"TEST MODE: Archivo NO enviado (WebService bloqueado) | " +
                                    $"Archivo: {archivoInfo.nombreArchivo} | " +
                                    $"Ruta: {archivoInfo.rutaFinal} | " +
                                    $"Ciudad: {archivoInfo.ciudad} | Divisa: {archivoInfo.divisa} | " +
                                    $"El archivo se mantiene en carpeta NO_ENVIADOS (comportamiento esperado en TEST)",
                                    "SantanderFileGenerator | CrearArchivo | TEST MODE");
                                if (ANS.Runtime.AppRuntime.IsTest)
                                    return GeneracionArchivoBancoResult.ExitoSinRestricciones();
                                continue; // Saltar al siguiente archivo sin intentar enviar
                            }
                            
                            // Enviar archivo al servicio Santander (solo en PRODUCTION)
                            bool enviadoExitosamente = await ServicioSantander.getInstancia()
                                .EnviarArchivoConClienteWS(archivoInfo.nombreArchivo, archivoInfo.contenidoBytes);
                            
                            if (enviadoExitosamente)
                            {
                                archivosEnviadosExitosamente++;
                                
                                // Si se envió exitosamente, mover a carpeta APPROVED
                                string fecha = DateTime.Now.ToString("ddMMyyyy");
                                string directorioBase = Path.GetDirectoryName(archivoInfo.rutaFinal);
                                string directorioApproved = Path.Combine(
                                    Path.GetDirectoryName(directorioBase), 
                                    $"{fecha}_APPROVED");
                                
                                if (!Directory.Exists(directorioApproved))
                                    Directory.CreateDirectory(directorioApproved);
                                
                                string rutaApproved = Path.Combine(directorioApproved, archivoInfo.nombreArchivo);
                                
                                // Mover archivo de NO_ENVIADOS a APPROVED
                                if (File.Exists(archivoInfo.rutaFinal))
                                {
                                    File.Move(archivoInfo.rutaFinal, rutaApproved, overwrite: true);
                                    
                                    ServicioLog.instancia.WriteInfo(
                                        $"Archivo movido a APPROVED | {archivoInfo.nombreArchivo} | " +
                                        $"Ciudad: {archivoInfo.ciudad} | Divisa: {archivoInfo.divisa}",
                                        "SantanderFileGenerator | CrearArchivo");
                                }
                            }
                            else
                            {
                                archivosConError++;
                                ServicioLog.instancia.WriteWarning(
                                    $"Archivo NO enviado exitosamente | {archivoInfo.nombreArchivo} | " +
                                    $"Ciudad: {archivoInfo.ciudad} | Divisa: {archivoInfo.divisa} | " +
                                    $"Se mantiene en carpeta NO_ENVIADOS",
                                    "SantanderFileGenerator | CrearArchivo");
                            }
                        }
                        catch (InvalidOperationException ex) when (ANS.Runtime.AppRuntime.IsTest && ex.Message.Contains("BLOQUEO EN TEST"))
                        {
                            // ✅ Excepción esperada en TEST: WebService bloqueado
                            archivosBloqueadosEnTest++;
                            ServicioLog.instancia.WriteInfo(
                                $"TEST MODE: WebService bloqueado (comportamiento esperado) | " +
                                $"Archivo: {archivoInfo.nombreArchivo} | " +
                                $"Ruta: {archivoInfo.rutaFinal} | " +
                                $"Ciudad: {archivoInfo.ciudad} | Divisa: {archivoInfo.divisa} | " +
                                $"Mensaje: {ex.Message}",
                                "SantanderFileGenerator | CrearArchivo | TEST MODE - WebService Bloqueado");
                        }
                        catch (Exception ex)
                        {
                            archivosConError++;
                            ServicioLog.instancia.WriteLog(ex, "Santander", 
                                $"Error al enviar archivo {archivoInfo.nombreArchivo}");
                        }
                    }
                }
                
                // ✅ Resumen detallado según modo
                if (ANS.Runtime.AppRuntime.IsTest)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"═══════════════════════════════════════════════════════════════",
                        "SantanderFileGenerator | CrearArchivo | TEST MODE - RESUMEN");
                    ServicioLog.instancia.WriteInfo(
                        $"RESUMEN TEST MODE | Total archivos generados: {archivosGenerados.Count} | " +
                        $"Archivos con prefijo TEST_: {archivosGenerados.Count} | " +
                        $"WebService bloqueado (no enviados): {archivosBloqueadosEnTest} | " +
                        $"Archivos con error: {archivosConError} | " +
                        $"Archivos enviados exitosamente: {archivosEnviadosExitosamente} (debe ser 0 en TEST)",
                        "SantanderFileGenerator | CrearArchivo | TEST MODE - RESUMEN");
                    ServicioLog.instancia.WriteInfo(
                        $"NOTA: En TEST, los archivos se generan localmente con prefijo TEST_ pero NO se envían al WebService.",
                        "SantanderFileGenerator | CrearArchivo | TEST MODE - RESUMEN");
                    ServicioLog.instancia.WriteInfo(
                        $"═══════════════════════════════════════════════════════════════",
                        "SantanderFileGenerator | CrearArchivo | TEST MODE - RESUMEN");
                }
                else
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Resumen de envío a Santander | Total archivos: {archivosGenerados.Count} | " +
                        $"Enviados exitosamente: {archivosEnviadosExitosamente} | " +
                        $"Con error: {archivosConError}",
                        "SantanderFileGenerator | CrearArchivo");
                }

                if (ANS.Runtime.AppRuntime.IsTest)
                    return GeneracionArchivoBancoResult.ExitoSinRestricciones();

                int totalArchivos = archivosGenerados.Count;
                if (archivosEnviadosExitosamente == totalArchivos && archivosConError == 0)
                    return GeneracionArchivoBancoResult.ExitoSinRestricciones();

                return GeneracionArchivoBancoResult.SantanderEnvioWebServiceFallido(
                    $"Santander WebService: enviados_ok={archivosEnviadosExitosamente}/{totalArchivos}, con_error={archivosConError}");
            }
            
            // ============================================================================
            // ⚠️ IMPLEMENTACIÓN DE TESTING - COMENTADA PARA PRODUCCIÓN
            // ============================================================================
            // Código de testing desactivado - ahora se usa el código de producción arriba
            /*
            if (archivosGenerados.Count > 0)
            {
                ServicioLog.instancia.WriteInfo(
                    $"Enviando notificación al servicio Santander (TESTING) | Total archivos generados: {archivosGenerados.Count}",
                    "SantanderFileGenerator | CrearArchivo");
                
                await ServicioSantander.getInstancia().EnviarArchivoVacioConCliente();
            }
            */
            return GeneracionArchivoBancoResult.ExitoSinRestricciones();
        }
        /// <summary>
        /// ✅ Crea uno o múltiples archivos si el contenido supera 500 líneas.
        /// Divide automáticamente el contenido en chunks de máximo 500 líneas para evitar timeouts.
        /// </summary>
        private async Task<List<(string rutaFinal, string nombreArchivo, byte[] contenidoBytes, string ciudad, string divisa)>> CrearArchivoPorCiudadYDivisa(StringBuilder contenido, string ciudad, string divisa, bool enviarATens = false)
        {
            var archivosGenerados = new List<(string rutaFinal, string nombreArchivo, byte[] contenidoBytes, string ciudad, string divisa)>();

            if (contenido.Length == 0) 
                return archivosGenerados; // No crear archivos vacíos

            // ✅ Obtener contenido original (StringBuilder) - mantener referencia para modificar
            StringBuilder contenidoBuilder = contenido;
            
            // ✅ Verificar si el contenido tiene más de 500 líneas (sin contar header/footer)
            int numeroLineasContenido = LineCount(contenido.ToString());
            
            // ✅ Obtener ruta base usando el nuevo método centralizado
            string directorioBase = GetRutaBaseDirectorio(ciudad, divisa);
            
            // ✅ Generar nombre de archivo base (mismo formato que antes)
            string ciudadUp = ciudad?.ToUpperInvariant();
            string divisaUp = divisa?.ToUpperInvariant();
            
            var carpetaCiudad = ciudadUp switch
            {
                var c when c == VariablesGlobales.maldonado => "MALDONADO",
                var c when c == VariablesGlobales.montevideo => "MONTEVIDEO",
                _ => "N/A"
            };
            
            var carpetaDivisa = divisaUp switch
            {
                var d when d == VariablesGlobales.uyu => "PESOS",
                var d when d == VariablesGlobales.usd => "DOLARES",
                _ => "N/A"
            };

            var sucursal = (carpetaCiudad, carpetaDivisa) switch
            {
                ("MALDONADO", "PESOS") => _sucTecnisegurPesosMald,
                ("MALDONADO", "DOLARES") => _sucTecnisegurDolaresMald,
                ("MONTEVIDEO", "PESOS") => _sucTecnisegurPesosMon,
                ("MONTEVIDEO", "DOLARES") => _sucTecnisegurDolaresMon,
                _ => "000" // Fallback
            };

            string fecha = DateTime.Now.ToString("ddMMyyyy");
            bool responseTens = false; // Siempre false porque el envío se hace al final
            string subcarpetaEstado = responseTens ? $"{fecha}_APPROVED" : $"{fecha}_NO_ENVIADOS";
            string directorioFinal = Path.Combine(directorioBase, subcarpetaEstado);

            // Crear la carpeta si no existe
            if (!Directory.Exists(directorioFinal))
            {
                Directory.CreateDirectory(directorioFinal);
            }

            // ✅ Si el contenido tiene más de 500 líneas, dividirlo en chunks
            if (numeroLineasContenido > 500)
            {
                ServicioLog.instancia.WriteInfo(
                    $"Contenido supera 500 líneas ({numeroLineasContenido} líneas) | " +
                    $"Se dividirá en múltiples archivos para evitar timeout | " +
                    $"Ciudad: {ciudad} | Divisa: {divisa}",
                    "SantanderFileGenerator | CrearArchivoPorCiudadYDivisa");

                // Dividir contenido en chunks de máximo 500 líneas
                string contenidoOriginal = contenidoBuilder.ToString();
                var chunks = DividirContenidoEnChunks(contenidoOriginal, maxLineasPorChunk: 500);

                // Crear un archivo para cada chunk
                for (int i = 0; i < chunks.Count; i++)
                {
                    // Generar nombre único manteniendo el formato TEC_{sucursal}_{timestamp}.dat
                    // Verifica que no exista otro archivo con el mismo nombre
                    string nombreArchivoOriginal = await GenerarNombreArchivoUnico(sucursal, directorioFinal, i);
                    // Aplicar prefijo TEST_ si está en modo TEST
                    string nombreArchivo = FileSystemGuard.GetFileNameWithTestPrefix(nombreArchivoOriginal);
                    string rutaFinal = Path.Combine(directorioFinal, nombreArchivo);
                    
                    // ✅ GUARDIA: Validar que la escritura esté permitida
                    FileSystemGuard.EnsureWriteAllowed(rutaFinal, "SantanderFileGenerator | CrearArchivoPorCiudadYDivisa");
                    
                    // ✅ Logging específico en TEST
                    if (ANS.Runtime.AppRuntime.IsTest && nombreArchivo != nombreArchivoOriginal)
                    {
                        ServicioLog.instancia.WriteInfo(
                            $"TEST MODE: Prefijo TEST_ aplicado al archivo | " +
                            $"Nombre original: {nombreArchivoOriginal} | " +
                            $"Nombre final: {nombreArchivo} | " +
                            $"Ruta: {rutaFinal}",
                            "SantanderFileGenerator | CrearArchivoPorCiudadYDivisa | TEST MODE");
                    }
                    
                    // Asegurar que el directorio existe
                    Directory.CreateDirectory(directorioFinal);
                    
                    // Guardar archivo
                    File.WriteAllText(rutaFinal, chunks[i]);
                    
                    // Convertir contenido a bytes para envío
                    byte[] contenidoBytes = Encoding.UTF8.GetBytes(chunks[i]);
                    
                    int totalLineas = LineCount(chunks[i]);
                    ServicioLog.instancia.WriteInfo(
                        $"Archivo {i + 1}/{chunks.Count} generado exitosamente | " +
                        $"Ruta: {rutaFinal} | Total líneas: {totalLineas} | " +
                        $"Ciudad: {ciudad} | Divisa: {divisa}",
                        "SantanderFileGenerator | CrearArchivoPorCiudadYDivisa");

                    archivosGenerados.Add((rutaFinal, nombreArchivo, contenidoBytes, ciudad, divisa));
                    
                    // Pequeña pausa entre archivos para asegurar timestamps únicos (al menos 1 segundo)
                    if (i < chunks.Count - 1)
                    {
                        await Task.Delay(1000); // Esperar 1 segundo para que el timestamp cambie
                    }
                }
            }
            else
            {
                // ✅ Contenido tiene 500 líneas o menos, crear un solo archivo normalmente
                // MANTENER LA LÓGICA ORIGINAL EXACTA que funcionaba bien
                
                // Contar líneas (como se hacía originalmente)
                int numRegistro = numeroLineasContenido;
                string numRegistroStr = numRegistro.ToString();
                
                // Agregar header y footer directamente al StringBuilder (como se hacía originalmente)
                contenidoBuilder.Insert(0, "H;1\n");
                contenidoBuilder.AppendLine("F;" + numRegistroStr);
                
                // Generar nombre de archivo único (índice 0 = primer archivo)
                // Verifica que no exista otro archivo con el mismo nombre
                string nombreArchivoOriginal = await GenerarNombreArchivoUnico(sucursal, directorioFinal, 0);
                // Aplicar prefijo TEST_ si está en modo TEST
                string nombreArchivo = FileSystemGuard.GetFileNameWithTestPrefix(nombreArchivoOriginal);
                string rutaFinal = Path.Combine(directorioFinal, nombreArchivo);
                
                // ✅ GUARDIA: Validar que la escritura esté permitida
                FileSystemGuard.EnsureWriteAllowed(rutaFinal, "SantanderFileGenerator | CrearArchivoPorCiudadYDivisa");
                
                // ✅ Logging específico en TEST
                if (ANS.Runtime.AppRuntime.IsTest && nombreArchivo != nombreArchivoOriginal)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"TEST MODE: Prefijo TEST_ aplicado al archivo | " +
                        $"Nombre original: {nombreArchivoOriginal} | " +
                        $"Nombre final: {nombreArchivo} | " +
                        $"Ruta: {rutaFinal}",
                        "SantanderFileGenerator | CrearArchivoPorCiudadYDivisa | TEST MODE");
                }
                
                // Asegurar que el directorio existe
                Directory.CreateDirectory(directorioFinal);
                
                // Guardar archivo (como se hacía originalmente)
                string contenidoFinal = contenidoBuilder.ToString();
                File.WriteAllText(rutaFinal, contenidoFinal);
                
                byte[] contenidoBytes = Encoding.UTF8.GetBytes(contenidoFinal);
                
                int totalLineas = LineCount(contenidoFinal);
                ServicioLog.instancia.WriteInfo(
                    $"Archivo generado exitosamente | Ruta: {rutaFinal} | Total líneas: {totalLineas} | " +
                    $"Ciudad: {ciudad} | Divisa: {divisa} | Aprobado: {responseTens}",
                    "SantanderFileGenerator | CrearArchivoPorCiudadYDivisa");

                archivosGenerados.Add((rutaFinal, nombreArchivo, contenidoBytes, ciudad, divisa));
            }

            await Task.Delay(250);
            
            return archivosGenerados;
        }
        // ============================================================================
        // ⚠️ MÉTODO OBSOLETO - Ya NO se usa en el flujo actual
        // ============================================================================
        // Este método se usaba para enviar archivos individualmente durante la generación.
        // Ahora el envío se hace de forma centralizada al final en el método CrearArchivo.
        //
        // Si en el futuro se necesita enviar archivos individualmente (no recomendado),
        // se puede actualizar este método para usar EnviarArchivoConClienteWS:
        //
        // CÓDIGO PARA PRODUCCIÓN (si se quisiera reactivar):
        /*
        private async Task<bool> generarYEnviarArchivoTens(StringBuilder contenido, string ciudad, string divisa)
        {
            string rutaArchivo = getRutaArchivoDAD(ciudad, divisa);
            byte[] archivo = Encoding.UTF8.GetBytes(contenido.ToString());
            string nombreArchivo = Path.GetFileName(rutaArchivo);

            // Enviar archivo real al servicio Santander
            bool enviadoExitosamente = await ServicioSantander.getInstancia()
                .EnviarArchivoConClienteWS(nombreArchivo, archivo);
            
            return enviadoExitosamente; // Retorna true si código de respuesta fue "0"
        }
        */
        //
        // NOTA: Actualmente este método NO se llama porque 'enviarATens' siempre es 'false'
        // ============================================================================
        [Obsolete("Este método ya no se usa. El envío se hace centralizado en CrearArchivo. Ver código comentado arriba para implementación en producción.")]
        private async Task<bool> generarYEnviarArchivoTens(StringBuilder contenido, string ciudad, string divisa)
        {
            // ⚠️ CÓDIGO LEGACY - Solo para compatibilidad, NO se ejecuta en el flujo actual
            // Este método ya no se llama porque 'enviarATens' siempre es 'false' en CrearArchivo
            
            DateTime fecha = DateTime.Now;
            string rutaArchivo = getRutaArchivoDAD(ciudad, divisa);
            byte[] archivo = Encoding.UTF8.GetBytes(contenido.ToString());
            string nombreCSV = Path.GetFileName(rutaArchivo);

            // Código comentado para producción:
            // bool enviadoExitosamente = await ServicioSantander.getInstancia()
            //     .EnviarArchivoConClienteWS(nombreCSV, archivo);
            // return enviadoExitosamente;
            
            // Código actual (solo para testing, no se ejecuta):
            await ServicioSantander.getInstancia().EnviarArchivoVacioConCliente();
            return false;
        }
        /// <summary>
        /// ✅ Crea uno o múltiples archivos CashOffice si el contenido supera 500 líneas.
        /// Divide automáticamente el contenido en chunks de máximo 500 líneas para evitar timeouts.
        /// </summary>
        private async Task<List<(string rutaFinal, string nombreArchivo, byte[] contenidoBytes, string ciudad, string divisa)>> CrearArchivoCashOffice(StringBuilder content, string divisa, bool enviarATens = false)
        {
            var archivosGenerados = new List<(string rutaFinal, string nombreArchivo, byte[] contenidoBytes, string ciudad, string divisa)>();

            if (content.Length == 0) 
                return archivosGenerados; // No crear archivos vacíos

            // ✅ Obtener contenido original (StringBuilder) - mantener referencia para modificar
            StringBuilder contenidoBuilder = content;
            int numeroLineasContenido = LineCount(contenidoBuilder.ToString());

            // ✅ NOTA: El parámetro 'enviarATens' y el método 'generarYEnviarArchivoTens' ya NO se usan
            // porque el envío se hace de forma centralizada al final en el método CrearArchivo
            // Por ahora, todos van a NO_ENVIADOS. En producción, el estado se actualizará después del envío
            bool responseTens = false; // Siempre false porque el envío se hace al final

            // ✅ Usar rutas desde configuración
            string directorioBase = GetRutaBaseDirectorio(VariablesGlobales.montevideo, divisa, esCashOffice: true);
            
            // Generar código de sucursal
            string sucursal = divisa == VariablesGlobales.uyu 
                ? _sucTecnisegurPesosMon 
                : _sucTecnisegurDolaresMon;

            // ✅ Estructura de carpetas: directorioBase/{fecha}_NO_ENVIADOS (o _APPROVED)
            string fecha = DateTime.Now.ToString("ddMMyyyy");
            string subcarpetaEstado = responseTens ? $"{fecha}_APPROVED" : $"{fecha}_NO_ENVIADOS";
            string directorioFinal = Path.Combine(directorioBase, subcarpetaEstado);

            if (!Directory.Exists(directorioFinal))
            {
                Directory.CreateDirectory(directorioFinal);
            }

            // ✅ Si el contenido tiene más de 500 líneas, dividirlo en chunks
            if (numeroLineasContenido > 500)
            {
                ServicioLog.instancia.WriteInfo(
                    $"Contenido CashOffice supera 500 líneas ({numeroLineasContenido} líneas) | " +
                    $"Se dividirá en múltiples archivos para evitar timeout | Divisa: {divisa}",
                    "SantanderFileGenerator | CrearArchivoCashOffice");

                // Dividir contenido en chunks de máximo 500 líneas
                string contenidoOriginal = contenidoBuilder.ToString();
                var chunks = DividirContenidoEnChunks(contenidoOriginal, maxLineasPorChunk: 500);

                // Crear un archivo para cada chunk
                for (int i = 0; i < chunks.Count; i++)
                {
                    // Generar nombre único manteniendo el formato TEC_{sucursal}_{timestamp}.dat
                    // Verifica que no exista otro archivo con el mismo nombre
                    string nombreArchivoOriginal = await GenerarNombreArchivoUnico(sucursal, directorioFinal, i);
                    // Aplicar prefijo TEST_ si está en modo TEST
                    string nombreArchivo = FileSystemGuard.GetFileNameWithTestPrefix(nombreArchivoOriginal);
                    string rutaFinal = Path.Combine(directorioFinal, nombreArchivo);
                    
                    // ✅ GUARDIA: Validar que la escritura esté permitida
                    FileSystemGuard.EnsureWriteAllowed(rutaFinal, "SantanderFileGenerator | CrearArchivoCashOffice");
                    
                    // ✅ Logging específico en TEST
                    if (ANS.Runtime.AppRuntime.IsTest && nombreArchivo != nombreArchivoOriginal)
                    {
                        ServicioLog.instancia.WriteInfo(
                            $"TEST MODE: Prefijo TEST_ aplicado al archivo CashOffice | " +
                            $"Nombre original: {nombreArchivoOriginal} | " +
                            $"Nombre final: {nombreArchivo} | " +
                            $"Ruta: {rutaFinal}",
                            "SantanderFileGenerator | CrearArchivoCashOffice | TEST MODE");
                    }
                    
                    // Asegurar que el directorio existe
                    Directory.CreateDirectory(directorioFinal);
                    
                    // Guardar archivo
                    File.WriteAllText(rutaFinal, chunks[i]);
                    
                    // Convertir contenido a bytes para envío
                    byte[] contenidoBytes = Encoding.UTF8.GetBytes(chunks[i]);
                    
                    int totalLineas = LineCount(chunks[i]);
                    ServicioLog.instancia.WriteInfo(
                        $"Archivo CashOffice {i + 1}/{chunks.Count} generado exitosamente | " +
                        $"Ruta: {rutaFinal} | Total líneas: {totalLineas} | Divisa: {divisa}",
                        "SantanderFileGenerator | CrearArchivoCashOffice");

                    archivosGenerados.Add((rutaFinal, nombreArchivo, contenidoBytes, VariablesGlobales.cashoffice, divisa));
                    
                    // Pequeña pausa entre archivos para asegurar timestamps únicos (al menos 1 segundo)
                    if (i < chunks.Count - 1)
                    {
                        await Task.Delay(1000); // Esperar 1 segundo para que el timestamp cambie
                    }
                }
            }
            else
            {
                // ✅ Contenido tiene 500 líneas o menos, crear un solo archivo normalmente
                // MANTENER LA LÓGICA ORIGINAL EXACTA que funcionaba bien
                
                // Contar líneas (como se hacía originalmente)
                int numRegistro = numeroLineasContenido;
                string numRegistroStr = numRegistro.ToString();
                
                // Agregar header y footer directamente al StringBuilder (como se hacía originalmente)
                contenidoBuilder.Insert(0, "H;1\n");
                contenidoBuilder.AppendLine("F;" + numRegistroStr);
                
                // Generar nombre de archivo único (índice 0 = primer archivo)
                // Verifica que no exista otro archivo con el mismo nombre
                string nombreArchivoOriginal = await GenerarNombreArchivoUnico(sucursal, directorioFinal, 0);
                // Aplicar prefijo TEST_ si está en modo TEST
                string nombreArchivo = FileSystemGuard.GetFileNameWithTestPrefix(nombreArchivoOriginal);
                string rutaFinal = Path.Combine(directorioFinal, nombreArchivo);
                
                // ✅ GUARDIA: Validar que la escritura esté permitida
                FileSystemGuard.EnsureWriteAllowed(rutaFinal, "SantanderFileGenerator | CrearArchivoCashOffice");
                
                // ✅ Logging específico en TEST
                if (AppRuntime.IsTest && nombreArchivo != nombreArchivoOriginal)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"TEST MODE: Prefijo TEST_ aplicado al archivo CashOffice | " +
                        $"Nombre original: {nombreArchivoOriginal} | " +
                        $"Nombre final: {nombreArchivo} | " +
                        $"Ruta: {rutaFinal}",
                        "SantanderFileGenerator | CrearArchivoCashOffice | TEST MODE");
                }
                
                // Guardar archivo (como se hacía originalmente)
                string contenidoFinal = contenidoBuilder.ToString();
                File.WriteAllText(rutaFinal, contenidoFinal);
                
                byte[] contenidoBytes = Encoding.UTF8.GetBytes(contenidoFinal);
                
                int totalLineas = LineCount(contenidoFinal);
                ServicioLog.instancia.WriteInfo(
                    $"Archivo generado exitosamente (CashOffice) | Ruta: {rutaFinal} | Total líneas: {totalLineas} | " +
                    $"Divisa: {divisa}",
                    "SantanderFileGenerator | CrearArchivoCashOffice");

                archivosGenerados.Add((rutaFinal, nombreArchivo, contenidoBytes, VariablesGlobales.cashoffice, divisa));
            }

            await Task.Delay(150);
            
            return archivosGenerados;
        }
        //METODO PARA CREAR LINEAS EN ARCHIVOS DIA A DIA Y TANDA!
        private void agregarLineaAlStringBuilder_Agrupado(StringBuilder lineas, CuentaBuzon unaCuenta, double totalPorCuenta)
        {

            string referencia = unaCuenta.IdReferenciaAlCliente;
            if (CuentasTata.ContainsKey(unaCuenta.IdCuenta))
            {
                referencia = ReemplazarPrimerCaracter(unaCuenta.IdReferenciaAlCliente, CuentasTata[unaCuenta.IdCuenta]);
            }

            // Formatear sucursal a 4 dígitos y cuenta a 12 dígitos
            var sucursalFormateada = unaCuenta.SucursalCuenta.PadLeft(4, '0');
            var cuentaFormateada = unaCuenta.Cuenta.PadLeft(12, '0');

            // Construir línea
            string linea = $"{_tipoRegistro};{_tipoOperacion};" +
                $"{sucursalFormateada};{cuentaFormateada};" +
                $"{unaCuenta.Divisa};{totalPorCuenta}00;" +
                $"{_tipoMovimiento};{_tipoDetalle};{referencia}";
            
            lineas.AppendLine(linea);
            
            // ✅ Logging: Registrar cada línea escrita al txt (agrupado)
            ServicioLog.instancia.WriteInfo(
                $"Línea escrita al txt (agrupado) | IDBuzon: {unaCuenta.NC ?? "N/A"} | " +
                $"Cuenta: {unaCuenta.Cuenta} | Sucursal: {unaCuenta.SucursalCuenta} | " +
                $"Divisa: {unaCuenta.Divisa} | MontoTotal: {totalPorCuenta:F2} | " +
                $"Referencia: {referencia} | Línea completa: {linea}",
                "SantanderFileGenerator | agregarLineaAlStringBuilder_Agrupado");
        }
        
        // ✅ NUEVO MÉTODO: Similar al anterior pero acepta la referencia como parámetro
        // Esto permite agrupar correctamente por cuenta+sucursal+referencia
        private void agregarLineaAlStringBuilder_AgrupadoConReferencia(StringBuilder lineas, CuentaBuzon unaCuenta, double totalPorCuenta, string referencia)
        {
            // Formatear sucursal a 4 dígitos y cuenta a 12 dígitos
            var sucursalFormateada = unaCuenta.SucursalCuenta.PadLeft(4, '0');
            var cuentaFormateada = unaCuenta.Cuenta.PadLeft(12, '0');

            // Construir línea
            string linea = $"{_tipoRegistro};{_tipoOperacion};" +
                $"{sucursalFormateada};{cuentaFormateada};" +
                $"{unaCuenta.Divisa};{totalPorCuenta}00;" +
                $"{_tipoMovimiento};{_tipoDetalle};{referencia}";
            
            lineas.AppendLine(linea);
            
            // ✅ Logging: Registrar cada línea escrita al txt (agrupado con referencia específica)
            ServicioLog.instancia.WriteInfo(
                $"Línea escrita al txt (agrupado por referencia) | IDBuzon: {unaCuenta.NC ?? "N/A"} | " +
                $"Cuenta: {unaCuenta.Cuenta} | Sucursal: {unaCuenta.SucursalCuenta} | " +
                $"Divisa: {unaCuenta.Divisa} | MontoTotal: {totalPorCuenta:F2} | " +
                $"Referencia: {referencia} | Línea completa: {linea}",
                "SantanderFileGenerator | agregarLineaAlStringBuilder_AgrupadoConReferencia");
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

            // Formatear sucursal a 4 dígitos y cuenta a 12 dígitos
            var sucursalFormateada = cb.SucursalCuenta.PadLeft(4, '0');
            var cuentaFormateada = cb.Cuenta.PadLeft(12, '0');

            string linea = $"{_tipoRegistro};{_tipoOperacion};" +
                $"{sucursalFormateada};{cuentaFormateada};" +
                $"{cb.Divisa};{tot.ImporteTotal}00;" +
                $"{_tipoMovimiento};{_tipoDetalle};{referenciaDetalle}";
            
            sb.AppendLine(linea);
            
            // ✅ Logging: Registrar cada línea escrita al txt (individual/P2P)
            ServicioLog.instancia.WriteInfo(
                $"Línea escrita al txt (individual/P2P) | IDBuzon: {cb.NC ?? "N/A"} | " +
                $"IDOperacion: {depo.IdOperacion} | Cuenta: {cb.Cuenta} | " +
                $"Sucursal: {cb.SucursalCuenta} | Divisa: {cb.Divisa} | " +
                $"Monto: {tot.ImporteTotal:F2} | Referencia: {referenciaDetalle} | " +
                $"Línea completa: {linea}",
                "SantanderFileGenerator | agregarLineaAlStringBuilder_Individual");
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

        /// <summary>
        /// ✅ Divide el contenido en chunks de máximo 500 líneas (sin contar header y footer).
        /// Cada chunk se formatea con su propio header y footer.
        /// </summary>
        /// <param name="contenidoOriginal">Contenido original sin header ni footer</param>
        /// <returns>Lista de chunks formateados, cada uno con header y footer</returns>
        private List<string> DividirContenidoEnChunks(string contenidoOriginal, int maxLineasPorChunk = 500)
        {
            var chunks = new List<string>();
            
            if (string.IsNullOrWhiteSpace(contenidoOriginal))
                return chunks;

            // Dividir el contenido en líneas (excluyendo líneas vacías al final)
            var lineas = contenidoOriginal
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (lineas.Count == 0)
                return chunks;

            // Si el contenido tiene 500 líneas o menos, retornar un solo chunk
            if (lineas.Count <= maxLineasPorChunk)
            {
                string contenidoCompleto = string.Join("\n", lineas);
                int numRegistro = lineas.Count;
                string chunkFormateado = $"H;1\n{contenidoCompleto}\nF;{numRegistro}";
                chunks.Add(chunkFormateado);
                return chunks;
            }

            // Dividir en chunks de máximo 500 líneas
            int indiceInicio = 0;
            int numeroChunk = 1;
            
            while (indiceInicio < lineas.Count)
            {
                int lineasEnChunk = Math.Min(maxLineasPorChunk, lineas.Count - indiceInicio);
                var lineasChunk = lineas.Skip(indiceInicio).Take(lineasEnChunk).ToList();
                
                string contenidoChunk = string.Join("\n", lineasChunk);
                int numRegistro = lineasChunk.Count;
                
                // Formatear chunk con header y footer
                string chunkFormateado = $"H;1\n{contenidoChunk}\nF;{numRegistro}";
                chunks.Add(chunkFormateado);
                
                ServicioLog.instancia.WriteInfo(
                    $"Chunk {numeroChunk} creado | Líneas: {lineasEnChunk} | " +
                    $"Líneas totales procesadas: {indiceInicio + lineasEnChunk} de {lineas.Count}",
                    "SantanderFileGenerator | DividirContenidoEnChunks");
                
                indiceInicio += lineasEnChunk;
                numeroChunk++;
            }

            ServicioLog.instancia.WriteInfo(
                $"Contenido dividido en {chunks.Count} archivo(s) | Total líneas originales: {lineas.Count} | " +
                $"Máximo líneas por archivo: {maxLineasPorChunk}",
                "SantanderFileGenerator | DividirContenidoEnChunks");

            return chunks;
        }

        /// <summary>
        /// ✅ Obtiene o crea un semáforo para una sucursal específica.
        /// Esto asegura que solo un archivo se genere a la vez por sucursal, evitando colisiones.
        /// </summary>
        private SemaphoreSlim ObtenerLockPorSucursal(string sucursal)
        {
            lock (_lockDictionary)
            {
                if (!_locksPorSucursal.ContainsKey(sucursal))
                {
                    _locksPorSucursal[sucursal] = new SemaphoreSlim(1, 1);
                }
                return _locksPorSucursal[sucursal];
            }
        }

        /// <summary>
        /// ✅ Obtiene todas las rutas base posibles donde podría existir un archivo Santander con la misma sucursal.
        /// Esto incluye: P2P, CashOffice, Tanda, y Día a Día.
        /// </summary>
        private List<string> ObtenerTodasLasRutasBasePosibles()
        {
            var rutas = new List<string>();
            
            try
            {
                // Agregar todas las rutas base posibles de Santander
                if (!string.IsNullOrWhiteSpace(ConfiguracionGlobal.Rutas.SantanderPuntoAPunto))
                    rutas.Add(ConfiguracionGlobal.Rutas.SantanderPuntoAPunto);
                
                if (!string.IsNullOrWhiteSpace(ConfiguracionGlobal.Rutas.SantanderCashOfficeP2P))
                    rutas.Add(ConfiguracionGlobal.Rutas.SantanderCashOfficeP2P);
                
                if (!string.IsNullOrWhiteSpace(ConfiguracionGlobal.Rutas.SantanderTanda))
                    rutas.Add(ConfiguracionGlobal.Rutas.SantanderTanda);
                
                if (!string.IsNullOrWhiteSpace(ConfiguracionGlobal.Rutas.SantanderDiaADia))
                    rutas.Add(ConfiguracionGlobal.Rutas.SantanderDiaADia);
            }
            catch (Exception ex)
            {
                ServicioLog.instancia.WriteWarning(
                    $"Error al obtener rutas base para verificación de archivos: {ex.Message}",
                    "SantanderFileGenerator | ObtenerTodasLasRutasBasePosibles");
            }
            
            return rutas;
        }

        /// <summary>
        /// ✅ Verifica si un archivo existe en TODAS las carpetas posibles de Santander (P2P, CashOffice, Tanda, Día a Día).
        /// Busca en todas las subcarpetas posibles (con fechas como {fecha}_NO_ENVIADOS, {fecha}_APPROVED, etc.)
        /// </summary>
        /// <param name="nombreArchivo">Nombre del archivo a buscar (ej: "TEC_004_20251125134000.dat")</param>
        /// <returns>True si el archivo existe en alguna carpeta, False si no existe en ninguna</returns>
        private bool VerificarArchivoEnTodasLasCarpetas(string nombreArchivo)
        {
            var rutasBase = ObtenerTodasLasRutasBasePosibles();
            
            foreach (var rutaBase in rutasBase)
            {
                try
                {
                    // Verificar si la carpeta base existe
                    if (!Directory.Exists(rutaBase))
                        continue;
                    
                    // Buscar el archivo directamente en la carpeta base
                    string rutaDirecta = Path.Combine(rutaBase, nombreArchivo);
                    if (File.Exists(rutaDirecta))
                    {
                        ServicioLog.instancia.WriteInfo(
                            $"Archivo encontrado en carpeta base | Ruta: {rutaDirecta} | Nombre: {nombreArchivo}",
                            "SantanderFileGenerator | VerificarArchivoEnTodasLasCarpetas");
                        return true;
                    }
                    
                    // Buscar en todas las subcarpetas (pueden tener formato {fecha}_NO_ENVIADOS, {fecha}_APPROVED, etc.)
                    var subcarpetas = Directory.GetDirectories(rutaBase, "*", SearchOption.TopDirectoryOnly);
                    foreach (var subcarpeta in subcarpetas)
                    {
                        string rutaEnSubcarpeta = Path.Combine(subcarpeta, nombreArchivo);
                        if (File.Exists(rutaEnSubcarpeta))
                        {
                            ServicioLog.instancia.WriteInfo(
                                $"Archivo encontrado en subcarpeta | Ruta: {rutaEnSubcarpeta} | Nombre: {nombreArchivo}",
                                "SantanderFileGenerator | VerificarArchivoEnTodasLasCarpetas");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Si hay error al acceder a una carpeta, continuar con las demás
                    ServicioLog.instancia.WriteWarning(
                        $"Error al verificar archivo en ruta {rutaBase}: {ex.Message}",
                        "SantanderFileGenerator | VerificarArchivoEnTodasLasCarpetas");
                    continue;
                }
            }
            
            return false; // No se encontró el archivo en ninguna carpeta
        }

        /// <summary>
        /// ✅ Genera un nombre de archivo único manteniendo el formato original TEC_{sucursal}_{timestamp}.dat
        /// Usa un lock por sucursal para evitar colisiones cuando se generan archivos en paralelo.
        /// Verifica que el archivo NO exista en NINGUNA carpeta de Santander (P2P, CashOffice, Tanda, Día a Día).
        /// Si existe en alguna carpeta, espera y genera otro timestamp.
        /// </summary>
        /// <param name="sucursal">Código de sucursal (ej: "137")</param>
        /// <param name="directorioFinal">Directorio donde se guardará el archivo</param>
        /// <param name="indiceChunk">Índice del chunk (0 para el primer archivo, 1+ para archivos adicionales)</param>
        /// <returns>Nombre de archivo único en formato TEC_{sucursal}_{timestamp}.dat</returns>
        private async Task<string> GenerarNombreArchivoUnico(string sucursal, string directorioFinal, int indiceChunk)
        {
            // ✅ Obtener lock para esta sucursal (evita que dos procesos generen archivos con la misma sucursal al mismo tiempo)
            var semaforo = ObtenerLockPorSucursal(sucursal);
            
            await semaforo.WaitAsync();
            try
            {
                int intentos = 0;
                const int maxIntentos = 10; // Máximo 10 intentos para evitar loops infinitos
                
                while (intentos < maxIntentos)
                {
                    // Generar timestamp
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                    string nombreArchivo = $"TEC_{sucursal}_{timestamp}.dat";
                    
                    // ✅ Verificar si el archivo existe en TODAS las carpetas posibles de Santander
                    // (P2P, CashOffice, Tanda, Día a Día) para garantizar unicidad global
                    if (!VerificarArchivoEnTodasLasCarpetas(nombreArchivo))
                    {
                        // El nombre es único en todas las carpetas, retornarlo
                        ServicioLog.instancia.WriteInfo(
                            $"Nombre de archivo único verificado | Nombre: {nombreArchivo} | Sucursal: {sucursal} | " +
                            $"Directorio destino: {directorioFinal}",
                            "SantanderFileGenerator | GenerarNombreArchivoUnico");
                        return nombreArchivo;
                    }
                    
                    // El archivo existe en alguna carpeta, esperar un segundo y generar otro timestamp
                    intentos++;
                    ServicioLog.instancia.WriteWarning(
                        $"Archivo ya existe en alguna carpeta, generando nuevo timestamp | Intento: {intentos}/{maxIntentos} | " +
                        $"Nombre: {nombreArchivo} | Sucursal: {sucursal} | Directorio destino: {directorioFinal}",
                        "SantanderFileGenerator | GenerarNombreArchivoUnico");
                    
                    // Esperar 1 segundo antes del siguiente intento
                    await Task.Delay(1000);
                }
                
                // Si después de todos los intentos no se encontró un nombre único,
                // agregar milisegundos al timestamp como último recurso
                // (aunque esto cambiaría el formato, es mejor que fallar)
                var timestampFinal = DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                string nombreFinal = $"TEC_{sucursal}_{timestampFinal}.dat";
                
                ServicioLog.instancia.WriteWarning(
                    $"No se pudo generar nombre único después de {maxIntentos} intentos | " +
                    $"Usando timestamp con milisegundos: {nombreFinal} | Sucursal: {sucursal}",
                    "SantanderFileGenerator | GenerarNombreArchivoUnico");
                
                return nombreFinal;
            }
            finally
            {
                semaforo.Release();
            }
        }
    }
}
