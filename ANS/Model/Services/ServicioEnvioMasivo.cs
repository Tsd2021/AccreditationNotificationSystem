using Microsoft.Data.SqlClient;
using System.Data;
using ANS.Model.Interfaces;
using ANS.Model.GeneradorArchivoPorBanco;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using System.IO;
using System.Reflection;
using Xceed.Wpf.Toolkit.Primitives;

namespace ANS.Model.Services
{
    public class ServicioEnvioMasivo
    {

        public static ServicioEnvioMasivo instancia;
        public ServicioEmail _emailService { get; set; } = new ServicioEmail();
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
            List<BuzonDTO> buzonesConAcreditaciones = new List<BuzonDTO>();

            if (buzones != null && buzones.Count > 0)
            {
                await hidratarDTOconSusAcreditaciones(buzones);

                foreach (var buzonDTO in buzones)
                {
                    if (buzonDTO.Acreditaciones != null && buzonDTO.Acreditaciones.Count > 0)
                    {
                        buzonesConAcreditaciones.Add(buzonDTO);
                    }
                }

                await obtenerIdOperacionInicialYFinal(buzonesConAcreditaciones);

                await obtenerUsuarioYFechaDelDeposito(buzonesConAcreditaciones);

                await obtenerFechaUltimaConexionDelBuzon(buzonesConAcreditaciones);

                foreach (var b in buzonesConAcreditaciones)
                {

                    b.MontoTotal = b.Acreditaciones.Sum(x => x.Monto);

                    await generarYEnviarExcelPorBuzon(b);
                }
            }
        }

        private async Task obtenerFechaUltimaConexionDelBuzon(List<BuzonDTO> buzones)
        {
            if (buzones == null || buzones.Count == 0) return;

            var keys = buzones.Select(b => b.NC).Distinct().ToList();

            var tvp = new DataTable();

            tvp.Columns.Add("NC",typeof(string));

            foreach (var nc in keys)
                tvp.Rows.Add(nc);

            var query = @" Select k.NC,Niveles.FechaUltConex from Niveles inner join @ListaNC k 
                           ON niveles.CodigoBuzon = k.NC";

            using var conn = new SqlConnection(ConfiguracionGlobal.ConexionWebBuzones);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(query,conn);

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
                         AND d.idoperacion = k.IdOperacion;
                        ";

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
            foreach (var b in buzones)
            {
                foreach (var a in b.Acreditaciones)
                {
                    if (dict.TryGetValue((a.NC, a.IdOperacion), out var info))
                    {
                        a.Usuario = info.Usuario;
                        a.FechaDep = info.FechaDep;
                        a.Empresa = info.Empresa;
                    }
                    else
                    {
                        // Si no existe, dejas defaults o asignas nulo, según convenga
                        a.Usuario = null;
                        a.Empresa = null;
                    }
                }
            }
        }

        private async Task obtenerIdOperacionInicialYFinal(List<BuzonDTO> buzones)
        {
            foreach (BuzonDTO b in buzones)
            {
                b.IdOperacionFinal = b.Acreditaciones.Max(x => x.IdOperacion);

                using (SqlConnection cnn = new SqlConnection(ConfiguracionGlobal.Conexion22))

                {
                    string query = @"select top 1 idoperacion,fecha
                                        from AcreditacionDepositoDiegoTest
                                        where idbuzon = @nc
                                        and idoperacion < (select top 1 idoperacion from AcreditacionDepositoDiegoTest
                                        where idbuzon = @nc
                                        and convert(date,fecha) = convert(date,getdate())
                                        group by idoperacion,fecha
                                        order by idoperacion asc)
                                        order by idoperacion desc";

                    await cnn.OpenAsync();

                    SqlCommand cmd = new SqlCommand(query, cnn);

                    cmd.Parameters.AddWithValue("@nc", b.NC);

                    using (SqlDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        int fechaOrdinal = r.GetOrdinal("fecha");

                        int idOperacionOrdinal = r.GetOrdinal("IDOPERACION");

                        if (await r.ReadAsync())
                        {
                            b.FechaInicio = r.GetDateTime(fechaOrdinal);

                            b.IdOperacionInicio = r.GetInt64(idOperacionOrdinal);
                        }
                    }
                }
            }
        }

