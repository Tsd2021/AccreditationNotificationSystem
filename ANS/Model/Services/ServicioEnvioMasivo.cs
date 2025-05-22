using Microsoft.Data.SqlClient;
using System.Data;
using ClosedXML.Excel;
using System.IO;
using System.Reflection;


namespace ANS.Model.Services
{
    public class ServicioEnvioMasivo
    {

        public static ServicioEnvioMasivo instancia;
        public ServicioEmail _emailService { get; set; } = ServicioEmail.getInstancia();
        public static ServicioEnvioMasivo getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioEnvioMasivo();
            }

            return instancia;
        }
        public async Task procesarEnvioMasivo(int numEnvioMasivo)
        {
            List<BuzonDTO> buzones = await getBuzonesByNumeroEnvioMasivo(numEnvioMasivo);

            await hidratarDTOconSusAcreditaciones(buzones, numEnvioMasivo);

            await obtenerUsuarioYFechaDelDeposito(buzones);

            await obtenerFechaUltimaConexionDelBuzon(buzones);

            var buzonesConAcreditaciones = buzones
                .Where(b => b.Acreditaciones != null && b.Acreditaciones.Count > 0)
                .ToList();

            if (buzonesConAcreditaciones.Count == 0) return;

            ObtenerMailsPorBuzon(buzonesConAcreditaciones);

            var semaphore = new SemaphoreSlim(initialCount: 20, maxCount: 20);
            var smtp = await ServicioEmail.instancia.getNewSmptClient();
            var sendLock = new SemaphoreSlim(1, 1);

            var tasks = buzonesConAcreditaciones.Select(async b =>
            {
                // preparar excelStream, subject, body, fileName, destino…
                b.MontoTotal = b.Acreditaciones.Sum(a => a.Monto);
                var excelStream = ArmarExcel(b, out var subject, out var body, out var fileName);
                await semaphore.WaitAsync();
                try
                {
                    // sólo UNO a la vez entra aquí:
                    await sendLock.WaitAsync();
                    try
                    {

                        await ServicioEmail.instancia
                            .EnviarExcelPorMailMasivoConMailKit(
                               excelStream, fileName, subject, body, b._Emails, smtp);

                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine($"Error al enviar el correo: {ex.Message}");
                    }

                    finally
                    {

                        sendLock.Release();

                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks);
            await smtp.DisconnectAsync(true);
        }

        private void ObtenerMailsPorBuzon(List<BuzonDTO> buzonesConAcreditaciones)
        {
            foreach (var b in buzonesConAcreditaciones)
            {
                foreach (var e in ServicioCC.getInstancia().getBuzones())
                {
                    if (b.NC == e.NC)
                    {
                        b._Emails = e._listaEmails;
                    }
                }
            }
        }

        public async Task hidratarDTOconSusAcreditaciones(List<BuzonDTO> deps, int numEnvioMasivo)
        {
            if (deps == null || deps.Count == 0) return;

            // 1) Mapear buzones
            var mapaBuzones = deps.ToDictionary(b => b.NC);

            // 2) Separar NC según EsHenderson
            var hendersons = deps.Where(b => b.EsHenderson).Select(b => b.NC).Distinct().ToList();
            var normals = deps.Where(b => !b.EsHenderson).Select(b => b.NC).Distinct().ToList();

            // 3) Construir cada TVP por separado
            DataTable BuildTvp(List<string> list)
            {
                var tvp = new DataTable();
                tvp.Columns.Add("NC", typeof(string));
                foreach (var nc in list) tvp.Rows.Add(nc);
                return tvp;
            }
            var tvpH = BuildTvp(hendersons);
            var tvpN = BuildTvp(normals);

            // 4) Calcular rangos horarios para Henderson
            DateTime today = DateTime.Today;
            DateTime startH, endH;
            if (numEnvioMasivo == 1)
            {
                // 1ª tanda: de “ayer 14:30” a “hoy 07:00”, salvo lunes
                switch (today.DayOfWeek)
                {
                    case DayOfWeek.Monday:
                        // Si hoy es lunes, restamos 3 días → viernes 14:30
                        startH = today.AddDays(-3).AddHours(14).AddMinutes(30);
                        break;
                    default:
                        // Sábado o domingo (si alguna vez aplicara): tomamos viernes
                        startH = today.AddDays(-1).AddHours(14).AddMinutes(30);
                        break;
                }

                // Siempre a las 07:00 del día “today”
                endH = today.AddHours(7);
            }
            else // numEnvioMasivo == 2
            {
                startH = today.AddHours(7);
                endH = today.AddHours(14).AddMinutes(30);
            }

            using (var conn = new SqlConnection(ConfiguracionGlobal.Conexion22))
            {
                await conn.OpenAsync();

                // A) Sólo Henderson
                if (hendersons.Any())
                {
                    const string sqlH = @"
                                    SELECT * 
                                    FROM acreditaciondepositodiegotest
                                    WHERE fecha > @fechaInicio AND fecha <= @fechaFin
                                    AND idbuzon IN (SELECT NC FROM @ListaH)";

                    using var cmdH = new SqlCommand(sqlH, conn);
                    // Parámetro TVP para Henderson
                    var pH = cmdH.Parameters.Add("@ListaH", SqlDbType.Structured);
                    pH.Value = tvpH;
                    pH.TypeName = "dbo.ListaNC";
                    // Parámetros de fecha
                    cmdH.Parameters.AddWithValue("@fechaInicio", startH);
                    cmdH.Parameters.AddWithValue("@fechaFin", endH);

                    using var readerH = await cmdH.ExecuteReaderAsync();
                    await MapearAcreditaciones(readerH, mapaBuzones);
                }

                // B) Sólo normales
                if (normals.Any())
                {
                    const string sqlN = @"
                SELECT * 
                FROM acreditaciondepositodiegotest
                WHERE CONVERT(date, fecha) = CONVERT(date, GETDATE())
                  AND idbuzon IN (SELECT NC FROM @ListaN)";

                    using var cmdN = new SqlCommand(sqlN, conn);
                    // Parámetro TVP para normales
                    var pN = cmdN.Parameters.Add("@ListaN", SqlDbType.Structured);
                    pN.Value = tvpN;
                    pN.TypeName = "dbo.ListaNC";

                    using var readerN = await cmdN.ExecuteReaderAsync();
                    await MapearAcreditaciones(readerN, mapaBuzones);
                }
            }
        }

        // Método auxiliar que mapea cada fila del reader al BuzonDTO correspondiente
        private async Task MapearAcreditaciones(SqlDataReader reader, Dictionary<string, BuzonDTO> mapa)
        {
            int ncOrd = reader.GetOrdinal("IDBUZON");
            int opOrd = reader.GetOrdinal("IDOPERACION");
            int cuentaOrd = reader.GetOrdinal("IDCUENTA");
            int monOrd = reader.GetOrdinal("MONEDA");
            int montoOrd = reader.GetOrdinal("MONTO");

            while (await reader.ReadAsync())
            {
                var acc = new AcreditacionDTO
                {
                    NC = reader.GetString(ncOrd),
                    IdOperacion = reader.GetInt64(opOrd),
                    IdCuenta = reader.GetInt32(cuentaOrd),
                    Divisa = reader.GetInt32(monOrd),
                    Monto = reader.GetDouble(montoOrd)
                };
                acc.setMoneda();

                if (mapa.TryGetValue(acc.NC, out var buzon))
                    buzon.Acreditaciones.Add(acc);
            }
        }


        private async Task obtenerUsuarioYFechaDelDeposito(List<BuzonDTO> buzones)
        {
            // 1. Early exit
            if (buzones == null || buzones.Count == 0) return;

            // 2. Recolectar pares únicos (NC, IdOperacion)
            var keys = buzones
              .SelectMany(b => b.Acreditaciones)
              .Select(a => (a.NC, a.IdOperacion))
              .Distinct()
              .ToList();

            // 3. Llenar un DataTable para el TVP
            var tvp = new DataTable();
            tvp.Columns.Add("NC", typeof(string));
            tvp.Columns.Add("IdOperacion", typeof(long));  // o del tipo correcto

            foreach (var (nc, idOp) in keys)
                tvp.Rows.Add(nc, idOp);

            // 4. Ejecutar un solo SELECT
            var sql = @"
                        SELECT 
                          d.codigo   AS NC,
                          d.idoperacion AS IdOperacion,
                          d.usuario,
                          d.fechadep  AS FechaDep,
                          d.empresa
                        FROM depositos d
                        INNER JOIN @ListaKeys k
                          ON d.codigo      = k.NC
                         AND d.idoperacion = k.IdOperacion;";

            using var conn = new SqlConnection(ConfiguracionGlobal.ConexionWebBuzones);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            var p = cmd.Parameters.AddWithValue("@ListaKeys", tvp);
            p.SqlDbType = SqlDbType.Structured;
            p.TypeName = "dbo.ListaDepositosKey";

            // 5. Leer TODO en un diccionario
            var dict = new Dictionary<(string NC, long IdOp), (string Usuario, DateTime FechaDep, string Empresa)>();
            using var reader = await cmd.ExecuteReaderAsync();
            int ordNc = reader.GetOrdinal("NC");
            int ordOp = reader.GetOrdinal("IdOperacion");
            int ordUser = reader.GetOrdinal("usuario");
            int ordFecha = reader.GetOrdinal("FechaDep");
            int ordEmp = reader.GetOrdinal("empresa");

            while (await reader.ReadAsync())
            {
                var nc = reader.GetString(ordNc);
                long idOp = reader.GetInt32(ordOp);
                var user = reader.GetString(ordUser);
                var fechaDep = reader.GetDateTime(ordFecha);
                var emp = reader.GetString(ordEmp);

                dict[(nc, idOp)] = (user, fechaDep, emp);
            }

            // 6. Asignar a cada AcreditacionDTO
            bool empresaDeLaAcreditacionAsignadaAlBuzon = false;
            foreach (var b in buzones)
            {
                foreach (var a in b.Acreditaciones)
                {
                    if (dict.TryGetValue((a.NC, a.IdOperacion), out var info))
                    {
                        a.Usuario = info.Usuario;
                        a.FechaDep = info.FechaDep;
                        a.Empresa = info.Empresa;
                        if (empresaDeLaAcreditacionAsignadaAlBuzon == false)
                        {
                            empresaDeLaAcreditacionAsignadaAlBuzon = true;
                            b.Empresa = a.Empresa;
                        }
                    }
                    else
                    {
                        // Si no existe, dejas defaults o asignas nulo, según convenga
                        a.Usuario = null;
                        a.Empresa = null;
                    }
                }
                empresaDeLaAcreditacionAsignadaAlBuzon = false;
            }
        }
        private async Task obtenerFechaUltimaConexionDelBuzon(List<BuzonDTO> buzones)
        {
            if (buzones == null || buzones.Count == 0) return;

            var keys = buzones.Select(b => b.NC).Distinct().ToList();

            var tvp = new DataTable();

            tvp.Columns.Add("NC", typeof(string));

            foreach (var nc in keys)
                tvp.Rows.Add(nc);

            var query = @" Select k.NC,Niveles.FechaUltConex from Niveles inner join @ListaNC k 
                           ON niveles.CodigoBuzon = k.NC";

            using var conn = new SqlConnection(ConfiguracionGlobal.ConexionWebBuzones);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(query, conn);

            var p = cmd.Parameters.AddWithValue("@ListaNC", tvp);

            p.SqlDbType = SqlDbType.Structured;

            p.TypeName = "dbo.ListaNC";

            var dict = new Dictionary<string, DateTime>();
            using var reader = await cmd.ExecuteReaderAsync();
            int ordNc = reader.GetOrdinal("NC");
            int ordFecha = reader.GetOrdinal("FechaUltConex");
            while (await reader.ReadAsync())
            {
                var nc = reader.GetString(ordNc);
                var fecha = reader.GetDateTime(ordFecha);
                dict[nc] = fecha;
            }

            foreach (var b in buzones)
            {
                if (dict.TryGetValue(b.NC, out var f))
                    b.UltimaFechaConexion = f;
                else
                    b.UltimaFechaConexion = DateTime.MinValue;
            }
        }

        private async Task<List<BuzonDTO>> getBuzonesByNumeroEnvioMasivo(int numEnvioMasivo)
        {


            var desdeTime = new TimeSpan(0, 0, 0);

            var hastaTime = new TimeSpan(0, 0, 0);

            string query = "";

            List<BuzonDTO> retorno = new List<BuzonDTO>();

            switch (numEnvioMasivo)
            {
                //Son son los <= 7
                case 1:
                    desdeTime = new TimeSpan(0, 0, 0);
                    hastaTime = new TimeSpan(7, 0, 0); ;
                    break;


                //Son los cierre > 7 pero < a 15:30
                case 2:
                    desdeTime = new TimeSpan(7, 0, 0);
                    hastaTime = new TimeSpan(15, 30, 0);
                    break;

                //Son los CIERRE > 15:30
                case 3:
                    // rangos de 15:30 a 19:00
                    desdeTime = new TimeSpan(15, 30, 0);
                    hastaTime = new TimeSpan(19, 0, 0);

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(numEnvioMasivo));
            }

            query = @"SELECT NC, NN, SUCURSAL, CIERRE,IDCLIENTE 
                       FROM dbo.CC 
                       WHERE ESTADO = 'alta' 
                       AND CAST(CIERRE AS time) > @desdeTime 
                       AND CAST(CIERRE AS time) <= @hastaTime";

            using (SqlConnection conn = new SqlConnection(ConfiguracionGlobal.Conexion22))
            {

                await conn.OpenAsync();

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@desdeTime", desdeTime);

                cmd.Parameters.AddWithValue("@hastaTime", hastaTime);

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {

                    int ncOrdinal = reader.GetOrdinal("NC");

                    int nnOrdinal = reader.GetOrdinal("NN");

                    int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");

                    int cierreOrdinal = reader.GetOrdinal("CIERRE");

                    int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");

                    while (await reader.ReadAsync())
                    {
                        BuzonDTO dto = new BuzonDTO();
                        dto.NC = reader.GetString(ncOrdinal);
                        dto.NN = reader.GetString(nnOrdinal);
                        dto.Sucursal = reader.GetString(sucursalOrdinal);
                        dto.Cierre = reader.GetDateTime(cierreOrdinal);
                        dto.Email = "dchiquiar@tecnisegur.com.uy";
                        dto.IdCliente = reader.GetInt32(idClienteOrdinal);
                        dto.EsHenderson = dto.esHenderson();
                        dto.NumeroEnvioMasivo = numEnvioMasivo;
                        retorno.Add(dto);
                    }
                }
            }
            return retorno;
        }

        private Stream ArmarExcel(BuzonDTO buzonDTO, out string subject, out string body, out string fileName)
        {
            var logoPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Images", "logoTecniExcel.png"
            );

            subject = body = fileName = string.Empty;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Acreditaciones");

            // 1) Logo y Título/Subtítulo
            if (File.Exists(logoPath))
                ws.AddPicture(logoPath).MoveTo(ws.Cell("A1")).WithSize(80, 80);

            ws.Range("C1", "H1").Merge().Value =
                $"TOTALES Y DEPÓSITOS DE BUZONERA {buzonDTO.NN.ToUpper()}";

            // 2) Calcular fechaInicio y fechaCierre
            DateTime fechaInicio;
            TimeSpan horaCierre = buzonDTO.Cierre.TimeOfDay;
            DateTime fechaCierre = new(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                                        horaCierre.Hours, horaCierre.Minutes, 0);

            DateTime hoy = DateTime.Today;
            if (!buzonDTO.EsHenderson)
            {
                int diasARestar = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;
                var baseDate = hoy.AddDays(-diasARestar);
                var t = buzonDTO.Cierre.TimeOfDay;
                fechaInicio = new(baseDate.Year, baseDate.Month, baseDate.Day, t.Hours, t.Minutes, 0);
            }
            else
            {
                DateTime baseDate;
                TimeSpan t;
                switch (buzonDTO.NumeroEnvioMasivo)
                {
                    case 1:
                        baseDate = hoy.DayOfWeek == DayOfWeek.Monday ? hoy.AddDays(-3) : hoy.AddDays(-1);
                        t = new(14, 30, 0);
                        break;
                    case 2:
                        baseDate = hoy;
                        t = new(7, 0, 0);
                        break;
                    default:
                        baseDate = buzonDTO.Cierre.Date;
                        t = buzonDTO.Cierre.TimeOfDay;
                        break;
                }
                fechaInicio = new(baseDate.Year, baseDate.Month, baseDate.Day, t.Hours, t.Minutes, 0);
            }

            string inicioStr = fechaInicio.ToString("dd/MM/yyyy HH:mm");
            string cierreStr = fechaCierre.ToString("dd/MM/yyyy HH:mm");
            ws.Range("C2", "H2").Merge().Value = $"DEL {inicioStr} AL {cierreStr}";

            // 3) Totales
            ws.Cell(5, 1).Value = "TOTALES:";
            ws.Cell(5, 1).Style.Font.SetBold();

            var headerColor = XLColor.FromHtml("#D9B382");
            var colsTot = new[] { "EMPRESA", "TOTAL PESOS", "TOTAL DÓLARES", "TOTAL ARG", "TOTAL REALES", "TOTAL EUROS" };
            int rowHeaderTot = 6;
            for (int i = 0; i < colsTot.Length; i++)
            {
                var c = ws.Cell(rowHeaderTot, i + 1);
                c.Value = colsTot[i];
                c.Style.Fill.SetBackgroundColor(headerColor).Font.SetBold();
                c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                c.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Cálculo de valores
            double totPesos = buzonDTO.Acreditaciones.Where(a => a.Divisa == 1).Sum(a => a.Monto);
            double totDolares = buzonDTO.Acreditaciones.Where(a => a.Divisa == 2).Sum(a => a.Monto);
            double totArg = buzonDTO.Acreditaciones.Where(a => a.Divisa == 3).Sum(a => a.Monto);
            double totReales = buzonDTO.Acreditaciones.Where(a => a.Divisa == 4).Sum(a => a.Monto);
            double totEuros = buzonDTO.Acreditaciones.Where(a => a.Divisa == 5).Sum(a => a.Monto);

            // Fila de la empresa
            int rowEmpresa = rowHeaderTot + 1;
            ws.Cell(rowEmpresa, 1).Value = buzonDTO.Empresa ?? string.Empty;
            ws.Cell(rowEmpresa, 2).Value = totPesos;
            ws.Cell(rowEmpresa, 3).Value = totDolares;
            ws.Cell(rowEmpresa, 4).Value = totArg;
            ws.Cell(rowEmpresa, 5).Value = totReales;
            ws.Cell(rowEmpresa, 6).Value = totEuros;
            // Formateo
            for (int col = 2; col <= 6; col++)
            {
                var cell = ws.Cell(rowEmpresa, col);
                cell.Style.NumberFormat.SetFormat("#,##0.00");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            ws.Cell(rowEmpresa, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Fila TOTAL
            int rowTotal = rowHeaderTot + 2;
            ws.Cell(rowTotal, 1).Value = "TOTAL";
            ws.Cell(rowTotal, 2).Value = totPesos;
            ws.Cell(rowTotal, 3).Value = totDolares;
            ws.Cell(rowTotal, 4).Value = totArg;
            ws.Cell(rowTotal, 5).Value = totReales;
            ws.Cell(rowTotal, 6).Value = totEuros;
            for (int col = 2; col <= 6; col++)
            {
                var cell = ws.Cell(rowTotal, col);
                cell.Style.NumberFormat.SetFormat("#,##0.00");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            ws.Cell(rowTotal, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // 4) Depósitos
            ws.Cell(rowTotal + 2, 1).Value = "DEPOSITOS:";
            ws.Cell(rowTotal + 2, 1).Style.Font.SetBold();

            var colsDet = new[] { "OPERACIÓN", "FECHA", "MONEDA", "TOTAL", "USUARIO", "EMPRESA", "SUCURSAL" };
            int rowHeaderDet = rowTotal + 3;
            for (int i = 0; i < colsDet.Length; i++)
            {
                var c = ws.Cell(rowHeaderDet, i + 1);
                c.Value = colsDet[i];
                c.Style.Fill.SetBackgroundColor(headerColor).Font.SetBold();
                c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                c.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Detalle filas
            int rowDet = rowHeaderDet + 1;
            foreach (var a in buzonDTO.Acreditaciones)
            {
                ws.Cell(rowDet, 1).Value = a.IdOperacion;
                ws.Cell(rowDet, 2).Value = a.FechaDep;
                ws.Cell(rowDet, 2).Style.DateFormat.SetFormat("yyyy-MM-dd HH:mm:ss");
                ws.Cell(rowDet, 3).Value = a.Moneda;
                ws.Cell(rowDet, 4).Value = a.Monto;
                ws.Cell(rowDet, 4).Style.NumberFormat.SetFormat("#,##0.00");
                ws.Cell(rowDet, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(rowDet, 5).Value = a.Usuario;
                ws.Cell(rowDet, 6).Value = a.Empresa;
                ws.Cell(rowDet, 7).Value = buzonDTO.Sucursal;

                ws.Range(rowDet, 1, rowDet, colsDet.Length)
                  .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowDet++;
            }

            // Ajustar anchos
            ws.Columns().AdjustToContents();

            // Guardar en Stream y asignar out params
            var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;

            fileName = $"EnvioMasivo_{buzonDTO.NC}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            subject = $"Acreditaciones Buzón Inteligente [{buzonDTO.NN}] - {fechaInicio:dd/MM/yyyy}";
            body = $@"<p>Acreditaciones del <strong>Buzón Inteligente {buzonDTO.NN}</strong><br/>del {inicioStr}<br/>al {cierreStr}</p>
<p><strong>Por favor, tener en cuenta fecha y hora de última conexión del buzón:</strong><br/>{buzonDTO.UltimaFechaConexion:dd/MM/yyyy HH:mm}</p>";

            return ms;
        }




        /*
         
         select top 1 idoperacion,fecha
from AcreditacionDepositoDiegoTest
where idbuzon like '%delas%'
and idoperacion < (select top 1 idoperacion from AcreditacionDepositoDiegoTest
where idbuzon like '%DELAS%'
and convert(date,fechA) = convert(date,getdate())
group by idoperacion,fecha
order by idoperacion asc)
order by idoperacion desc
         */

        public class BuzonDTO
        {

            public string NC { get; set; }
            public string NN { get; set; }
            public string Empresa { get; set; }
            public DateTime FechaInicio { get; set; }
            public DateTime Cierre { get; set; }
            public double MontoTotal { get; set; }
            public string Moneda { get; set; }
            public string Email { get; set; }
            public int Divisa { get; set; }
            public int IdOperacion { get; set; }
            public string Sucursal { get; set; }
            public long IdOperacionFinal { get; set; }
            public long IdOperacionInicio { get; set; }
            public List<AcreditacionDTO> Acreditaciones { get; set; } = new List<AcreditacionDTO>();
            public DateTime UltimaFechaConexion { get; set; }
            public int IdCliente { get; set; }
            public bool EsHenderson { get; set; }
            public int NumeroEnvioMasivo { get; set; }
            public List<Email> _Emails { get; set; } = new List<Email>();
            public BuzonDTO()
            {

            }
            public bool esHenderson()
            {

                if (IdCliente == 164)
                {
                    return true;
                }
                Cliente c = ServicioCliente.getInstancia().getById(164);

                foreach (Cliente unCli in c.ClientesRelacionados)
                {
                    if (IdCliente == unCli.IdCliente)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public class AcreditacionDTO
        {

            public long IdOperacion { get; set; }
            public double Monto { get; set; }
            public string Moneda { get; set; }
            public int Divisa { get; set; }
            public int IdCuenta { get; set; }
            public string NC { get; set; }
            public string Usuario { get; set; }
            public DateTime FechaDep { get; set; }
            public string Empresa { get; set; }
            public void setMoneda()
            {
                if (Divisa == 1)
                {
                    Moneda = "UYU";
                }
                if (Divisa == 2)
                {
                    Moneda = "USD";
                }
            }
        }
    }
}
