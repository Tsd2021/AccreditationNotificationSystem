using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.Runtime.Guards;
using System.IO;
using System.Linq;
using System.Text;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class ScotiaFileGenerator : IBancoModoAcreditacion
    {

        string _rutOrdenante = "000000215437010016";
        string _tipoOperativa = "02";
        string _fecha;
        string _importe = "0";
        string _signo = "+";
        string _moneda = "0";
        string _nroCuenta = "0";
        string _sucursal;
        private ConfiguracionAcreditacion _config { get; set; }

        public async Task GenerarArchivo(List<CuentaBuzon> cb)
        {

            if (cb == null || cb.Count == 0)
            {
                throw new Exception("No hay cuentas para generar el archivo");
            }

            armarStringParaTxt_Agrupado(cb);

        }

        public ScotiaFileGenerator(ConfiguracionAcreditacion config)
        {
            _config = config;
        }

        //Armar String para P2P - DEJAR POR SI SE PRECISA EN UN FUTURO
        /*
        private void armarStringParaTxt(List<CuentaBuzon> cb)
        {
            // Agrupar las cuentas por divisa
            var cuentasAgrupadas = cb.GroupBy(cuenta => cuenta.Divisa);

            foreach (var grupo in cuentasAgrupadas)
            {
                StringBuilder _txt = new StringBuilder();

                foreach (CuentaBuzon _unaCuenta in grupo)
                {
                    foreach (Deposito _unDeposito in _unaCuenta.Depositos)
                    {
                        // Creamos un StringBuilder para cada línea y lo reiniciamos en cada iteración.
                        StringBuilder _unaLineaTxt = new StringBuilder();

                        // Asignar la fecha (puedes ajustar el formato si es necesario)
                        _fecha = DateTime.Today.ToString("ddMMyyyy");
                        _signo = "+";

                        // Calcular el importe con el formato deseado
                        _importe = _unDeposito.Totales.Sum(i => i.ImporteTotal).ToString("F2");

                        // Definir la moneda según la divisa de la cuenta
                        if (_unaCuenta.Divisa == VariablesGlobales.uyu)
                        {
                            _moneda = "00";
                        }
                        else if (_unaCuenta.Divisa == VariablesGlobales.usd)
                        {
                            _moneda = "01";
                        }

                        // Preparar el número de cuenta
                        _nroCuenta = _unaCuenta.Cuenta;
                        _sucursal = _unaCuenta.SucursalCuenta;

                        while (_nroCuenta.Length < 11)
                        {
                            _nroCuenta = "0" + _nroCuenta;
                        }

                        if (_moneda == "00")
                        {
                            _nroCuenta = "2101" + "0000" + _nroCuenta;
                        }
                        else if (_moneda == "01")
                        {
                            _nroCuenta = "2101" + "2225" + _nroCuenta;
                        }

                        // Armado de la línea (asegúrate de que las longitudes y espacios sean los requeridos)
                        _unaLineaTxt.Append(_rutOrdenante);
                        _unaLineaTxt.Append(' ', 3);
                        _unaLineaTxt.Append(_tipoOperativa);
                        _unaLineaTxt.Append(_fecha);
                        _unaLineaTxt.Append(' ', 21);
                        _unaLineaTxt.Append(_importe);
                        _unaLineaTxt.Append(_signo);
                        _unaLineaTxt.Append(_moneda);
                        _unaLineaTxt.Append(_nroCuenta);
                        _unaLineaTxt.Append(' ', 2);
                        _unaLineaTxt.Append(' ', 40);
                        _unaLineaTxt.Append(' ', 12);
                        _unaLineaTxt.Append(' ', 9);
                        _unaLineaTxt.Append(' ', 1);
                        _unaLineaTxt.Append(' ', 30);
                        _unaLineaTxt.Append(' ', 692);

                        _txt.AppendLine(_unaLineaTxt.ToString());
                    }
                }

                // Determinar la ruta en base a la divisa (grupo.Key)
                string directory = "";
                if (grupo.Key == VariablesGlobales.uyu)
                {
                    directory = @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SCOTIABANK\puntoapunto\PESOS\";
                }
                else if (grupo.Key == VariablesGlobales.usd)
                {
                    directory = @"C:\Users\dchiquiar\Desktop\ACREDITACIONES TEST\SCOTIABANK\puntoapunto\DOLARES\";
                }

                // Generar el nombre del archivo con el mismo formato que el código viejo
                // Ejemplo: "Acreditacionbuzones4_2_2025.xls" para el 4 de febrero de 2025
                string fileName = "Acreditacionbuzones"
                    + DateTime.Now.Day.ToString() + "_"
                    + DateTime.Now.Month.ToString() + "_"
                    + DateTime.Now.Year.ToString() + ".txt";

                // Combinar la ruta del directorio con el nombre del archivo
                string rutaDestino = Path.Combine(directory, fileName);

                // Llama al método que se encarga de crear el directorio (si no existe) y escribir el archivo
                crearYEscribirArchivo(_txt, rutaDestino);
            }
        }
        */
        /*
        private void armarStringParaTxt_Agrupado(List<CuentaBuzon> cb)
        {
            // Agrupar las cuentas por Divisa y Ciudad (se asume que "Ciudad" es una propiedad de CuentaBuzon)
            var cuentasAgrupadas = cb.GroupBy(cuenta => new { cuenta.Divisa, cuenta.Ciudad });

            foreach (var grupo in cuentasAgrupadas)
            {
                // Extraer la divisa y la ciudad para el grupo actual
                string divisa = grupo.Key.Divisa;
                string ciudad = grupo.Key.Ciudad; // Debe ser "MONTEVIDEO" o "MALDONADO"

                // StringBuilder que contendrá todas las líneas del archivo para este grupo (por divisa y ciudad)
                StringBuilder _txt = new StringBuilder();

                foreach (CuentaBuzon _unaCuenta in grupo)
                {
                    // Creamos un StringBuilder nuevo para cada línea (resumen por cuenta)
                    StringBuilder _unaLineaTxt = new StringBuilder();

                    // Asignar la fecha (formato: ddMMyyyy)
                    _fecha = DateTime.Today.ToString("ddMMyyyy");
                    _signo = "+";

                    // Calcular el importe total de la cuenta sumando todos los importes de sus depósitos
                    decimal totalImporteCuenta = _unaCuenta.Depositos.Sum(d => d.Totales.Sum(i => i.ImporteTotal));
                    // Para la línea del txt se formatea el importe multiplicándolo por 10^7 y rellenándolo a 15 dígitos
                    _importe = ((long)(totalImporteCuenta * 10000000)).ToString().PadLeft(15, '0');

                    // Definir la moneda según la divisa de la cuenta
                    if (divisa == VariablesGlobales.uyu)
                    {
                        _moneda = "00";
                    }
                    else if (divisa == VariablesGlobales.usd)
                    {
                        _moneda = "01";
                    }

                    // Preparar el número de cuenta y la sucursal
                    _nroCuenta = _unaCuenta.Cuenta;
                    _sucursal = _unaCuenta.SucursalCuenta;

                    // Asegurarse de que el número de cuenta tenga al menos 11 dígitos (rellena con ceros a la izquierda)
                    while (_nroCuenta.Length < 11)
                    {
                        _nroCuenta = "0" + _nroCuenta;
                    }

                    // Prependemos el prefijo según la moneda
                    if (_moneda == "00")
                    {
                        _nroCuenta = "2101" + "0000" + _nroCuenta;
                    }
                    else if (_moneda == "01")
                    {
                        _nroCuenta = "2101" + "2225" + _nroCuenta;
                    }

                    // Armado de la línea de texto (ajusta los espacios y longitudes para llegar a los 876 caracteres requeridos)
                    _unaLineaTxt.Append(_rutOrdenante);             // Campo: Rut Ordenante (longitud fija)
                    _unaLineaTxt.Append(' ', 3);                     // 3 espacios
                    _unaLineaTxt.Append(_tipoOperativa);             // Campo: Tipo Operativa (longitud fija)
                    _unaLineaTxt.Append(_fecha);                     // Fecha en formato ddMMyyyy (8 caracteres)
                    _unaLineaTxt.Append(' ', 21);                    // 21 espacios
                    _unaLineaTxt.Append(_importe);                   // Importe en 15 dígitos (por ejemplo, "000000060000000")
                    _unaLineaTxt.Append(_signo);                     // Signo "+"
                    _unaLineaTxt.Append(_moneda);                    // Moneda ("00" o "01")
                    _unaLineaTxt.Append(_nroCuenta);                 // Número de cuenta formateado (con prefijo)
                    _unaLineaTxt.Append(' ', 2);                     // 2 espacios
                    _unaLineaTxt.Append(' ', 40);                    // 40 espacios
                    _unaLineaTxt.Append(' ', 12);                    // 12 espacios
                    _unaLineaTxt.Append(' ', 9);                     // 9 espacios
                    _unaLineaTxt.Append(' ', 1);                     // 1 espacio
                    _unaLineaTxt.Append(' ', 30);                    // 30 espacios
                    _unaLineaTxt.Append(' ', 692);                   // 692 espacios

                    // Agregar la línea del resumen de la cuenta al texto final
                    _txt.AppendLine(_unaLineaTxt.ToString());
                }

                // Calcular el total de los importes para el grupo (suma de importes de cada cuenta)
                decimal groupTotalImporte = grupo.Sum(cuenta => cuenta.Depositos.Sum(d => d.Totales.Sum(i => i.ImporteTotal)));
                // Se usa la parte entera (sin decimales) para la construcción del nombre del archivo
                string totalImportesString = ((long)groupTotalImporte).ToString();

                // Generar el nombre de archivo. Se incorpora la ciudad en el nombre:
                // Formato: dd-MM-yyyy-HH-mm-<Ciudad>-<Divisa><TotalImportes>-AcreditacionBuzonesTecnisegurMont.txt
                // Ejemplo: "04-07-2024-16-01-MONTEVIDEO-UYU4386500-AcreditacionBuzonesTecnisegurMont.txt"
                string dateTimePart = DateTime.Now.ToString("dd-MM-yyyy-HH-mm");
                string currencyCode = (divisa == VariablesGlobales.uyu) ? "UYU" : "USD";
                string fileName = $"{dateTimePart}-{ciudad}-{currencyCode}{totalImportesString}-AcreditacionBuzonesTecnisegur.txt";

                // Determinar el directorio de salida según la divisa y la ciudad
                // Ejemplo de subcarpetas: "MONTEVIDEO PESOS" o "MALDONADO DOLARES"
                string subFolder = "";
                if (divisa == VariablesGlobales.uyu)
                {
                    subFolder = $"{ciudad} PESOS";
                }
                else if (divisa == VariablesGlobales.usd)
                {
                    subFolder = $"{ciudad} DOLARES";
                }
                // Ruta Test Producción:
                string baseDirectory = @"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\TXT\SCOTIABANK";
                // Ruta Test Local:
                //string baseDirectory = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SCOTIABANK";
                // Combinar para obtener la ruta completa
                string directory = Path.Combine(baseDirectory, subFolder);
                string rutaDestino = Path.Combine(directory, fileName);

                // Llama al método que se encarga de crear el directorio (si no existe) y escribir el archivo
                crearYEscribirArchivo(_txt, rutaDestino);
            }
        }

        private void crearYEscribirArchivo(StringBuilder txt, string route)
        {
            // ✅ GUARDIA: Validar que la escritura esté permitida
            FileSystemGuard.EnsureWriteAllowed(route, "ScotiaFileGenerator | crearYEscribirArchivo");
            
            // Obtener el directorio a partir de la ruta completa
            string directory = Path.GetDirectoryName(route);

            // Verificar si el directorio existe, y si no, crearlo
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Aplicar prefijo TEST_ al nombre de archivo si está en modo TEST
            string fileName = Path.GetFileName(route);
            string dir = Path.GetDirectoryName(route);
            fileName = FileSystemGuard.GetFileNameWithTestPrefix(fileName);
            route = Path.Combine(dir, fileName);

            // Escribir el contenido en el archivo
            File.WriteAllText(route, txt.ToString());
        }
        */
        /// <summary>
        /// Genera archivos TXT agrupados para Scotiabank
        /// ✅ IMPORTANTE: La validación de Cash Office se hace contra el banco del BUZÓN (CC.BANCO), 
        /// no el banco de la cuenta (cuentasbuzones.BANCO). Los buzones con CC.BANCO = 'CASHOFFICE' 
        /// se envían a la ruta de Cash Office configurada en App.config.
        /// </summary>
        private void armarStringParaTxt_Agrupado(List<CuentaBuzon> cb)
        {
            if (cb == null || !cb.Any())
                throw new Exception("No hay cuentas para generar el archivo");

            // ✅ Agrupo por DIVISA + CIUDAD (cada grupo genera un archivo)
            var grupos = cb.GroupBy(c => new { c.Divisa, c.Ciudad });

            foreach (var grupo in grupos)
            {
                var txt = new StringBuilder();

                // 2) Dentro de cada DIVISA/Ciudad, agrupo por CUENTA+SUCURSAL y sumo depósitos
                foreach (var gCuenta in grupo.GroupBy(c => new { c.Cuenta, c.SucursalCuenta }))
                {
                    var ejemplo = gCuenta.First();

                    // Fecha y signo
                    string fecha = DateTime.Today.ToString("ddMMyyyy");
                    string signo = "+";

                    // Total de esta cuenta (suma de todos los depósitos)
                    decimal totalImporte = gCuenta
                        .Sum(c => c.Depositos
                            .Sum(d => d.Totales
                                .Sum(i => i.ImporteTotal)));

                    // ✅ Recolectar información de depósitos para logging (resumida)
                    var depositosInfo = gCuenta
                        .SelectMany(c => (c.Depositos ?? new List<Deposito>())
                            .Select(d => new { 
                                c.NC, 
                                d.IdOperacion, 
                                Monto = d.Totales?.Sum(t => t.ImporteTotal) ?? 0 
                            }))
                        .ToList();
                    
                    int totalDepositos = depositosInfo.Count;
                    string divisaCodigo = grupo.Key.Divisa == VariablesGlobales.uyu ? "UYU" : "USD";

                    long parteEntera = (long)totalImporte;
                    // (parte entera + "00"), pad a 15 dígitos
                    string importe = (parteEntera.ToString() + "00")
                        .PadLeft(15, '0');

                    // Moneda: "00" o "01"
                    string moneda = ejemplo.Divisa == VariablesGlobales.uyu
                        ? "00" : "01";

                    // Número de cuenta: limpiar espacios y pad 11 + prefijo según moneda
                    string cuentaLimpia = (ejemplo.Cuenta ?? "").Trim().Replace(" ", "");
                    string nroCuenta = cuentaLimpia.PadLeft(11, '0');
                    nroCuenta = moneda == "00"
                        ? "2101" + "0000" + nroCuenta
                        : "2101" + "2225" + nroCuenta;

                    // 876 caracteres
                    var linea = new StringBuilder();
                    linea
                        .Append(_rutOrdenante)   // 18
                        .Append(' ', 3)          //  3
                        .Append(_tipoOperativa)  //  2
                        .Append(fecha)           //  8
                        .Append(' ', 21)         // 21
                        .Append(importe)         // 15
                        .Append(signo)           //  1
                        .Append(moneda)          //  2
                        .Append(nroCuenta)       // 19
                        .Append(' ', 2)          //  2
                        .Append(' ', 40)         // 40
                        .Append(' ', 12)         // 12
                        .Append(' ', 9)          //  9
                        .Append(' ', 1)          //  1
                        .Append(' ', 30)         // 30
                        .Append(' ', 692);       // 692

                    string lineaFinal = linea.ToString();
                    txt.AppendLine(lineaFinal);
                    
                    // ✅ Logging mejorado: Formato más amigable y legible
                    // Solo mostrar primeros 5 depósitos si hay muchos, para no saturar el log
                    string resumenDepositos;
                    if (totalDepositos <= 5)
                    {
                        resumenDepositos = string.Join(", ", depositosInfo.Select(d => 
                            $"{d.NC}(${d.Monto:F2})"));
                    }
                    else
                    {
                        var primeros5 = depositosInfo.Take(5).Select(d => 
                            $"{d.NC}(${d.Monto:F2})");
                        resumenDepositos = string.Join(", ", primeros5) + 
                            $" ... y {totalDepositos - 5} más";
                    }
                    
                    ServicioLog.instancia.WriteInfo(
                        $"IdCuenta: {ejemplo.IdCuenta} | " +
                        $"Cuenta: {ejemplo.Cuenta} | Sucursal: {ejemplo.SucursalCuenta} | " +
                        $"Divisa: {divisaCodigo} | Total: ${totalImporte:N2} | " +
                        $"Depósitos ({totalDepositos}): {resumenDepositos}",
                        "ScotiaFileGenerator | armarStringParaTxt_Agrupado");
                }

                // 3) Obtengo de la primera cuenta del grupo los valores externos
                var ejemploGrupo = grupo.First();
                // Asumimos que ConfiguracionAcreditacion tiene propiedad 'Henderson'
                //int henderson = ejemploGrupo.esHenderson() ?? 0;
                string ciudad = grupo.Key.Ciudad;
                
                // ✅ Logging: Registrar información del grupo antes de procesarlo
                ServicioLog.instancia.WriteInfo(
                    $"Procesando grupo Scotiabank | Ciudad: '{ciudad}' | Divisa: {grupo.Key.Divisa} | " +
                    $"BancoBuzon: {ejemploGrupo.BancoBuzon ?? "NULL"} | " +
                    $"BancoCuenta: {ejemploGrupo.Banco ?? "NULL"}",
                    "ScotiaFileGenerator | armarStringParaTxt_Agrupado");

                // Calculo 'Modo' y 'suctecni'
                //string modo = henderson == 1 ? "Henderson_Tanda1"
                //            : henderson == 2 ? "Henderson_Tanda2"
                //            : "";

                string modo = "";

                string suctecni = ciudad == "MONTEVIDEO" ? "Mont" : "Mald";

                // Rutas según ciudad (usando configuración centralizada)
                string rutaBase = ciudad == "MONTEVIDEO"
                    ? ConfiguracionGlobal.Rutas.ScotiabankMontevideo
                    : ConfiguracionGlobal.Rutas.ScotiabankMaldonado;
                string basePath = rutaBase;

                // Carpeta diaria yyyy-MM-dd
                string folderName = DateTime.Now.ToString("yyyy-MM-dd");
                string folderPath = Path.Combine(basePath, folderName);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Nombre de archivo
                long totalGrupo = (long)grupo
                    .Sum(c => c.Depositos
                        .Sum(d => d.Totales
                            .Sum(i => i.ImporteTotal)));
                string divCode = grupo.Key.Divisa == VariablesGlobales.uyu ? "UYU" : "USD";
                string timestamp = DateTime.Now.ToString("dd-MM-yyyy-HH-mm");
                
                // Agregar tipo de acreditación al nombre del archivo si está disponible
                string tipoAcreditacionSufijo = "";
                
                // ✅ PRIORIDAD 1: Verificar si es cliente Farmashop/Coboe (ID 179)
                // Si alguna cuenta del grupo pertenece a Farmashop, usar sufijo específico
                bool esFarmashop = grupo.Any(c => c.IdCliente == 179);
                if (esFarmashop)
                {
                    tipoAcreditacionSufijo = "_Farmashop";
                }
                // ✅ PRIORIDAD 2: Si no es Farmashop, usar el tipo de acreditación de la configuración
                else if (_config != null && !string.IsNullOrWhiteSpace(_config.TipoAcreditacion))
                {
                    // Normalizar el nombre del tipo de acreditación para el sufijo
                    string tipoNormalizado = _config.TipoAcreditacion;
                    if (tipoNormalizado.Equals("Tanda1", StringComparison.OrdinalIgnoreCase) ||
                        tipoNormalizado.Equals("Tanda", StringComparison.OrdinalIgnoreCase))
                    {
                        tipoAcreditacionSufijo = "_Tanda1";
                    }
                    else if (tipoNormalizado.Equals("Tanda2", StringComparison.OrdinalIgnoreCase))
                    {
                        tipoAcreditacionSufijo = "_Tanda2";
                    }
                    else if (tipoNormalizado.Equals("DiaADia", StringComparison.OrdinalIgnoreCase) ||
                             tipoNormalizado.Equals("DXD", StringComparison.OrdinalIgnoreCase))
                    {
                        tipoAcreditacionSufijo = "_DiaADia";
                    }
                }
                
                string fileName = $"{timestamp}-{divCode}{totalGrupo}-" +
                                    $"AcreditacionBuzonesTecnisegur{modo}{suctecni}{tipoAcreditacionSufijo}.txt";

                string rutaDestino = Path.Combine(folderPath, fileName);

                // 4) Grabo con tu método
                crearYEscribirArchivo(txt, rutaDestino);
            }
        }

        private void crearYEscribirArchivo(StringBuilder txt, string route)
        {
            // ✅ GUARDIA: Validar que la escritura esté permitida
            FileSystemGuard.EnsureWriteAllowed(route, "ScotiaFileGenerator | crearYEscribirArchivo");
            
            var directory = Path.GetDirectoryName(route);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            // Aplicar prefijo TEST_ al nombre de archivo si está en modo TEST
            string fileName = Path.GetFileName(route);
            fileName = FileSystemGuard.GetFileNameWithTestPrefix(fileName);
            route = Path.Combine(directory, fileName);
            
            string contenido = txt.ToString();
            File.WriteAllText(route, contenido);
            
            // ✅ Logging: Registrar resumen del archivo generado
            int totalLineas = contenido.Split(new[] { Environment.NewLine, "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
            ServicioLog.instancia.WriteInfo(
                $"Archivo generado exitosamente | Ruta: {route} | Total líneas escritas: {totalLineas}",
                "ScotiaFileGenerator | crearYEscribirArchivo");
        }


    }
}
