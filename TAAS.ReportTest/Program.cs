using SharedDTOs;
using TAAS.Reports;
using NPOI.HSSF.UserModel;

Console.WriteLine("==========================================");
Console.WriteLine(" TAAS - PRUEBA DEL MÉTODO REAL");
Console.WriteLine(" ArmarExcelConReportViewer");
Console.WriteLine("==========================================");

string carpetaResultados = Path.Combine(
    AppContext.BaseDirectory,
    "ResultadosMetodoReal"
);

Directory.CreateDirectory(carpetaResultados);

Console.WriteLine();
Console.WriteLine("=== PRUEBA INDIVIDUAL ===");

bool individualCorrecta = false;

try
{
    ProbarMetodoReal(0);

    individualCorrecta = true;

    Console.WriteLine("PRUEBA INDIVIDUAL CORRECTA");
}
catch (Exception ex)
{
    GuardarError(0, ex);
}

if (individualCorrecta)
{
    Console.WriteLine();
    Console.WriteLine("=== PRUEBA CONCURRENTE ===");

    const int cantidad = 8;

    Task<bool>[] tareas = Enumerable.Range(1, cantidad)
        .Select(numero => Task.Run(() =>
        {
            try
            {
                ProbarMetodoReal(numero);
                return true;
            }
            catch (Exception ex)
            {
                GuardarError(numero, ex);
                return false;
            }
        }))
        .ToArray();

    bool[] resultados = await Task.WhenAll(tareas);

    Console.WriteLine();
    Console.WriteLine(
        $"RESULTADO CONCURRENTE: {resultados.Count(x => x)}/{cantidad} CORRECTOS"
    );
}

Console.WriteLine();
Console.WriteLine("Prueba finalizada.");
Console.WriteLine("Presioná ENTER para salir...");
Console.ReadLine();


void ProbarMetodoReal(int numero)
{
    // Creamos un buzón ficticio.
    var buzon = new BuzonDTO2
    {
        NC = $"BUZON_PRUEBA_{numero}",
        NN = $"BUZON PRUEBA {numero}",
        NombreWS = "NO_DEFINIDO",

        Cierre = DateTime.Today.AddHours(14).AddMinutes(30),

        EsHenderson = false,
        NumeroEnvioMasivo = 2,

        UltimaFechaConexion = DateTime.Now,

        Acreditaciones = new List<AcreditacionDTO2>
        {
            new AcreditacionDTO2
            {
                IdOperacion = 100000 + numero,
                Monto = 1500.50,
                Divisa = 1,
                NC = $"BUZON_PRUEBA_{numero}",
                Usuario = "USUARIO PRUEBA",
                FechaDep = DateTime.Now.AddHours(-2),
                Empresa = "EMPRESA A",
                FechaAcreditacion = DateTime.Now
            },

            new AcreditacionDTO2
            {
                IdOperacion = 200000 + numero,
                Monto = 250.75,
                Divisa = 2,
                NC = $"BUZON_PRUEBA_{numero}",
                Usuario = "USUARIO PRUEBA",
                FechaDep = DateTime.Now.AddHours(-1),
                Empresa = "EMPRESA A",
                FechaAcreditacion = DateTime.Now
            },

            new AcreditacionDTO2
            {
                IdOperacion = 300000 + numero,
                Monto = 3000.00,
                Divisa = 1,
                NC = $"BUZON_PRUEBA_{numero}",
                Usuario = "USUARIO PRUEBA",
                FechaDep = DateTime.Now.AddMinutes(-30),
                Empresa = "EMPRESA B",
                FechaAcreditacion = DateTime.Now
            }
        }
    };

    // Instancia independiente por ejecución.
    var servicio = new ReportService();

    using Stream excel = servicio.ArmarExcelConReportViewer(
        buzon,
        out string subject,
        out string body,
        out string fileName
    );

    // Guardamos el resultado con nombre único.
    string archivo = Path.Combine(
        carpetaResultados,
        $"MetodoReal_{numero:D2}.xls"
    );

    using (var archivoSalida = File.Create(archivo))
    {
        excel.CopyTo(archivoSalida);
    }

    // Comprobamos que el archivo exista y tenga contenido.
    FileInfo info = new FileInfo(archivo);

    if (info.Length == 0)
    {
        throw new Exception(
            $"El Excel #{numero} se generó vacío."
        );
    }

    // Validamos que NPOI pueda abrir el archivo generado.
    using (var archivoEntrada = File.OpenRead(archivo))
    using (var libro = new HSSFWorkbook(archivoEntrada))
    {
        if (libro.NumberOfSheets == 0)
        {
            throw new Exception(
                $"El Excel #{numero} no contiene hojas."
            );
        }

        if (libro.GetSheetAt(0).SheetName != "TotalesyDepositosCC")
        {
            throw new Exception(
                $"El Excel #{numero} no tiene el nombre de hoja esperado."
            );
        }
    }

    Console.WriteLine(
        $"OK #{numero:D2} | {info.Length:N0} bytes | {fileName}"
    );

    if (numero == 0)
    {
        Console.WriteLine();
        Console.WriteLine($"Asunto: {subject}");
        Console.WriteLine($"Cuerpo: {body}");
        Console.WriteLine();
    }
}

void GuardarError(int numero, Exception ex)
{
    string archivo = Path.Combine(
        carpetaResultados,
        $"Error_{numero:D2}.txt"
    );

    File.WriteAllText(archivo, ex.ToString());

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR #{numero:D2}");
    Console.ResetColor();

    Console.WriteLine(ex.ToString());

    Console.WriteLine($"Detalle guardado en: {archivo}");
}