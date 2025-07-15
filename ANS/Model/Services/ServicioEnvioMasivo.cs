using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.Reporting.NETCore;
using System.Data;
using System.IO;
using System.Reflection;
using TAAS.Reports;
using SharedDTOs;

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
            try
            {
          
            List<BuzonDTO> buzones = await getBuzonesByNumeroEnvioMasivo(numEnvioMasivo);

            await hidratarDTOconSusAcreditaciones(buzones, numEnvioMasivo);

            await obtenerUsuarioYFechaDelDeposito(buzones);

            await obtenerFechaUltimaConexionDelBuzon(buzones);

            var buzonesConAcreditaciones = buzones
                .Where(b => b.Acreditaciones != null && b.Acreditaciones.Count > 0)
                .ToList();

            if (buzonesConAcreditaciones.Count == 0) return;

            var reportService = new TAAS.Reports.ReportService();

            ObtenerMailsPorBuzon(buzonesConAcreditaciones);

            var semaphore = new SemaphoreSlim(initialCount: 20, maxCount: 20);
            var smtp = await ServicioEmail.instancia.getNewSmptClient();
            var sendLock = new SemaphoreSlim(1, 1);

            var tasks = buzonesConAcreditaciones.Select(async b =>
            {
                // preparar excelStream, subject, body, fileName, destino…
                b.MontoTotal = b.Acreditaciones.Sum(a => a.Monto);

                var b2 = new BuzonDTO2
                {
                    NC = b.NC,
                    NN = b.NN,
                    Empresa = b.Empresa,
                    FechaInicio = b.FechaInicio,
                    Cierre = b.Cierre,
                    MontoTotal = b.MontoTotal,
                    Moneda = b.Moneda,
                    Divisa = b.Divisa,
                    IdOperacion = b.IdOperacion,
                    Sucursal = b.Sucursal,
                    IdOperacionFinal = b.IdOperacionFinal,
                    IdOperacionInicio = b.IdOperacionInicio,
                    NumeroEnvioMasivo = b.NumeroEnvioMasivo,
                    UltimaFechaConexion = b.UltimaFechaConexion,
                    EsHenderson = b.EsHenderson,
                    NombreWS = b.NombreWS,
                    // mapea Acreditaciones:
                    Acreditaciones = b.Acreditaciones.Select(a => new AcreditacionDTO2
                    {
                        NC = a.NC,
                        IdOperacion = a.IdOperacion,
                        Divisa = a.Divisa,
                        Monto = a.Monto,
                        Usuario = a.Usuario,
                        FechaDep = a.FechaDep,
                        Empresa = a.Empresa
                    }).ToList()
                };

                //var excelStream = ArmarExcelConReportViewer(b, out var subject, out var body, out var fileName);
                var excelStream = reportService.ArmarExcelConReportViewer(b2, out var subject, out var body, out var fileName);
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
            catch(Exception e)
            {
                throw e;
            }
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

                    const string sqlN = @"SELECT * 
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
            var sql =   @"
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

                //Son los cierre > 7 pero < a 14:30
                case 2:
                    desdeTime = new TimeSpan(7, 0, 0);
                    hastaTime = new TimeSpan(14, 30, 0);
                    break;

                case 3:
                    // rangos de 14:30 a 15:30
                    desdeTime = new TimeSpan(14, 30, 0);
                    hastaTime = new TimeSpan(15, 30, 0);
                    break;

                case 4:
                    // rangos de 15:30 a 19:00
                    desdeTime = new TimeSpan(15, 30, 0);
                    hastaTime = new TimeSpan(19, 0, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(numEnvioMasivo));
            }

                query = @"SELECT c.NC, c.NN, c.SUCURSAL, c.CIERRE,c.IDCLIENTE , ws.NombreWS
                        from
                        cc as c 
                        left join 
                        cc_nombrews as ws 
                        on ws.NC = c.NC 
                        where c.estado = 'alta'
                        AND CAST(c.CIERRE AS time) > @desdeTime 
                        AND CAST(c.CIERRE AS time) <= @hastaTime";

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

                    int nombreWSOrdinal = reader.GetOrdinal("NombreWS");

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
                        if(!reader.IsDBNull(nombreWSOrdinal))
                        {
                            dto.NombreWS = reader.GetString(nombreWSOrdinal);
                        }
                        else
                        {
                            dto.NombreWS = "NO_DEFINIDO";
                        }
                        retorno.Add(dto);
                    }
                }

                if (numEnvioMasivo == 1)
                {

                    string queryParaObtenerHenderson = @"select distinct NC,NN,SUCURSAL,CIERRE,cc.IDCLIENTE from cc
                                                         inner join ClientesRelacionadosTest as cr 
                                                         on cc.IDCLIENTE = cr.IdRazonSocial 
                                                         where cr.IdCliente = 164 
                                                         and tanda = 1 and estado = 'alta' ";

                    SqlCommand cmd2 = new SqlCommand(queryParaObtenerHenderson, conn);

                    using (SqlDataReader reader2 = await cmd2.ExecuteReaderAsync())
                    {
                        int ncOrdinal = reader2.GetOrdinal("NC");

                        int nnOrdinal = reader2.GetOrdinal("NN");

                        int sucursalOrdinal = reader2.GetOrdinal("SUCURSAL");

                        int cierreOrdinal = reader2.GetOrdinal("CIERRE");

                        int idClienteOrdinal = reader2.GetOrdinal("IDCLIENTE");

                        while (await reader2.ReadAsync())
                        {

                            BuzonDTO dto = new BuzonDTO();
                            dto.NC = reader2.GetString(ncOrdinal);
                            dto.NN = reader2.GetString(nnOrdinal);
                            dto.Sucursal = reader2.GetString(sucursalOrdinal);
                            dto.Cierre = reader2.GetDateTime(cierreOrdinal);
                            dto.Email = "dchiquiar@tecnisegur.com.uy";
                            dto.IdCliente = reader2.GetInt32(idClienteOrdinal);
                            dto.EsHenderson = true;
                            dto.NumeroEnvioMasivo = numEnvioMasivo;
                            retorno.Add(dto);
                        }
                    }

                }
            }
            return retorno;
        }

        private Stream ArmarExcel(BuzonDTO buzonDTO, out string subject, out string body, out string fileName)
        {
            // Inicializar out-params
            subject = body = fileName = string.Empty;

            // Directorio base y logo
            string baseDir = AppContext.BaseDirectory;
            var logoPath = Path.Combine(baseDir, "Images", "logoTecniExcel.png");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Acreditaciones");

            // 1) Logo y Título/Subtítulo
            if (File.Exists(logoPath))
                ws.AddPicture(logoPath)
                  .MoveTo(ws.Cell("A1"))
                  .WithSize(80, 80);

            ws.Range("C1", "H1").Merge().Value =
                $"TOTALES Y DEPÓSITOS DE BUZONERA {buzonDTO.NN.ToUpper()}";

            // 2) Fechas inicio y cierre
            DateTime hoy = DateTime.Today;
            DateTime fechaInicio;
            DateTime fechaCierre;

            if (buzonDTO.EsHenderson && buzonDTO.NumeroEnvioMasivo == 1)
            {
                int diasARestar = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;
                var baseDate = hoy.AddDays(-diasARestar);
                fechaInicio = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, 14, 30, 0);
                fechaCierre = new DateTime(hoy.Year, hoy.Month, hoy.Day, 7, 0, 0);
            }
            else
            {
                var horaCierreDto = buzonDTO.Cierre.TimeOfDay;
                fechaCierre = new DateTime(hoy.Year, hoy.Month, hoy.Day, horaCierreDto.Hours, horaCierreDto.Minutes, 0);

                if (!buzonDTO.EsHenderson)
                {
                    int diasARestar = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;
                    var baseDate = hoy.AddDays(-diasARestar);
                    var t = buzonDTO.Cierre.TimeOfDay;
                    fechaInicio = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, t.Hours, t.Minutes, 0);
                }
                else
                {
                    switch (buzonDTO.NumeroEnvioMasivo)
                    {
                        case 2:
                            fechaInicio = new DateTime(hoy.Year, hoy.Month, hoy.Day, 7, 0, 0);
                            break;
                        default:
                            var cierreDto = buzonDTO.Cierre;
                            fechaInicio = new DateTime(cierreDto.Year, cierreDto.Month, cierreDto.Day, cierreDto.Hour, cierreDto.Minute, 0);
                            break;
                    }
                }
            }

            string inicioStr = fechaInicio.ToString("dd/MM/yyyy HH:mm");
            string cierreStr = fechaCierre.ToString("dd/MM/yyyy HH:mm");
            ws.Range("C2", "H2").Merge().Value =
                $"DEL {inicioStr} AL {cierreStr}";

            // 3) Totales agrupados por empresa depositante
            ws.Cell(5, 1).Value = "TOTALES:";
            ws.Cell(5, 1).Style.Font.SetBold();

            var headerColor = XLColor.FromHtml("#D9B382");
            var colsTot = new[] { "EMPRESA", "TOTAL PESOS", "TOTAL DÓLARES", "TOTAL ARG", "TOTAL REALES", "TOTAL EUROS" };
            int rowHeaderTot = 6;
            for (int i = 0; i < colsTot.Length; i++)
            {
                var c = ws.Cell(rowHeaderTot, i + 1);
                c.Value = colsTot[i];
                c.Style.Fill.SetBackgroundColor(headerColor)
                       .Font.SetBold();
                c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                c.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Agrupar acreditaciones por empresa
            var totalesPorEmpresa = buzonDTO.Acreditaciones
                .GroupBy(a => a.Empresa)
                .Select(g => new {
                    Empresa = g.Key,
                    Pesos = g.Where(a => a.Divisa == 1).Sum(a => a.Monto),
                    Dolares = g.Where(a => a.Divisa == 2).Sum(a => a.Monto),
                    Arg = g.Where(a => a.Divisa == 3).Sum(a => a.Monto),
                    Reales = g.Where(a => a.Divisa == 4).Sum(a => a.Monto),
                    Euros = g.Where(a => a.Divisa == 5).Sum(a => a.Monto)
                })
                .ToList();

            // Filas de cada empresa
            int row = rowHeaderTot + 1;
            foreach (var t in totalesPorEmpresa)
            {
                ws.Cell(row, 1).Value = t.Empresa;
                ws.Cell(row, 2).Value = t.Pesos;
                ws.Cell(row, 3).Value = t.Dolares;
                ws.Cell(row, 4).Value = t.Arg;
                ws.Cell(row, 5).Value = t.Reales;
                ws.Cell(row, 6).Value = t.Euros;

                for (int col = 2; col <= 6; col++)
                {
                    var cell = ws.Cell(row, col);
                    cell.Style.NumberFormat.SetFormat("#,##0.00");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                ws.Cell(row, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                row++;
            }

            // Fila TOTAL general
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 2).Value = totalesPorEmpresa.Sum(x => x.Pesos);
            ws.Cell(row, 3).Value = totalesPorEmpresa.Sum(x => x.Dolares);
            ws.Cell(row, 4).Value = totalesPorEmpresa.Sum(x => x.Arg);
            ws.Cell(row, 5).Value = totalesPorEmpresa.Sum(x => x.Reales);
            ws.Cell(row, 6).Value = totalesPorEmpresa.Sum(x => x.Euros);
            for (int col = 2; col <= 6; col++)
            {
                var cell = ws.Cell(row, col);
                cell.Style.NumberFormat.SetFormat("#,##0.00");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            ws.Cell(row, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // 4) Depósitos
            int rowHeaderDet = row + 2;
            ws.Cell(rowHeaderDet - 1, 1).Value = "DEPOSITOS:";
            ws.Cell(rowHeaderDet - 1, 1).Style.Font.SetBold();

            var colsDet = new[] { "OPERACIÓN", "FECHA", "MONEDA", "TOTAL", "USUARIO", "EMPRESA", "SUCURSAL" };
            for (int i = 0; i < colsDet.Length; i++)
            {
                var c = ws.Cell(rowHeaderDet, i + 1);
                c.Value = colsDet[i];
                c.Style.Fill.SetBackgroundColor(headerColor)
                       .Font.SetBold();
                c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                c.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

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

            // Ajustar anchos automáticamente
            ws.Columns().AdjustToContents();

            // Guardar en memoria y preparar out-params
            var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;

            fileName = $"EnvioMasivo_{buzonDTO.NC}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            subject = $"Acreditaciones Buzón Inteligente [{buzonDTO.NN}] - {fechaInicio:dd/MM/yyyy}";
            body = $@"
<p>Acreditaciones del <strong>Buzón Inteligente {buzonDTO.NN}</strong><br/>
del {inicioStr}<br/>al {cierreStr}</p>
<p><strong>Por favor, tener en cuenta fecha y hora de última conexión del buzón:</strong><br/>
{buzonDTO.UltimaFechaConexion:dd/MM/yyyy HH:mm}</p>";

            return ms;
        }

        private Stream ArmarExcel2(BuzonDTO buzonDTO, out string subject, out string body, out string fileName)
        {
            subject = body = fileName = string.Empty;
            string baseDir = AppContext.BaseDirectory;
            var logoPath = Path.Combine(baseDir, "Images", "logoTecniExcel.png");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Acreditaciones");

            // ────────────── Layout exacto según AcreditacionTI-POCITOS ──────────────

            // Anchos de columnas (igual que plantilla)
            ws.Column("B").Width = 15.710625;
            ws.Column("C").Width = 9.140625;
            ws.Column("D").Width = 0.710625; ws.Column("D").Hide();
            ws.Column("E").Width = 15.710625;
            ws.Column("F").Width = 13.0;
            ws.Column("G").Width = 13.0;
            ws.Column("H").Width = 13.0;
            ws.Column("I").Width = 13.0;

            // Márgenes de página
            ws.PageSetup.Margins.Left = 0.7874015748031497;
            ws.PageSetup.Margins.Right = 0.7874015748031497;
            ws.PageSetup.Margins.Top = 0.7874015748031497;
            ws.PageSetup.Margins.Bottom = 0.7874015748031497;

            // Altos de fila fijos
            ws.Row(1).Height = 21.75;
            ws.Row(2).Height = 12.00;
            ws.Row(3).Height = 24.60;
            ws.Row(4).Height = 5.10;
            ws.Row(5).Height = 6.00;
            ws.Row(7).Height = 11.85;
            ws.Row(8).Height = 17.10;
            ws.Row(9).Height = 2.10;
            ws.Row(10).Height = 25.50;
            ws.Row(13).Height = 8.25;
            ws.Row(14).Height = 17.10;

            // 1) Área logo
            ws.Range("B2:C5").Merge();
            if (File.Exists(logoPath))
                ws.AddPicture(logoPath)
                  .MoveTo(ws.Cell("B2"))
                  .WithSize(80, 80);

            // 2) Título
            ws.Range("E3:I3").Merge()
              .Value = $"TOTALES Y DEPOSITOS DE BUZONERA {buzonDTO.NN.ToUpper()}";

            // 3) Subtítulo fechas
            DateTime hoy = DateTime.Today;
            DateTime fechaInicio, fechaCierre;
            if (buzonDTO.EsHenderson && buzonDTO.NumeroEnvioMasivo == 1)
            {
                int dias = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;
                var bd = hoy.AddDays(-dias);
                fechaInicio = new DateTime(bd.Year, bd.Month, bd.Day, 14, 30, 0);
                fechaCierre = new DateTime(hoy.Year, hoy.Month, hoy.Day, 9, 30, 0);
            }
            else
            {
                var c = buzonDTO.Cierre;
                fechaCierre = new DateTime(hoy.Year, hoy.Month, hoy.Day, c.Hour, c.Minute, 0);
                if (!buzonDTO.EsHenderson)
                {
                    int dias = hoy.DayOfWeek == DayOfWeek.Monday ? 3 : 1;
                    var bd2 = hoy.AddDays(-dias);
                    var t = c.TimeOfDay;
                    fechaInicio = new DateTime(bd2.Year, bd2.Month, bd2.Day, t.Hours, t.Minutes, 0);
                }
                else if (buzonDTO.NumeroEnvioMasivo == 2)
                    fechaInicio = new DateTime(hoy.Year, hoy.Month, hoy.Day, 7, 0, 0);
                else
                    fechaInicio = new DateTime(hoy.Year, hoy.Month, hoy.Day, c.Hour, c.Minute, 0);
            }
            string inicioStr = fechaInicio.ToString("dd/MM/yyyy HH:mm");
            string cierreStr = fechaCierre.ToString("dd/MM/yyyy HH:mm");
            ws.Range("E5:I6").Merge()
              .Value = $"DEL {inicioStr} AL {cierreStr}";

            // 4) Sección TOTALES
            ws.Range("B8:C8").Merge().Value = "TOTALES:";
            ws.Range("B8:C8").Style.Font.SetBold();

            var headerColor = XLColor.FromHtml("#D9B382");
            ws.Range("B10:C10").Merge().Value = "EMPRESA";
            ws.Range("B10:C10").Style
              .Fill.SetBackgroundColor(headerColor)
              .Font.SetBold()
              .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
              .Border.OutsideBorder = XLBorderStyleValues.Thin;

            var totCols = new[] {
        new { Col="E", Title="TOTAL PESOS" },
        new { Col="F", Title="TOTAL DOLARES" },
        new { Col="G", Title="TOTAL ARG" },
        new { Col="H", Title="TOTAL REALES" },
        new { Col="I", Title="TOTAL EUROS" },
    };
            foreach (var tc in totCols)
            {
                ws.Cell($"{tc.Col}10").Value = tc.Title;
                ws.Cell($"{tc.Col}10").Style
                  .Fill.SetBackgroundColor(headerColor)
                  .Font.SetBold()
                  .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                  .Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            var totales = buzonDTO.Acreditaciones
              .GroupBy(a => a.Empresa)
              .Select(g => new {
                  Empresa = g.Key,
                  Pesos = g.Where(a => a.Divisa == 1).Sum(a => a.Monto),
                  Dolares = g.Where(a => a.Divisa == 2).Sum(a => a.Monto),
                  Arg = g.Where(a => a.Divisa == 3).Sum(a => a.Monto),
                  Reales = g.Where(a => a.Divisa == 4).Sum(a => a.Monto),
                  Euros = g.Where(a => a.Divisa == 5).Sum(a => a.Monto)
              })
              .ToList();

            int row = 11;
            foreach (var t in totales)
            {
                ws.Range($"B{row}:C{row}").Merge().Value = t.Empresa;
                ws.Range($"B{row}:C{row}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                foreach (var tc in totCols)
                {
                    double val = tc.Col switch
                    {
                        "E" => t.Pesos,
                        "F" => t.Dolares,
                        "G" => t.Arg,
                        "H" => t.Reales,
                        "I" => t.Euros,
                        _ => 0
                    };
                    ws.Cell($"{tc.Col}{row}").Value = val;
                    ws.Cell($"{tc.Col}{row}").Style
                      .NumberFormat.SetFormat("#,##0.00")
                      .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                      .Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                row++;
            }

            // Fila TOTAL general
            ws.Range($"B{row}:C{row}").Merge().Value = "TOTAL";
            ws.Range($"B{row}:C{row}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            foreach (var tc in totCols)
            {
                ws.Cell($"{tc.Col}{row}").FormulaA1 = $"=SUM({tc.Col}11:{tc.Col}{row - 1})";
                ws.Cell($"{tc.Col}{row}").Style
                  .NumberFormat.SetFormat("#,##0.00")
                  .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                  .Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // 5) Sección DEPÓSITOS
            ws.Range("B14:C14").Merge().Value = "DEPOSITOS:";
            ws.Range("B14:C14").Style.Font.SetBold();

            var detHeaders = new Dictionary<string, string> {
        { "B", "OPERACION" },
        { "C", "FECHA" },
        { "E", "MONEDA" },
        { "F", "TOTAL" },
        { "G", "USUARIO" },
        { "H", "EMPRESA" },
        { "I", "SUCURSAL" }
    };
            foreach (var kv in detHeaders)
            {
                ws.Cell($"{kv.Key}15").Value = kv.Value;
                ws.Cell($"{kv.Key}15").Style
                  .Fill.SetBackgroundColor(headerColor)
                  .Font.SetBold()
                  .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                  .Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            int rowDet = 16;
            foreach (var a in buzonDTO.Acreditaciones)
            {
                ws.Cell($"B{rowDet}").Value = a.IdOperacion;
                ws.Cell($"C{rowDet}").Value = a.FechaDep;
                ws.Cell($"C{rowDet}").Style.DateFormat.SetFormat("yyyy-MM-dd HH:mm:ss");

                ws.Cell($"E{rowDet}").Value = a.Moneda;
                ws.Cell($"F{rowDet}").Value = a.Monto;
                ws.Cell($"F{rowDet}").Style
                  .NumberFormat.SetFormat("#,##0.00")
                  .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                ws.Cell($"G{rowDet}").Value = a.Usuario;
                ws.Cell($"H{rowDet}").Value = a.Empresa;
                ws.Cell($"I{rowDet}").Value = buzonDTO.Sucursal;

                ws.Range($"B{rowDet}:I{rowDet}")
                  .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                rowDet++;
            }

            // Guardar en memoria
            var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;

            fileName = $"EnvioMasivo_{buzonDTO.NC}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            subject = $"Acreditaciones Buzón Inteligente [{buzonDTO.NN}] - {fechaInicio:dd/MM/yyyy}";
            body = $@"
<p>Acreditaciones del <strong>Buzón Inteligente {buzonDTO.NN}</strong><br/>
del {inicioStr}<br/>al {cierreStr}</p>
<p><strong>Última conexión del buzón:</strong><br/>
{buzonDTO.UltimaFechaConexion:dd/MM/yyyy HH:mm}</p>";

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
        public class TotalesImprimir
        {
            public string EMPRESA { get; set; }
            public string TOTALPESOS { get; set; }
            public string TOTALDOLARES { get; set; }
            public string TOTALARG { get; set; }
            public string TOTALEUROS { get; set; }
            public string TOTALREALES { get; set; }
        }

        public class DepositosCCImprimir
        {
            public string OPERACION { get; set; }
            public string MONEDA { get; set; }
            public string FECHA { get; set; }
            public string TOTAL { get; set; }
            public string USUARIO { get; set; }
            public string EMPRESA { get; set; }
            public string SUCURSAL { get; set; }
        }
        private Stream ArmarExcelConReportViewer(BuzonDTO buzonDTO, out string subject, out string body, out string fileName)
        {
            subject = body = fileName = string.Empty;
            string exeFolder = AppContext.BaseDirectory;
            string logPath = Path.Combine(exeFolder, "reportLogs.txt");
            string reportPath = Path.Combine(exeFolder, "Reports", "TotalesyDepositosCC.rdlc");
            string logMsg = $"[{DateTime.Now}] Buscando RDLC en: {reportPath} - Existe: {File.Exists(reportPath)}";
            Console.WriteLine(logMsg);
            File.AppendAllText(logPath, logMsg + Environment.NewLine);

            if (!File.Exists(reportPath))
            {
                string errMsg = $"[{DateTime.Now}] ERROR: No se encontró el informe en: {reportPath}";
                File.AppendAllText(logPath, errMsg + Environment.NewLine);
                throw new FileNotFoundException(errMsg);
            }

            try
            {
                // 1. Construyo los DataSources con strings
                var totales = buzonDTO.Acreditaciones
                    .GroupBy(a => a.Empresa)
                    .Select(g => new TotalesImprimir
                    {
                        EMPRESA = g.Key,
                        TOTALPESOS = ((decimal)g.Where(a => a.Divisa == 1).Sum(a => a.Monto)).ToString("N2"),
                        TOTALDOLARES = ((decimal)g.Where(a => a.Divisa == 2).Sum(a => a.Monto)).ToString("N2"),
                        TOTALARG = ((decimal)g.Where(a => a.Divisa == 3).Sum(a => a.Monto)).ToString("N2"),
                        TOTALREALES = ((decimal)g.Where(a => a.Divisa == 4).Sum(a => a.Monto)).ToString("N2"),
                        TOTALEUROS = ((decimal)g.Where(a => a.Divisa == 5).Sum(a => a.Monto)).ToString("N2"),
                    })
                    .ToList();

                // Calculo totales generales
                var totalGeneral = new TotalesImprimir
                {
                    EMPRESA = "TOTAL",
                    TOTALPESOS = totales.Sum(x => decimal.Parse(x.TOTALPESOS)).ToString("N2"),
                    TOTALDOLARES = totales.Sum(x => decimal.Parse(x.TOTALDOLARES)).ToString("N2"),
                    TOTALARG = totales.Sum(x => decimal.Parse(x.TOTALARG)).ToString("N2"),
                    TOTALREALES = totales.Sum(x => decimal.Parse(x.TOTALREALES)).ToString("N2"),
                    TOTALEUROS = totales.Sum(x => decimal.Parse(x.TOTALEUROS)).ToString("N2"),
                };
                totales.Add(totalGeneral);

                // 2. Depositos (igual que antes)...
                var depositos = buzonDTO.Acreditaciones
                    .Select(a => new DepositosCCImprimir
                    {
                        OPERACION = a.IdOperacion.ToString(),
                        MONEDA = a.Moneda,
                        FECHA = a.FechaDep.ToString("yyyy-MM-dd HH:mm:ss"),
                        TOTAL = a.Monto.ToString("N2"),
                        USUARIO = a.Usuario,
                        EMPRESA = a.Empresa,
                        SUCURSAL = buzonDTO.Sucursal
                    })
                    .ToList();

                // 3. Cargar el RDLC
                var report = new LocalReport();
                using var stream = File.OpenRead(reportPath);
                report.LoadReportDefinition(stream);

                // 4. Asigno los DataSources
                report.DataSources.Clear();
                report.DataSources.Add(new ReportDataSource("DataSet1", totales));
                report.DataSources.Add(new ReportDataSource("DataSet2", depositos));

                // 5. Parámetros
                string fechaRango = $"DEL {buzonDTO.FechaInicio:dd/MM/yyyy HH:mm} AL {buzonDTO.Cierre:dd/MM/yyyy HH:mm}";
                report.SetParameters(new ReportParameter("FECHA1", fechaRango));
                report.SetParameters(new ReportParameter("SUCURSAL", buzonDTO.NN));

                // 6. Render a Excel
                byte[] excelBytes = report.Render(
                    format: "EXCELOPENXML",
                    deviceInfo: null,
                    out var mimeType, out var encoding, out var fileExt, out var streams, out var warnings
                );

                // 7. Preparar salida
                fileName = $"Acreditacion{buzonDTO.NN}_{DateTime.Now:yyyyMMdd_HHmmss}.{fileExt}";
                subject = $"Acreditaciones Buzón Inteligente [{buzonDTO.NN}] - {buzonDTO.FechaInicio:dd/MM/yyyy}";
                body = $"Acreditaciones del Buzón Inteligente {buzonDTO.NN} del <strong>{fechaRango}</strong>";

                return new MemoryStream(excelBytes);
            }
            catch (LocalProcessingException lpex)
            {
                string errMsg = $"[{DateTime.Now}] Error en Render: {lpex.Message} | Inner: {lpex.InnerException?.Message}";
                Console.Error.WriteLine(errMsg);
                File.AppendAllText(logPath, errMsg + Environment.NewLine);
                throw;
            }
            catch (Exception ex)
            {
                string errMsg = $"[{DateTime.Now}] Error genérico: {ex.Message} | Inner: {ex.InnerException?.Message}";
                Console.Error.WriteLine(errMsg);
                File.AppendAllText(logPath, errMsg + Environment.NewLine);
                throw;
            }
        }



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
            public string NombreWS { get; set; }
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
