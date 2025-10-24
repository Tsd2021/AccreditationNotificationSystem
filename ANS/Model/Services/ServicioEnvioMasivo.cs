
using Microsoft.Data.SqlClient;
using System.Data;
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

                var buzonesConAcreditaciones = buzones
                  .Where(b => b.Acreditaciones != null && b.Acreditaciones.Count > 0)
                  .ToList();

                if (buzonesConAcreditaciones.Count == 0) return;

                await obtenerUsuarioYFechaDelDeposito(buzones);

                await obtenerFechaUltimaConexionDelBuzon(buzones);

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
            catch (Exception e)
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


            var mapaBuzones = deps
     .GroupBy(b => b.NC)
     .ToDictionary(
         g => g.Key,
         g => g.First()
     );


            var hendersons = deps.Where(b => b.EsHenderson).Select(b => b.NC).Distinct().ToList();
            var normals = deps.Where(b => !b.EsHenderson).Select(b => b.NC).Distinct().ToList();


            DataTable BuildTvp(List<string> list)
            {
                var tvp = new DataTable();
                tvp.Columns.Add("NC", typeof(string));
                foreach (var nc in list) tvp.Rows.Add(nc);
                return tvp;
            }
            var tvpH = BuildTvp(hendersons);

            var tvpN = BuildTvp(normals);


            DateTime today = DateTime.Today;

            DateTime startH, endH;
            if (numEnvioMasivo == 1)
            {

                switch (today.DayOfWeek)
                {
                    case DayOfWeek.Monday:

                        startH = today.AddDays(-3).AddHours(14).AddMinutes(30);
                        break;
                    default:

                        startH = today.AddDays(-1).AddHours(14).AddMinutes(30);
                        break;
                }


                endH = today.AddHours(7);
            }
            else
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


        public async Task obtenerUsuarioYFechaDelDeposito(List<BuzonDTO> buzones)
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

            query =     @"SELECT c.NC, c.NN, c.SUCURSAL, c.CIERRE,c.IDCLIENTE , ws.NombreWS
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
                        dto.Email = "acreditaciones@tecnisegur.com.uy";
                        dto.IdCliente = reader.GetInt32(idClienteOrdinal);
                        dto.EsHenderson = dto.esHenderson();
                        dto.NumeroEnvioMasivo = numEnvioMasivo;
                        if (!reader.IsDBNull(nombreWSOrdinal))
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

                    string queryParaObtenerHenderson = @"select distinct cc.NC,NN,SUCURSAL,CIERRE,cc.IDCLIENTE,cnws.NombreWS from cc
                                                        inner join ClientesRelacionadosTest as cr 
                                                        on cc.IDCLIENTE = cr.IdRazonSocial 
                                                        left join CC_NombreWS cnws
                                                        on cc.NC = cnws.NC
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

                        int nombreWSOrdinal = reader2.GetOrdinal("NombreWS");

                        while (await reader2.ReadAsync())
                        {

                            BuzonDTO dto = new BuzonDTO();
                            dto.NC = reader2.GetString(ncOrdinal);
                            dto.NN = reader2.GetString(nnOrdinal);
                            dto.Sucursal = reader2.GetString(sucursalOrdinal);
                            dto.Cierre = reader2.GetDateTime(cierreOrdinal);
                            dto.Email = "acreditaciones@tecnisegur.com.uy";
                            dto.IdCliente = reader2.GetInt32(idClienteOrdinal);
                            dto.EsHenderson = true;
                            dto.NumeroEnvioMasivo = numEnvioMasivo;
                            if (!reader2.IsDBNull(nombreWSOrdinal))
                            {
                                dto.NombreWS = reader2.GetString(nombreWSOrdinal);
                            }
                            else
                            {
                                dto.NombreWS = "NO_DEFINIDO";
                            }
                            retorno.Add(dto);
                        }
                    }

                }
            }
            return retorno;
        }

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
    }
}
