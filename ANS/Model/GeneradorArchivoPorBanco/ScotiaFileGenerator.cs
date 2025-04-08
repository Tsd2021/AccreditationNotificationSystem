using ANS.Model.Interfaces;
using System.IO;
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
                // La ruta padre
                string baseDirectory = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\TXT\SCOTIABANK";
                // Combinar para obtener la ruta completa
                string directory = Path.Combine(baseDirectory, subFolder);
                string rutaDestino = Path.Combine(directory, fileName);

                // Llama al método que se encarga de crear el directorio (si no existe) y escribir el archivo
                crearYEscribirArchivo(_txt, rutaDestino);
            }
        }

        private void crearYEscribirArchivo(StringBuilder txt, string route)
        {
            // Obtener el directorio a partir de la ruta completa
            string directory = Path.GetDirectoryName(route);

            // Verificar si el directorio existe, y si no, crearlo
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Escribir el contenido en el archivo
            File.WriteAllText(route, txt.ToString());
        }

    }
}