        public async Task hidratarDTOconSusAcreditaciones(List<BuzonDTO> deps)
        {
            if (deps == null || deps.Count == 0)
                return;

            var mapaBuzones = deps.ToDictionary(b => b.NC);

            var ncList = deps.Select(d => d.NC).Distinct().ToList();

            var tvp = new System.Data.DataTable();

            tvp.TableName = "ListaNC";

            tvp.Columns.Add("NC", typeof(string));

            foreach (var nc in ncList)
                tvp.Rows.Add(nc);

            using (SqlConnection conn = new SqlConnection(ConfiguracionGlobal.Conexion22))
            {
                await conn.OpenAsync();

                string query = @"select * from acreditaciondepositodiegotest where 
                                convert(date,fecha) = convert(date,getdate()) 
                                and idbuzon in (Select nc from @ListaNC)";

                SqlCommand cmd = new SqlCommand(query, conn);

                var p = cmd.Parameters.AddWithValue("@ListaNC", tvp);

                p.SqlDbType = SqlDbType.Structured;

                p.TypeName = "dbo.ListaNC";
                try
                {

                
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int ncOrdinal = reader.GetOrdinal("IDBUZON");
                    int idOperacionOrdinal = reader.GetOrdinal("IDOPERACION");
                    int idCuentaOrdinal = reader.GetOrdinal("IDCUENTA");
                    int monedaOrdinal = reader.GetOrdinal("MONEDA");
                    int montoOrdinal = reader.GetOrdinal("MONTO");


                    while (await reader.ReadAsync())
                    {
                        var acc = new AcreditacionDTO
                        {
                            NC = reader.GetString(ncOrdinal),
                            IdOperacion = reader.GetInt64(idOperacionOrdinal),
                            IdCuenta = reader.GetInt32(idCuentaOrdinal),
                            Divisa = reader.GetInt32(monedaOrdinal),
                            Monto = reader.GetDouble(montoOrdinal),

                        };
                        acc.setMoneda();

                        if (mapaBuzones.TryGetValue(acc.NC, out var buzon))
                        {
                            buzon.Acreditaciones.Add(acc);
                        }
                    }
                }
                }
                catch(Exception e)
                {
                    throw e;
                }
            }
        }

        private async Task<List<BuzonDTO>> getBuzonesByNumeroEnvioMasivo(int numEnvioMasivo)
        {

            int desde = 0;

            int hasta = 0;

            int desdeMins = 0;

            int hastaMins = 0;

            string query = "";

            List<BuzonDTO> retorno = new List<BuzonDTO>();

            switch (numEnvioMasivo)
            {
                //Son son los <= 7
                case 1:
                    desde = 0; hasta = 7;
                    query = @"SELECT NC,NN,SUCURSAL,CIERRE 
                            FROM dbo.CC 
                            WHERE ESTADO = 'alta' 
                            AND DATEPART(HOUR,   CIERRE) <= @hasta";
                    break;


                //Son los cierre > 7 pero < a 15:30
                case 2:
                    desde = 7; desdeMins = 0; hasta = 15; hastaMins = 30;

                    query = @"SELECT NC,NN,SUCURSAL,CIERRE 
                            FROM dbo.CC 
                            WHERE ESTADO = 'alta' 
                            AND DATEPART(HOUR,   CIERRE) > @desde 
                            AND DATEPART(MINUTE, CIERRE) > @desdeMins 
                            AND DATEPART(HOUR,   CIERRE)  <= @hasta 
                            AND DATEPART(MINUTE, CIERRE) <= @hastaMins;";
                    break;

                //Son los CIERRE > 15:30
                case 3:
                    desde = 15; desdeMins = 30; hasta = 19; hastaMins = 0;

                    query = @"SELECT NC,NN,SUCURSAL,CIERRE 
                            FROM dbo.CC 
                            WHERE ESTADO = 'alta' 
                            AND DATEPART(HOUR,   CIERRE) > @desde 
                            AND DATEPART(MINUTE, CIERRE) > @desdeMins 
                            AND DATEPART(HOUR,   CIERRE)  <= @hasta 
                            AND DATEPART(MINUTE, CIERRE) <= @hastaMins;";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(numEnvioMasivo));
            }

            using (SqlConnection conn = new SqlConnection(ConfiguracionGlobal.Conexion22))
            {
                await conn.OpenAsync();
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@desde", desde);
                cmd.Parameters.AddWithValue("@desdeMins", desdeMins);
                cmd.Parameters.AddWithValue("@hasta", hasta);
                cmd.Parameters.AddWithValue("@hastaMins", hastaMins);

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {

                    int ncOrdinal = reader.GetOrdinal("NC");
                    int nnOrdinal = reader.GetOrdinal("NN");
                    int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");
                    int cierreOrdinal = reader.GetOrdinal("CIERRE");

                    while (await reader.ReadAsync())
                    {
                        BuzonDTO dto = new BuzonDTO();
                        dto.NC = reader.GetString(ncOrdinal);
                        dto.NN = reader.GetString(nnOrdinal);
                        dto.Sucursal = reader.GetString(sucursalOrdinal);
                        dto.Cierre = reader.GetDateTime(cierreOrdinal);
                        dto.Email = "dchiquiar@tecnisegur.com.uy";
                        retorno.Add(dto);
                    }
                }
            }
            return retorno;
        }

        private async Task generarYEnviarExcelPorBuzon(BuzonDTO buzonDTO)
        {
            // ********** 1. Prepara ruta del logo y crea el workbook **********
            var logoPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Images", "logoTecniExcel.png"
            );

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Acreditaciones");

            // ********** 2. Inserta el logo **********
            if (File.Exists(logoPath))
            {
                // Ajusta el tamaño y posición según necesites
                var pic = ws.AddPicture(logoPath)
                            .MoveTo(ws.Cell("A1"))
                            .WithSize(80, 80);
            }

            // ********** 3. Título y subtítulo (rango de fechas) **********
            // Título
            ws.Range("C1", "H1").Merge()
              .Value = $"TOTALES Y DEPÓSITOS DE BUZONERA {buzonDTO.NN.ToUpper()}";


            var fechaHoy = DateTime.Today.ToString("dd/MM/yyyy");
            // 2. Hora y minuto tomados de Cierre
            var horaCierre = buzonDTO.Cierre.ToString("HH:mm");

            // Subtítulo con rango
            ws.Range("C2", "H2").Merge()
              .Value = $"DEL {buzonDTO.FechaInicio:dd/MM/yyyy HH:mm} AL {fechaHoy} {horaCierre}";

            // ********** 4. Cabecera de Totales **********
            var totalesHeaderRow = 4;
            var columnasTot = new[] { "EMPRESA", "TOTAL PESOS", "TOTAL DÓLARES", "TOTAL ARG", "TOTAL REALES", "TOTAL EUROS" };
            for (int i = 0; i < columnasTot.Length; i++)
            {
                ws.Cell(totalesHeaderRow, i + 1).Value = columnasTot[i];
                ws.Cell(totalesHeaderRow, i + 1).Style.Fill.SetBackgroundColor(XLColor.LightGoldenrodYellow)
                                                       .Font.SetBold();
            }

            // ********** 5. Datos de Totales **********
            // Calcula cada total, aunque no haya datos: filtra y suma o deja 0
            double suma(int div) => buzonDTO.Acreditaciones
                                           .Where(a => a.Divisa == div)
                                           .Sum(a => a.Monto);

            var totPesos = suma(1);
            var totDolares = suma(2);
            var totArg = suma(3);
            var totReales = suma(4);
            var totEuros = suma(5);

            // Fila de la empresa
            ws.Cell(totalesHeaderRow + 1, 1).Value = buzonDTO.Empresa;
            ws.Cell(totalesHeaderRow + 1, 2).Value = totPesos;
            ws.Cell(totalesHeaderRow + 1, 3).Value = totDolares;
            ws.Cell(totalesHeaderRow + 1, 4).Value = totArg;
            ws.Cell(totalesHeaderRow + 1, 5).Value = totReales;
            ws.Cell(totalesHeaderRow + 1, 6).Value = totEuros;

            // Fila de totales generales (si quisieras agrupar varias empresas en el futuro)
            ws.Cell(totalesHeaderRow + 2, 1).Value = "TOTAL";
            ws.Cell(totalesHeaderRow + 2, 2).Value = totPesos;
            ws.Cell(totalesHeaderRow + 2, 3).Value = totDolares;
            ws.Cell(totalesHeaderRow + 2, 4).Value = totArg;
            ws.Cell(totalesHeaderRow + 2, 5).Value = totReales;
            ws.Cell(totalesHeaderRow + 2, 6).Value = totEuros;

            // ********** 6. Cabecera de Detalle de Depósitos **********
            var detalleHeaderRow = totalesHeaderRow + 4;
            var columnasDet = new[] {
        "OPERACIÓN", "FECHA", "MONEDA", "TOTAL",
        "USUARIO", "EMPRESA", "SUCURSAL"
    };

            for (int i = 0; i < columnasDet.Length; i++)
            {
                ws.Cell(detalleHeaderRow, i + 1).Value = columnasDet[i];
                ws.Cell(detalleHeaderRow, i + 1).Style.Fill.SetBackgroundColor(XLColor.LightGoldenrodYellow)
                                                          .Font.SetBold();
            }

            // ********** 7. Filas de Depósitos **********
            var row = detalleHeaderRow + 1;
            foreach (var a in buzonDTO.Acreditaciones)
            {
                ws.Cell(row, 1).Value = a.IdOperacion;
                ws.Cell(row, 2).Value = a.FechaDep;           // tipo DateTime
                ws.Cell(row, 2).Style.DateFormat.SetFormat("yyyy-MM-dd HH:mm:ss");
                ws.Cell(row, 3).Value = a.Moneda;             // “UYU”, “USD”, …
                ws.Cell(row, 4).Value = a.Monto;
                ws.Cell(row, 5).Value = a.Usuario;
                ws.Cell(row, 6).Value = a.Empresa;
                ws.Cell(row, 7).Value = buzonDTO.Sucursal;
                row++;
            }

            // ********** 8. Ajustes finales (anchos) **********
            ws.Columns().AdjustToContents();

            // ********** 9. Guardar y enviar **********
      
            string fileName = $"EnvioMasivo_{buzonDTO.NC}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = Path.Combine(@"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\ENVIOMASIVO\", fileName);
            wb.SaveAs(filePath);


            try
            {
                /*
                 
                 string nombreArchivoMontevideo = "ReporteDiario_Santander_Montevideo_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
                string filePathMontevideo = Path.Combine(@"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\", nombreArchivoMontevideo);
                 */
                string asunto = $"Acreditaciones Buzón Inteligente [{buzonDTO.NN}] - {fechaHoy}";
                /*
                 
                 ACREDITACIONES BUZÓN INTELIGENTE TI 18 Y TACUAREMBO DEL 21/04/2025 14:30 AL 22/04/2025 09:30
FAVOR TENER EN CUENTA FECHA Y HORA DE ÚLTIMA CONEXIÓN DE BUZÓN: 22/04/2025 07:59
ATENCIÓN, ESTE ES UN CORREO ENVIADO AUTOMATICAMENTE, FAVOR NO RESPONDER EL MISMO
                 
                 */
                string cuerpo = $@"
                                <p>
                                  Acreditaciones del <strong>Buzón Inteligente {buzonDTO.NN}</strong><br/>
                                  del {buzonDTO.FechaInicio:dd/MM/yyyy HH:mm}<br/>
                                  al {fechaHoy} {horaCierre}
                                </p>
                                <p>
                                  <strong>Por favor, tener en cuenta fecha y hora de última conexión del buzón:</strong><br/>
                                  {buzonDTO.UltimaFechaConexion:dd/MM/yyyy HH:mm}
                                </p>
                                ";

                await _emailService.enviarExcelPorMailMasivo(filePath,asunto,cuerpo,buzonDTO.Email);


            }
            catch(Exception e)
            {
                Console.WriteLine("e");
                throw e;
            }


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
            public BuzonDTO()
            {

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
