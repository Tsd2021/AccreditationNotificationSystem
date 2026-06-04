using ANS.Model;
using ANS.Model.GeneradorArchivoPorBanco;
using ANS.Model.Interfaces;
using ANS.Runtime;
using ANS.Runtime.Guards;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ANS.Model.Services
{
    public class ServicioCuentaBuzon : IServicioCuentaBuzon
    {
        private string _conexionTSD = ConfiguracionGlobal.Conexion22;

        // ✅ Thread-safe: Lazy<T> garantiza inicialización única
        // Este servicio es usado por múltiples jobs de Quartz concurrentes, thread-safety es esencial
        private static readonly Lazy<ServicioCuentaBuzon> _lazy =
            new Lazy<ServicioCuentaBuzon>(() => new ServicioCuentaBuzon());

        public static ServicioCuentaBuzon instancia => _lazy.Value;

        public ServicioEmail _emailService { get; set; } = new ServicioEmail();

        public static ServicioCuentaBuzon getInstancia()
        {
            return _lazy.Value;
        }

        /// <summary>
        /// Helper para aplicar guardias y prefijos a rutas de archivos Excel
        /// </summary>
        private static string AplicarGuardiaYPrefijoRuta(string rutaCompleta, string contexto)
        {
            // Aplicar prefijo TEST_ al nombre de archivo si está en modo TEST
            string fileName = Path.GetFileName(rutaCompleta);
            string directory = Path.GetDirectoryName(rutaCompleta);
            fileName = FileSystemGuard.GetFileNameWithTestPrefix(fileName);
            string rutaFinal = Path.Combine(directory, fileName);

            // ✅ GUARDIA: Validar que la escritura esté permitida
            FileSystemGuard.EnsureWriteAllowed(rutaFinal, contexto);

            // Asegurar que el directorio existe
            Directory.CreateDirectory(directory);

            return rutaFinal;
        }
        public List<DtoAcreditacionesPorEmpresa> getAcreditacionesDeHoy_BackUp(Banco b)
        {
            var lista = new List<DtoAcreditacionesPorEmpresa>();
            const string sql = @"
                                SELECT 
                                cb.EMPRESA,
                                LTRIM(RTRIM(cc.SUCURSAL))    AS Ciudad,
                                cb.CUENTA,
                                acc.MONEDA,
                                LTRIM(RTRIM(cb.SUCURSAL))    AS Sucursal,
                                SUM(acc.MONTO)               AS Total
                                FROM ConfiguracionAcreditacion AS config
                                INNER JOIN CUENTASBUZONES AS cb
                                ON config.CuentasBuzonesId = cb.ID
                                INNER JOIN cc
                                ON cb.IDCLIENTE = cc.IDCLIENTE
                                AND config.NC       = cc.NC
                                INNER JOIN {TableNameResolver.AcreditacionDeposito} AS acc 
                                ON acc.IDBUZON  = config.NC
                                AND acc.IDCUENTA = cb.ID
                                WHERE cb.BANCO = @banco
                                AND CAST(acc.FECHA AS date) = CAST(GETDATE() AS date)
                                GROUP BY 
                                cb.EMPRESA,
                                LTRIM(RTRIM(cc.SUCURSAL)),
                                cb.CUENTA,
                                acc.MONEDA,
                                LTRIM(RTRIM(cb.SUCURSAL))
                                ORDER BY 
                                cb.EMPRESA,
                                acc.MONEDA;";

            using var conn = new SqlConnection(_conexionTSD);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@banco", b.NombreBanco);
            conn.Open();

            using var rdr = cmd.ExecuteReader();
            int ordEmpresa = rdr.GetOrdinal("EMPRESA");
            int ordCiudad = rdr.GetOrdinal("Ciudad");
            int ordSucursal = rdr.GetOrdinal("Sucursal");
            int ordCuenta = rdr.GetOrdinal("CUENTA");
            int ordMoneda = rdr.GetOrdinal("MONEDA");
            int ordTotal = rdr.GetOrdinal("Total");

            while (rdr.Read())
            {
                var dto = new DtoAcreditacionesPorEmpresa
                {
                    Empresa = rdr.GetString(ordEmpresa).Trim(),
                    Ciudad = rdr.GetString(ordCiudad).Trim(),
                    Sucursal = rdr.GetString(ordSucursal).Trim(),
                    NumeroCuenta = rdr.GetString(ordCuenta),
                    Divisa = rdr.GetInt32(ordMoneda),
                    Monto = rdr.GetDouble(ordTotal)
                };
                dto.setMoneda(); // mapea Divisa → string, p.ej. "UYU" o "USD"
                if (dto.Empresa.All(char.IsDigit))
                {
                    dto.Empresa = ServicioCliente
                                      .getInstancia()
                                      .getByRut(dto.Empresa)
                                      .Nombre;
                }
                lista.Add(dto);
            }

            return lista;
        }

        public List<DtoAcreditacionesPorEmpresa> getAcreditacionesDeHoy(Banco b, DateTime? diaOperativo = null)
        {
            var lista = new List<DtoAcreditacionesPorEmpresa>();
            var baseDia = (diaOperativo ?? DateTime.Today).Date;

            // ✅ Usar TableNameResolver para obtener nombre de tabla según RuntimeMode
            var tableName = TableNameResolver.AcreditacionDeposito;
            TableNameResolver.ValidateTableName(tableName, "ServicioCuentaBuzon.getAcreditacionesDeHoy");
            string sql = $@"
            SELECT 
            CB.EMPRESA                                     AS EMPRESA, 
            LTRIM(RTRIM(CC.SUCURSAL))                      AS Ciudad,      
            CB.SUCURSAL                                    AS Sucursal, 
            CB.CUENTA                                      AS CUENTA, 
            COALESCE(AD.MONEDA, CB.MONEDA)                 AS MONEDA, 
            CAST(SUM(AD.MONTO) AS FLOAT)                   AS Total       
            FROM dbo.{tableName} AS AD 
            INNER JOIN dbo.CUENTASBUZONES          AS CB ON CB.ID = AD.IDCUENTA 
            LEFT  JOIN dbo.CC                      AS CC ON CC.NC = AD.IDBUZON 
                                                AND CC.IDCLIENTE = CB.IDCLIENTE 
            WHERE 
            AD.IDBANCO = @pIdBanco 
            AND AD.FECHA >= @fechaDesde
            AND AD.FECHA < DATEADD(day, 1, @fechaDesde)
            GROUP BY
            CB.EMPRESA,
            LTRIM(RTRIM(CC.SUCURSAL)),
            CB.SUCURSAL,
            CB.CUENTA,
            COALESCE(AD.MONEDA, CB.MONEDA)
            ORDER BY
            EMPRESA ASC, Sucursal, CUENTA, MONEDA;";

            using var conn = new SqlConnection(_conexionTSD);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@pIdBanco", SqlDbType.Int).Value = b.BancoId;   // usa el ID de banco
            cmd.Parameters.Add("@fechaDesde", SqlDbType.DateTime).Value = baseDia;


            conn.Open();
            using var rdr = cmd.ExecuteReader();

            int ordEmpresa = rdr.GetOrdinal("EMPRESA");
            int ordCiudad = rdr.GetOrdinal("Ciudad");
            int ordSucursal = rdr.GetOrdinal("Sucursal");
            int ordCuenta = rdr.GetOrdinal("CUENTA");
            int ordMoneda = rdr.GetOrdinal("MONEDA");
            int ordTotal = rdr.GetOrdinal("Total");

            while (rdr.Read())
            {
                var dto = new DtoAcreditacionesPorEmpresa
                {
                    Empresa = rdr.IsDBNull(ordEmpresa) ? string.Empty : rdr.GetString(ordEmpresa).Trim(),
                    Ciudad = rdr.IsDBNull(ordCiudad) ? string.Empty : rdr.GetString(ordCiudad).Trim(),
                    Sucursal = rdr.IsDBNull(ordSucursal) ? string.Empty : rdr.GetString(ordSucursal).Trim(),
                    NumeroCuenta = rdr.IsDBNull(ordCuenta) ? string.Empty : rdr.GetString(ordCuenta),
                    Divisa = rdr.IsDBNull(ordMoneda) ? 0 : rdr.GetInt32(ordMoneda),
                    Monto = rdr.IsDBNull(ordTotal) ? 0d : rdr.GetDouble(ordTotal)
                };

                dto.setMoneda(); // mapea Divisa → "UYU"/"USD"

                // Si EMPRESA viene como RUT numérico, traducir a nombre
                if (!string.IsNullOrWhiteSpace(dto.Empresa) && dto.Empresa.All(char.IsDigit))
                {
                    dto.Empresa = ServicioCliente
                                      .getInstancia()
                                      .getByRut(dto.Empresa)
                                      ?.Nombre ?? dto.Empresa;
                }

                lista.Add(dto);
            }

            return lista;

        }


        //Metodo para obtener todas las cuentas de buzones,por cliente,banco,cuenta,moneda,tipo de acreditación,todo completo.
        public List<CuentaBuzon> getAll()
        {

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {

                List<CuentaBuzon> listaTotalDeCuentasBuzones = new List<CuentaBuzon>();

                string queryUnionTotal = "SELECT DISTINCT * " +
                "FROM ( " +
                "    SELECT cc.nc, cc.nn, cc.banco, cb.EMPRESA, cb.CUENTA, cc.IDCLIENTE, cb.MONEDA, cb.ID AS IDCUENTABUZON " +
                "    FROM cc " +
                "    INNER JOIN cuentasbuzones cb ON cc.IDCLIENTE = cb.IDCLIENTE " +
                "    WHERE cc.banco IS NOT NULL " +
                "      AND LTRIM(RTRIM(cc.banco)) <> '' " +
                "      AND cc.banco <> 'SIN ASIGNAR' " +
                "    UNION " +
                "    SELECT cc.nc, cc.nn, cb.BANCO AS banco, cb.EMPRESA, cb.CUENTA, cc.IDCLIENTE, cb.MONEDA, cb.ID AS IDCUENTABUZON " +
                "    FROM cc " +
                "    INNER JOIN ClientesRelacionadosTest crt ON cc.IDCLIENTE = crt.IdRazonSocial " +
                "    INNER JOIN cuentasbuzones cb ON crt.idcliente = cb.IDCLIENTE " +
                "    WHERE cc.banco IS NOT NULL " +
                "      AND cc.IDCLIENTE = 164 " +
                "      AND cb.banco LIKE '%sant%' " +
                "    UNION " +
                "    SELECT cc.nc, cc.nn, cb.BANCO AS banco, cb.EMPRESA, cb.CUENTA, cc.IDCLIENTE, cb.MONEDA, cb.ID AS IDCUENTABUZON " +
                "    FROM cc " +
                "    INNER JOIN ClientesRelacionadosTest crt ON cc.IDCLIENTE = crt.IdRazonSocial " +
                "    INNER JOIN cuentasbuzones cb ON crt.idcliente = cb.IDCLIENTE " +
                "    WHERE cc.banco IS NOT NULL " +
                "      AND cc.IDCLIENTE <> 164 " +
                "      AND cb.banco LIKE '%scot%' " +
                ") AS t " +
                "ORDER BY nc asc;";


                conn.Open();

                SqlCommand cmd = new SqlCommand(queryUnionTotal, conn);


                using (SqlDataReader reader = cmd.ExecuteReader())
                {

                    int ncOrdinal = reader.GetOrdinal("NC");
                    int bancoOrdinal = reader.GetOrdinal("BANCO");
                    int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");
                    int idCuentaOrdinal = reader.GetOrdinal("IDCUENTABUZON");
                    int monedaOrdinal = reader.GetOrdinal("MONEDA");
                    int empresaOrdinal = reader.GetOrdinal("EMPRESA");
                    int nnOrdinal = reader.GetOrdinal("NN");

                    while (reader.Read())
                    {
                        CuentaBuzon cuentaBuzon = new CuentaBuzon()
                        {
                            NC = reader.GetString(ncOrdinal),
                            Banco = reader.GetString(bancoOrdinal),
                            IdCliente = reader.GetInt32(idClienteOrdinal),
                            IdCuenta = reader.GetInt32(idCuentaOrdinal),
                            Moneda = reader.GetString(monedaOrdinal),
                            Empresa = reader.GetString(empresaOrdinal),
                            NN = reader.GetString(nnOrdinal)
                        };
                        listaTotalDeCuentasBuzones.Add(cuentaBuzon);
                    }
                }

                return listaTotalDeCuentasBuzones;
            }


        }

        public List<CuentaBuzon> getAllByTipoAcreditacion(string tipoAcreditacion)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                string query = @"SELECT c.NC, cb.BANCO, c.BANCO as BANCOBUZON, c.CIERRE, c.IDCLIENTE, cb.CUENTA, cb.MONEDA, cb.EMPRESA, config.TipoAcreditacion,c.SUCURSAL as CIUDAD, cb.SUCURSAL, cb.TANDA, c.NN
                                from ConfiguracionAcreditacion config 
                                inner join cuentasbuzones cb on config.CuentasBuzonesId = cb.id 
                                inner join cc c on cb.idcliente = c.IDCLIENTE 
                                and c.nc = config.nc 
                                where config.TipoAcreditacion = @tipoAcreditacion;";

                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tipoAcreditacion", tipoAcreditacion);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // Obtener los índices de las columnas
                    int ncOrdinal = reader.GetOrdinal("NC");
                    int bancoOrdinal = reader.GetOrdinal("BANCO");
                    int bancoBuzonOrdinal = reader.GetOrdinal("BANCOBUZON");
                    int cierreOrdinal = reader.GetOrdinal("CIERRE");
                    int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");
                    int cuentaOrdinal = reader.GetOrdinal("CUENTA");
                    int monedaOrdinal = reader.GetOrdinal("MONEDA");
                    int empresaOrdinal = reader.GetOrdinal("EMPRESA");
                    int tipoAcreditacionOrdinal = reader.GetOrdinal("TipoAcreditacion");
                    int ciudadOrdinal = reader.GetOrdinal("CIUDAD");
                    int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");
                    int tandaOrdinal = reader.GetOrdinal("TANDA");
                    int nnOrdinal = reader.GetOrdinal("NN");

                    while (reader.Read())
                    {
                        CuentaBuzon cuentaBuzon = new CuentaBuzon
                        {
                            NC = reader.GetString(ncOrdinal),
                            Banco = reader.GetString(bancoOrdinal),
                            BancoBuzon = reader.IsDBNull(bancoBuzonOrdinal) ? null : reader.GetString(bancoBuzonOrdinal),
                            Cierre = reader.IsDBNull(cierreOrdinal) ? (DateTime?)null : reader.GetDateTime(cierreOrdinal),
                            IdCliente = reader.GetInt32(idClienteOrdinal),
                            Cuenta = reader.GetString(cuentaOrdinal),
                            Moneda = reader.GetString(monedaOrdinal),
                            Empresa = reader.GetString(empresaOrdinal),
                            SucursalCuenta = reader.GetString(sucursalOrdinal),
                            Ciudad = reader.GetString(ciudadOrdinal),
                            Producto = reader.GetInt32(tandaOrdinal),
                            NN = reader.GetString(nnOrdinal)
                        };

                        cuentaBuzon.setDivisa();

                        // Verificar y asignar la configuración
                        if (!reader.IsDBNull(tipoAcreditacionOrdinal))
                        {
                            cuentaBuzon.Config = new ConfiguracionAcreditacion(reader.GetString(tipoAcreditacionOrdinal));
                        }

                        buzonesFound.Add(cuentaBuzon);
                    }

                }

            }

            return buzonesFound;

        }
        public List<CuentaBuzon> getAllByTipoAcreditacionYBanco(string tipoAcreditacion, Banco banco)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();

            string query = "";

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {

                // POR DEFAULT 
                query = @"select distinct c.NC, 
                        cb.BANCO, 
                        c.BANCO as BANCOBUZON,
                        c.CIERRE, 
                        c.IDCLIENTE, 
                        cb.CUENTA, 
                        cb.MONEDA, 
                        cb.EMPRESA, 
                        config.TipoAcreditacion, 
                        c.SUCURSAL as CIUDAD, 
                        cb.SUCURSAL, 
                        c.IDCC, 
                        cb.ID, 
                        c.NN  
                        from ConfiguracionAcreditacion config 
                        inner join cuentasbuzones cb on config.CuentasBuzonesId = cb.id 
                        inner join cc c on cb.idcliente = c.IDCLIENTE 
                        and c.nc = config.nc  
                        where cb.BANCO = @bank 
                        and config.TipoAcreditacion = @tipoAcreditacion;";

                //Excluye DELASSIERRAS ya que acredita DIA A DIA pero en una hora específica.
                if (tipoAcreditacion.ToUpper() == VariablesGlobales.diaxdia.ToUpper())
                {
                    if (banco.NombreBanco.ToUpper() == VariablesGlobales.santander.ToUpper())
                    {
                        query = @"select distinct c.NC,
                            cb.BANCO,
                            c.BANCO as BANCOBUZON,
                            c.CIERRE,
                            c.IDCLIENTE,
                            cb.CUENTA,
                            cb.MONEDA,
                            cb.EMPRESA,
                            config.TipoAcreditacion,
                            c.SUCURSAL as CIUDAD,
                            cb.SUCURSAL,
                            c.IDCC,
                            cb.ID,
                            c.NN  
                            from ConfiguracionAcreditacion config 
                            inner join cuentasbuzones cb on config.CuentasBuzonesId = cb.id 
                            inner join cc c on cb.idcliente = c.IDCLIENTE 
                            and c.nc = config.nc 
                            where cb.BANCO = @bank 
                            and cb.IDCLIENTE NOT IN (268)
                            and config.TipoAcreditacion = @tipoAcreditacion;";

                    }

                    if (banco.NombreBanco.ToUpper() == VariablesGlobales.bbva.ToUpper())
                    {
                        // Excluir Nike (ID 998) porque se acredita en su job específico a las 14:25
                        query = @"select distinct c.NC,
                            cb.BANCO,
                            c.BANCO as BANCOBUZON,
                            c.CIERRE,
                            c.IDCLIENTE,
                            cb.CUENTA,
                            cb.MONEDA,
                            cb.EMPRESA,
                            config.TipoAcreditacion,
                            c.SUCURSAL as CIUDAD,
                            cb.SUCURSAL,
                            c.IDCC,
                            cb.ID,
                            c.NN  
                            from ConfiguracionAcreditacion config 
                            inner join cuentasbuzones cb on config.CuentasBuzonesId = cb.id 
                            inner join cc c on cb.idcliente = c.IDCLIENTE 
                            and c.nc = config.nc 
                            where cb.BANCO = @bank 
                            and cb.IDCLIENTE NOT IN (998)
                            and config.TipoAcreditacion = @tipoAcreditacion;";

                    }
                }

                if (tipoAcreditacion.ToUpper() == VariablesGlobales.diaxdia.ToUpper())
                {
                    if (banco.NombreBanco.ToUpper() == VariablesGlobales.scotiabank.ToUpper())
                    {

                        //Si es dia a dia Scotiabank hay que excluir todo Henderson (164) y Farmashop (179) porque ya fueron acreditados.
                        //Henderson se acredita en Tanda1/Tanda2, Farmashop se acredita en su job específico a las 06:58.
                        //Y tambien la consulta es especial porque no exluye RELACIONADOS!
                        query = @"select distinct c.NC,
                                cb.BANCO,
                                c.BANCO as BANCOBUZON,
                                c.CIERRE,
                                c.IDCLIENTE,
                                cb.CUENTA,
                                cb.MONEDA,
                                cb.EMPRESA,
                                config.TipoAcreditacion,
                                c.SUCURSAL as CIUDAD,
                                cb.SUCURSAL,
                                c.IDCC,
                                cb.ID,
                                c.NN  
                                from ConfiguracionAcreditacion config 
                                inner join cuentasbuzones cb on config.CuentasBuzonesId = cb.id 
                                inner join cc c on cb.idcliente = c.IDCLIENTE 
                                and c.nc = config.nc 
                                where cb.BANCO = @bank 
                                and cb.idcliente not in (164, 179)
                                and config.TipoAcreditacion = @tipoAcreditacion;";
                    }
                }

                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@tipoAcreditacion", tipoAcreditacion);

                cmd.Parameters.AddWithValue("@bank", banco.NombreBanco);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {

                    int ncOrdinal = reader.GetOrdinal("NC");
                    int bancoOrdinal = reader.GetOrdinal("BANCO");
                    int bancoBuzonOrdinal = reader.GetOrdinal("BANCOBUZON");
                    int cierreOrdinal = reader.GetOrdinal("CIERRE");
                    int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");
                    int cuentaOrdinal = reader.GetOrdinal("CUENTA");
                    int monedaOrdinal = reader.GetOrdinal("MONEDA");
                    int empresaOrdinal = reader.GetOrdinal("EMPRESA");
                    int tipoAcreditacionOrdinal = reader.GetOrdinal("TipoAcreditacion");
                    int ciudadOrdinal = reader.GetOrdinal("CIUDAD");
                    int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");
                    int idReferenciaAlCliente = reader.GetOrdinal("IDCC");
                    int idCuenta = reader.GetOrdinal("ID");
                    int nnOrdinal = reader.GetOrdinal("NN");

                    while (reader.Read())
                    {
                        CuentaBuzon cuentaBuzon = new CuentaBuzon
                        {
                            NC = reader.GetString(ncOrdinal),
                            Banco = reader.GetString(bancoOrdinal),
                            BancoBuzon = reader.IsDBNull(bancoBuzonOrdinal) ? null : reader.GetString(bancoBuzonOrdinal),
                            Cierre = reader.IsDBNull(cierreOrdinal) ? (DateTime?)null : reader.GetDateTime(cierreOrdinal),
                            IdCliente = reader.GetInt32(idClienteOrdinal),
                            Cuenta = reader.GetString(cuentaOrdinal).Replace("\r", "").Replace("\n", ""), // Limpia \r\n
                            Moneda = reader.GetString(monedaOrdinal),
                            Empresa = reader.GetString(empresaOrdinal),
                            SucursalCuenta = reader.GetString(sucursalOrdinal),
                            Ciudad = reader.GetString(ciudadOrdinal),
                            IdReferenciaAlCliente = reader.GetString(idReferenciaAlCliente),
                            IdCuenta = reader.GetInt32(idCuenta),
                            NN = reader.GetString(nnOrdinal)

                        };

                        cuentaBuzon.setDivisa();

                        cuentaBuzon.setCashOffice(); // ✅ El banco del BUZÓN (CC.BANCO) define si es CashOffice, no el banco de la cuenta (cuentasbuzones.BANCO)

                        if (!reader.IsDBNull(tipoAcreditacionOrdinal))
                        {
                            cuentaBuzon.Config = new ConfiguracionAcreditacion(reader.GetString(tipoAcreditacionOrdinal));
                        }


                        if (banco.NombreBanco.ToUpper() == VariablesGlobales.santander.ToUpper() && tipoAcreditacion.ToUpper() == VariablesGlobales.p2p.ToUpper())
                        {
                            if (cuentaBuzon.Empresa == "TATA MULTIAHORRO")
                            {

                                cuentaBuzon.Empresa = "MULTIAHORRO";

                            }

                            if (cuentaBuzon.Empresa == "TATA BAS")
                            {

                                cuentaBuzon.Empresa = "BAS";

                            }
                        }

                        buzonesFound.Add(cuentaBuzon);
                    }
                }
            }
            return buzonesFound;
        }
        public List<CuentaBuzon> getAllByBanco(Banco banco)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                string query = @"
                SELECT DISTINCT 
                c.NC, 
                cb.BANCO, 
                c.BANCO as BANCOBUZON,
                c.CIERRE, 
                c.IDCLIENTE, 
                cb.CUENTA, 
                cb.MONEDA, 
                cb.EMPRESA, 
                c.SUCURSAL AS CIUDAD, 
                cb.SUCURSAL, 
                c.IDCC, 
                cb.ID, 
                c.NN, 
                config.TipoAcreditacion AS CONFIGURACION 
                from ConfiguracionAcreditacion config 
                inner join cuentasbuzones cb on config.CuentasBuzonesId = cb.id 
                inner join cc c on cb.idcliente = c.IDCLIENTE 
                and c.nc = config.nc 
                where cb.BANCO = @banco";

                conn.Open();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@banco", banco.NombreBanco);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // Obtener los índices de las columnas
                        int ncOrdinal = reader.GetOrdinal("NC");
                        int bancoOrdinal = reader.GetOrdinal("BANCO");
                        int bancoBuzonOrdinal = reader.GetOrdinal("BANCOBUZON");
                        int cierreOrdinal = reader.GetOrdinal("CIERRE");
                        int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");
                        int cuentaOrdinal = reader.GetOrdinal("CUENTA");
                        int monedaOrdinal = reader.GetOrdinal("MONEDA");
                        int empresaOrdinal = reader.GetOrdinal("EMPRESA");
                        int ciudadOrdinal = reader.GetOrdinal("CIUDAD");
                        int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");
                        int idReferenciaOrdinal = reader.GetOrdinal("IDCC");
                        int idCuentaOrdinal = reader.GetOrdinal("ID");
                        int nnOrdinal = reader.GetOrdinal("NN");
                        int configOrdinal = reader.GetOrdinal("CONFIGURACION");

                        while (reader.Read())
                        {
                            CuentaBuzon cuentaBuzon = new CuentaBuzon
                            {
                                NC = reader.GetString(ncOrdinal),
                                Banco = reader.GetString(bancoOrdinal),
                                BancoBuzon = reader.IsDBNull(bancoBuzonOrdinal) ? null : reader.GetString(bancoBuzonOrdinal),
                                Cierre = reader.IsDBNull(cierreOrdinal) ? (DateTime?)null : reader.GetDateTime(cierreOrdinal),
                                IdCliente = reader.GetInt32(idClienteOrdinal),
                                Cuenta = reader.GetString(cuentaOrdinal).Replace("\r", "").Replace("\n", ""), // Limpia \r\n
                                Moneda = reader.GetString(monedaOrdinal),
                                Empresa = reader.GetString(empresaOrdinal),
                                Ciudad = reader.GetString(ciudadOrdinal),
                                SucursalCuenta = reader.GetString(sucursalOrdinal),
                                IdReferenciaAlCliente = reader.GetString(idReferenciaOrdinal),
                                IdCuenta = reader.GetInt32(idCuentaOrdinal),
                                NN = reader.GetString(nnOrdinal)

                            };

                            ConfiguracionAcreditacion configActual = new ConfiguracionAcreditacion(reader.GetString(configOrdinal));

                            cuentaBuzon.Config = configActual;

                            cuentaBuzon.setDivisa();

                            cuentaBuzon.setCashOffice();

                            buzonesFound.Add(cuentaBuzon);
                        }
                    }
                }
            }

            return buzonesFound;
        }
        public async Task<List<CuentaBuzon>> getCuentasPorClienteBancoYTipoAcreditacion(int idCliente, Banco bank, ConfiguracionAcreditacion configuracionAcreditacion)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();
            string query = @"select config.nc, cb.banco, c.banco as BANCOBUZON, c.cierre, cb.idcliente, cb.cuenta, cb.moneda, cb.empresa, config.TipoAcreditacion AS CONFIGURACION, c.sucursal as ciudad, cb.sucursal, c.idcc, cb.id, c.nn 
                            from ConfiguracionAcreditacion config 
                            inner join cuentasbuzones cb on config.CuentasBuzonesId = cb.id 
                            inner join cc c on c.nc = config.nc 
                            where CB.BANCO = @banco AND config.TipoAcreditacion = @tipoAcreditacion  
                            AND CB.IDCLIENTE = @idCliente";

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idCliente", idCliente);
                    cmd.Parameters.AddWithValue("@banco", bank.NombreBanco);
                    cmd.Parameters.AddWithValue("@tipoAcreditacion", configuracionAcreditacion.TipoAcreditacion);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int ncOrdinal = reader.GetOrdinal("NC");
                        int bancoOrdinal = reader.GetOrdinal("BANCO");
                        int bancoBuzonOrdinal = reader.GetOrdinal("BANCOBUZON");
                        int cierreOrdinal = reader.GetOrdinal("CIERRE");
                        int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");
                        int cuentaOrdinal = reader.GetOrdinal("CUENTA");
                        int monedaOrdinal = reader.GetOrdinal("MONEDA");
                        int empresaOrdinal = reader.GetOrdinal("EMPRESA");
                        int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");
                        int sucursalCiudadOrdinal = reader.GetOrdinal("CIUDAD");
                        int idReferenciaOrdinal = reader.GetOrdinal("IDCC");
                        int idCuentaOrdinal = reader.GetOrdinal("ID");
                        int nnOrdinal = reader.GetOrdinal("NN");
                        int configOrdinal = reader.GetOrdinal("CONFIGURACION");
                        while (await reader.ReadAsync())
                        {
                            CuentaBuzon cuentaBuzon = new CuentaBuzon
                            {
                                NC = reader.GetString(ncOrdinal),
                                Banco = reader.GetString(bancoOrdinal),
                                BancoBuzon = reader.IsDBNull(bancoBuzonOrdinal) ? null : reader.GetString(bancoBuzonOrdinal),
                                Cierre = reader.IsDBNull(cierreOrdinal) ? (DateTime?)null : reader.GetDateTime(cierreOrdinal),
                                IdCliente = reader.GetInt32(idClienteOrdinal),
                                Cuenta = reader.GetString(cuentaOrdinal).Replace("\r", "").Replace("\n", ""),
                                Moneda = reader.GetString(monedaOrdinal),
                                Empresa = reader.GetString(empresaOrdinal),
                                SucursalCuenta = reader.GetString(sucursalOrdinal),
                                IdReferenciaAlCliente = reader.GetString(idReferenciaOrdinal),
                                IdCuenta = reader.GetInt32(idCuentaOrdinal),
                                NN = reader.GetString(nnOrdinal),
                                Ciudad = reader.GetString(sucursalCiudadOrdinal)
                            };

                            ConfiguracionAcreditacion configActual = new ConfiguracionAcreditacion(reader.GetString(configOrdinal));

                            cuentaBuzon.Config = configActual;

                            cuentaBuzon.setDivisa();

                            cuentaBuzon.setCashOffice();

                            buzonesFound.Add(cuentaBuzon);
                        }
                    }
                }
            }
            return buzonesFound;
        }

        public async Task<List<CuentaBuzon>> getCuentasPorClienteBancoTipoAcreditacionYTanda(int idCliente, Banco bank, ConfiguracionAcreditacion configuracionAcreditacion, int numTanda)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();

            string query = @"select config.nc, cb.banco, c.banco as BANCOBUZON, c.cierre, cb.idcliente, cb.cuenta, cb.moneda, cb.empresa, config.TipoAcreditacion AS CONFIGURACION, c.sucursal as ciudad, cb.sucursal, c.idcc, cb.id, c.nn 
                            from ConfiguracionAcreditacion config 
                            inner join cuentasbuzones cb on config.CuentasBuzonesId = cb.id 
                            inner join cc c on c.nc = config.nc 
                            where CB.BANCO = @banco AND config.TipoAcreditacion = @tipoAcreditacion  
                            AND CB.IDCLIENTE = @idCliente";


            #region Parche_Coboe_Scotiabank_Tanda1
            //        if (bank.NombreBanco.ToUpper() == VariablesGlobales.scotiabank.ToUpper() && numTanda == 1)
            //        {
            //            query = @"
            //SELECT
            //    config.NC,
            //    cb.BANCO,
            //    c.CIERRE,
            //    cb.IDCLIENTE,
            //    cb.CUENTA,
            //    cb.MONEDA,
            //    cb.EMPRESA,
            //    config.TipoAcreditacion AS CONFIGURACION,
            //    c.SUCURSAL              AS CIUDAD,
            //    cb.SUCURSAL,
            //    c.IDCC,
            //    cb.ID,
            //    c.NN
            //FROM ConfiguracionAcreditacion AS config
            //JOIN CUENTASBUZONES AS cb
            //  ON config.CuentasBuzonesId = cb.ID
            //JOIN CC AS c
            //  ON c.NC = config.NC
            //WHERE cb.BANCO = @banco
            //  AND cb.IDCLIENTE IN (@idCliente, 179)
            //  AND (
            //        (cb.IDCLIENTE = 179 AND REPLACE(LOWER(config.TipoAcreditacion),' ','') = 'diaadia')
            //     OR (cb.IDCLIENTE <> 179 AND config.TipoAcreditacion = @tipoAcreditacion)
            //  );";
            //        }
            #endregion
            try
            {


                using (SqlConnection conn = new SqlConnection(_conexionTSD))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@idCliente", idCliente);
                        cmd.Parameters.AddWithValue("@banco", bank.NombreBanco);
                        cmd.Parameters.AddWithValue("@tipoAcreditacion", configuracionAcreditacion.TipoAcreditacion);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int ncOrdinal = reader.GetOrdinal("NC");
                            int bancoOrdinal = reader.GetOrdinal("BANCO");
                            int cierreOrdinal = reader.GetOrdinal("CIERRE");
                            int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");
                            int cuentaOrdinal = reader.GetOrdinal("CUENTA");
                            int monedaOrdinal = reader.GetOrdinal("MONEDA");
                            int empresaOrdinal = reader.GetOrdinal("EMPRESA");
                            int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");
                            int sucursalCiudadOrdinal = reader.GetOrdinal("CIUDAD");
                            int idReferenciaOrdinal = reader.GetOrdinal("IDCC");
                            int idCuentaOrdinal = reader.GetOrdinal("ID");
                            int nnOrdinal = reader.GetOrdinal("NN");
                            int configOrdinal = reader.GetOrdinal("CONFIGURACION");
                            while (await reader.ReadAsync())
                            {
                                CuentaBuzon cuentaBuzon = new CuentaBuzon
                                {
                                    NC = reader.GetString(ncOrdinal),
                                    Banco = reader.GetString(bancoOrdinal),
                                    Cierre = reader.IsDBNull(cierreOrdinal) ? (DateTime?)null : reader.GetDateTime(cierreOrdinal),
                                    IdCliente = reader.GetInt32(idClienteOrdinal),
                                    Cuenta = reader.GetString(cuentaOrdinal).Replace("\r", "").Replace("\n", ""),
                                    Moneda = reader.GetString(monedaOrdinal),
                                    Empresa = reader.GetString(empresaOrdinal),
                                    SucursalCuenta = reader.GetString(sucursalOrdinal),
                                    IdReferenciaAlCliente = reader.GetString(idReferenciaOrdinal),
                                    IdCuenta = reader.GetInt32(idCuentaOrdinal),
                                    NN = reader.GetString(nnOrdinal),
                                    Ciudad = reader.GetString(sucursalCiudadOrdinal)
                                };

                                ConfiguracionAcreditacion configActual = new ConfiguracionAcreditacion(reader.GetString(configOrdinal));

                                cuentaBuzon.Config = configActual;

                                cuentaBuzon.setDivisa();

                                cuentaBuzon.setCashOffice();

                                buzonesFound.Add(cuentaBuzon);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return buzonesFound;
        }
        public async Task<List<CuentaBuzon>> getCuentaBuzonesByClienteYBanco(int idcliente, Banco bank)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();

            string query = @"select DISTINCT config.nc, cb.banco, c.banco as BANCOBUZON, c.cierre, cb.idcliente, cb.cuenta, cb.moneda, cb.empresa, config.TipoAcreditacion AS CONFIGURACION, c.sucursal as ciudad, cb.sucursal, c.idcc, cb.id, c.nn 
                            from ConfiguracionAcreditacion config 
                            inner join cuentasbuzones cb on config.CuentasBuzonesId = cb.id 
                            inner join cc c on cb.idcliente = c.IDCLIENTE 
                            and c.nc = config.nc 
                            where cb.BANCO = @bank
                            and cb.IDCLIENTE = @idcli";

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                await conn.OpenAsync();

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idcli", idcliente);

                    cmd.Parameters.AddWithValue("@bank", bank.NombreBanco);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {

                        int ncOrdinal = reader.GetOrdinal("NC");
                        int bancoOrdinal = reader.GetOrdinal("BANCO");
                        int bancoBuzonOrdinal = reader.GetOrdinal("BANCOBUZON");
                        int cierreOrdinal = reader.GetOrdinal("CIERRE");
                        int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");
                        int cuentaOrdinal = reader.GetOrdinal("CUENTA");
                        int monedaOrdinal = reader.GetOrdinal("MONEDA");
                        int empresaOrdinal = reader.GetOrdinal("EMPRESA");
                        int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");
                        int sucursalCiudadOrdinal = reader.GetOrdinal("CIUDAD");
                        int idReferenciaOrdinal = reader.GetOrdinal("IDCC");
                        int idCuentaOrdinal = reader.GetOrdinal("ID");
                        int nnOrdinal = reader.GetOrdinal("NN");
                        int configOrdinal = reader.GetOrdinal("CONFIGURACION");

                        while (await reader.ReadAsync())
                        {
                            CuentaBuzon cuentaBuzon = new CuentaBuzon
                            {
                                NC = reader.GetString(ncOrdinal),
                                Banco = reader.GetString(bancoOrdinal),
                                BancoBuzon = reader.IsDBNull(bancoBuzonOrdinal) ? null : reader.GetString(bancoBuzonOrdinal),
                                Cierre = reader.IsDBNull(cierreOrdinal) ? (DateTime?)null : reader.GetDateTime(cierreOrdinal),
                                IdCliente = reader.GetInt32(idClienteOrdinal),
                                Cuenta = reader.GetString(cuentaOrdinal).Replace("\r", "").Replace("\n", ""),
                                Moneda = reader.GetString(monedaOrdinal),
                                Empresa = reader.GetString(empresaOrdinal),
                                SucursalCuenta = reader.GetString(sucursalOrdinal),
                                IdReferenciaAlCliente = reader.GetString(idReferenciaOrdinal),
                                IdCuenta = reader.GetInt32(idCuentaOrdinal),
                                NN = reader.GetString(nnOrdinal),
                                Ciudad = reader.GetString(sucursalCiudadOrdinal),
                            };

                            ConfiguracionAcreditacion configActual = new ConfiguracionAcreditacion(reader.GetString(configOrdinal));

                            cuentaBuzon.Config = configActual;

                            cuentaBuzon.setDivisa();

                            cuentaBuzon.setCashOffice();

                            buzonesFound.Add(cuentaBuzon);
                        }
                    }
                }
            }

            return buzonesFound;
        }
        public async Task<GeneracionArchivoBancoResult> generarArchivoPorBanco(List<CuentaBuzon> listaCuentaBuzones, Banco banco, string tipoAcreditacion)
        {

            if (listaCuentaBuzones == null)
            {
                throw new Exception("Error en método generarArchivoPorBanco: listaBuzones es null.");
            }

            if (listaCuentaBuzones.Count == 0)
            {
                throw new Exception("Error en método generarArchivoPorBanco: Lista Buzones tiene 0 elementos");
            }

            // ✅ Logging: Resumen detallado al inicio de generación de archivo
            int totalDepositos = listaCuentaBuzones.Sum(cb => cb.Depositos?.Count ?? 0);
            decimal totalMonto = listaCuentaBuzones
                .Sum(cb => cb.Depositos?.Sum(d => d.Totales?.Sum(t => t.ImporteTotal) ?? 0) ?? 0);

            // Agrupar por ciudad y divisa para información más detallada
            var cuentasPorCiudad = listaCuentaBuzones.GroupBy(cb => cb.Ciudad ?? "N/A");
            var cuentasPorDivisa = listaCuentaBuzones.GroupBy(cb => cb.Divisa ?? "N/A");

            var detallesCiudad = string.Join(", ", cuentasPorCiudad.Select(g =>
                $"{g.Key}: {g.Count()} cuenta(s)"));
            var detallesDivisa = string.Join(", ", cuentasPorDivisa.Select(g =>
                $"{g.Key}: {g.Count()} cuenta(s)"));

            ServicioLog.instancia.WriteInfo(
                $"INICIO GENERACIÓN ARCHIVO | Banco: {banco.NombreBanco} | Tipo: {tipoAcreditacion} | " +
                $"Total Cuentas: {listaCuentaBuzones.Count} | Total Depósitos: {totalDepositos} | " +
                $"Monto Total: ${ServicioLog.instancia.FormatearMontoPublico(totalMonto)} | " +
                $"Distribución por Ciudad: {detallesCiudad} | " +
                $"Distribución por Divisa: {detallesDivisa}",
                $"ServicioCuentaBuzon | generarArchivoPorBanco");

            IBancoModoAcreditacion bank = BankFactory.GetModoAcreditacionByBanco(banco.NombreBanco, tipoAcreditacion);

            if (bank != null)

            {

                var resultadoGeneracion = await bank.GenerarArchivo(listaCuentaBuzones);

                // ✅ Logging: Resumen detallado al final de generación de archivo
                ServicioLog.instancia.WriteInfo(
                    $"FIN GENERACIÓN ARCHIVO | Banco: {banco.NombreBanco} | Tipo: {tipoAcreditacion} | " +
                    $"Archivo generado (disco) | Permite acreditar en tabla principal: {resultadoGeneracion.PermiteInsertarEnAcreditacionPrincipal} | " +
                    $"Auditoría envío fallido: {resultadoGeneracion.RequiereAuditoriaEnvioFallido} | " +
                    $"Total Cuentas procesadas: {listaCuentaBuzones.Count} | " +
                    $"Total Depósitos: {totalDepositos} | " +
                    $"Monto Total procesado: ${ServicioLog.instancia.FormatearMontoPublico(totalMonto)}",
                    $"ServicioCuentaBuzon | generarArchivoPorBanco");

                return resultadoGeneracion;

            }

            throw new Exception("Error en método generarArchivoPorBanco: el modo de" +
                " acreditacion por banco no fue encontrado.");
        }
        private static void LogearTimeoutSantander(List<CuentaBuzon> buzones, GeneracionArchivoBancoResult resultado, string tipoAcreditacion)
        {
            int totalDepositos = buzones.Sum(b => b.Depositos?.Count ?? 0);
            decimal totalMonto = buzones.Sum(b =>
                (decimal)(b.Depositos?.Sum(d => d.Totales?.Sum(t => t.ImporteTotal) ?? 0) ?? 0));
            ServicioLog.instancia.WriteWarning(
                $"TIME OUT Santander: se registra en auditoría y también se inserta en tabla principal por posible procesamiento bancario. | " +
                $"Banco: Santander | Tipo: {tipoAcreditacion} | " +
                $"NombreArchivoOrigen: {resultado.NombreArchivoOriginalParaAuditoria ?? "N/A"} | " +
                $"Buzones: {buzones.Count} | Depósitos: {totalDepositos} | " +
                $"Monto aprox.: {ServicioLog.instancia.FormatearMontoPublico(totalMonto)}",
                "ServicioCuentaBuzon | TIME OUT Santander");
        }

        /// <summary>
        /// Maneja el caso TIME OUT Santander: primero inserta en tabla principal (crítico),
        /// luego intenta registrar en AcreditacionesConError (no crítico — fallo logueado sin relanzar).
        /// La inserción en tabla principal es prioritaria porque previene reprocesamientos de IDOPERACION
        /// que Santander puede haber procesado aunque nuestro sistema no recibió confirmación.
        /// </summary>
        private async Task ManejarTimeoutSantander(
            List<CuentaBuzon> buzones,
            GeneracionArchivoBancoResult resultado,
            string tipoAcreditacion,
            Func<Task> insertarEnPrincipal)
        {
            LogearTimeoutSantander(buzones, resultado, tipoAcreditacion);
            await insertarEnPrincipal();
            try
            {
                await ServicioAcreditacion.getInstancia().RegistrarSantanderPendienteAuditoriaPorFalloEnvioWs(
                    buzones,
                    resultado.EstadoEnvioWsParaAuditoria ?? "FALLIDO_WS",
                    resultado.ObservacionParaAuditoria ?? resultado.Motivo,
                    resultado.NombreArchivoOriginalParaAuditoria);
            }
            catch (Exception ex)
            {
                ServicioLog.instancia.WriteError(
                    $"[CRÍTICO] TIME OUT Santander: falló el registro en AcreditacionesConError luego de insertar en tabla principal. " +
                    $"Tipo: {tipoAcreditacion} | NombreArchivoOrigen: {resultado.NombreArchivoOriginalParaAuditoria ?? "N/A"} | " +
                    $"Error: {ex.Message}",
                    "ServicioCuentaBuzon | ManejarTimeoutSantander");
            }
        }

        #region MÉTODOS ACREDITAR POR CONFIGURACIÓN!
        public async Task acreditarPuntoAPuntoPorBanco(Banco bank)
        {
            int ultIdOperacionPorBuzon = 0;

            List<CuentaBuzon> buzones = getAllByTipoAcreditacionYBanco(VariablesGlobales.p2p, bank);

            if (buzones == null || buzones.Count == 0)
                throw new Exception("No se encontraron buzones punto a punto");

            foreach (CuentaBuzon unBuzon in buzones)
            {

                ultIdOperacionPorBuzon = await obtenerUltimaOperacionByNC(unBuzon);

                try
                {
                    if (ultIdOperacionPorBuzon > 0)
                    {

                        TimeSpan timeSpanDelBuzon = unBuzon.Cierre.HasValue ? unBuzon.Cierre.Value.TimeOfDay : TimeSpan.Zero;

                        await ServicioDeposito.getInstancia()
                                .asignarDepositosAlBuzon(unBuzon, ultIdOperacionPorBuzon, timeSpanDelBuzon);

                    }
                }

                catch (Exception ex)
                {

                    Console.WriteLine($"Error asignando depósitos para buzón {unBuzon.NC}: {ex.Message}");

                }

            }

            var resultadoGen = await generarArchivoPorBanco(buzones, bank, VariablesGlobales.p2p);
            if (resultadoGen.RequiereAuditoriaEnvioFallido)
            {
                if (resultadoGen.EsTimeoutWs)
                {
                    await ManejarTimeoutSantander(buzones, resultadoGen, VariablesGlobales.p2p,
                        () => ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzones(buzones));
                    return;
                }
                await ServicioAcreditacion.getInstancia().RegistrarSantanderPendienteAuditoriaPorFalloEnvioWs(
                    buzones,
                    resultadoGen.EstadoEnvioWsParaAuditoria ?? "FALLIDO_WS",
                    resultadoGen.ObservacionParaAuditoria ?? resultadoGen.Motivo,
                    resultadoGen.NombreArchivoOriginalParaAuditoria);
                return;
            }

            await ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzones(buzones);

        }
        public async Task acreditarDiaADiaPorBanco(Banco banco)
        {
            TimeSpan horaCierre = TimeSpan.Zero;

            List<CuentaBuzon> cuentaBuzones = getAllByTipoAcreditacionYBanco(VariablesGlobales.diaxdia, banco);

            List<CuentaBuzon> cuentasBuzonesConDepositos = new List<CuentaBuzon>();

            try
            {
                if (cuentaBuzones != null && cuentaBuzones.Count > 0)
                {
                    foreach (var unaCuentaBuzon in cuentaBuzones)
                    {
                 
                        int ultIdOperacion = await obtenerUltimaOperacionByNC(unaCuentaBuzon);

                        if (ultIdOperacion > 0)
                        {

                            if (unaCuentaBuzon.Cierre.HasValue)
                            {
                                horaCierre = unaCuentaBuzon.Cierre.Value.TimeOfDay;
                            }

                            await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(unaCuentaBuzon, ultIdOperacion, horaCierre);

                            if (unaCuentaBuzon.Depositos.Count > 0)
                            {
                                cuentasBuzonesConDepositos.Add(unaCuentaBuzon);
                            }
                        }
                    }

                    if (cuentasBuzonesConDepositos.Count > 0)
                    {
                        var resultadoGen = await generarArchivoPorBanco(cuentasBuzonesConDepositos, banco, VariablesGlobales.diaxdia);
                        if (resultadoGen.RequiereAuditoriaEnvioFallido)
                        {
                            if (resultadoGen.EsTimeoutWs)
                            {
                                await ManejarTimeoutSantander(cuentasBuzonesConDepositos, resultadoGen, VariablesGlobales.diaxdia,
                                    () => ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzones(cuentasBuzonesConDepositos));
                                return;
                            }
                            await ServicioAcreditacion.getInstancia().RegistrarSantanderPendienteAuditoriaPorFalloEnvioWs(
                                cuentasBuzonesConDepositos,
                                resultadoGen.EstadoEnvioWsParaAuditoria ?? "FALLIDO_WS",
                                resultadoGen.ObservacionParaAuditoria ?? resultadoGen.Motivo,
                                resultadoGen.NombreArchivoOriginalParaAuditoria);
                            return;
                        }

                        await ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzones(cuentasBuzonesConDepositos);
                    }


                    return;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            throw new Exception("No se encontaron buzones día a día");
        }
        public async Task acreditarTandaPorBanco(Banco bank)
        {
            List<CuentaBuzon> cuentaBuzones = getAllByTipoAcreditacionYBanco(VariablesGlobales.tanda, bank);

            if (cuentaBuzones != null && cuentaBuzones.Count > 0)
            {
                foreach (var unaCuentaBuzon in cuentaBuzones)
                {

                    int ultIdOperacion = await obtenerUltimaOperacionByNC(unaCuentaBuzon);

                    if (ultIdOperacion > 0)
                    {

                        await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(unaCuentaBuzon, ultIdOperacion, TimeSpan.Zero);
                    }

                    else return; //abandona. no genera nada.

                    var resultadoGenTanda = await generarArchivoPorBanco(cuentaBuzones, bank, VariablesGlobales.tanda);
                    if (resultadoGenTanda.RequiereAuditoriaEnvioFallido)
                    {
                        if (resultadoGenTanda.EsTimeoutWs)
                        {
                            await ManejarTimeoutSantander(cuentaBuzones, resultadoGenTanda, VariablesGlobales.tanda,
                                () => ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzones(cuentaBuzones));
                            return;
                        }
                        await ServicioAcreditacion.getInstancia().RegistrarSantanderPendienteAuditoriaPorFalloEnvioWs(
                            cuentaBuzones,
                            resultadoGenTanda.EstadoEnvioWsParaAuditoria ?? "FALLIDO_WS",
                            resultadoGenTanda.ObservacionParaAuditoria ?? resultadoGenTanda.Motivo,
                            resultadoGenTanda.NombreArchivoOriginalParaAuditoria);
                        return;
                    }

                    //luego de generar, tiene que insertar en ACREDITACIONESDEPOSITO los depositos que fueron insertados ok

                    return;
                }
            }
            throw new Exception("No se encontaron buzones tanda");
        }
        #endregion
        private async Task<int> obtenerUltimaOperacionByNC(CuentaBuzon cuenta)
        {

            int idmoneda = cuenta.getIdMoneda();

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                // ✅ Usar TableNameResolver para obtener nombre de tabla según RuntimeMode
                var tableName = TableNameResolver.AcreditacionDeposito;
                TableNameResolver.ValidateTableName(tableName, "ServicioCuentaBuzon.getUltimaOperacionAcreditada");
                string query = $"select max(idoperacion) from {tableName} where IDBUZON = @ncFound and IDCUENTA = @idCuenta and MONEDA = @idMoneda";

                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ncFound", cuenta.NC);
                    cmd.Parameters.AddWithValue("@idCuenta", cuenta.IdCuenta);
                    cmd.Parameters.AddWithValue("@idMoneda", idmoneda);

                    object result = await cmd.ExecuteScalarAsync();
                    return result != DBNull.Value && result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }
        public async Task acreditarTandaHendersonSantander(TimeSpan horaCierreActual, int numTanda)
        {
            if (horaCierreActual == TimeSpan.Zero)
            {
                throw new Exception("Error en acreditarTandaHendersonSantander: La hora de cierre actual no puede ser cero.");
            }
            Banco santander = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

            Cliente henderson = ServicioCliente.getInstancia().getById(164);

            ConfiguracionAcreditacion config = new ConfiguracionAcreditacion(VariablesGlobales.tanda);
            try
            {
                List<CuentaBuzon> cuentas = await getCuentasPorClienteBancoYTipoAcreditacion(henderson.IdCliente, santander, config);

                foreach (CuentaBuzon acc in cuentas)
                {

                    if (acc.NC == "EA24L1010N07000720")
                    {
                        Console.WriteLine("ES TAPES FARMACIA");
                    }
                    int ultimoIdOperacion = await obtenerUltimaOperacionByNC(acc);

                    if (ultimoIdOperacion > 0)
                    {
                        await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(acc, ultimoIdOperacion, horaCierreActual);
                    }

                }

                if (cuentas.Count > 0)
                {
                    {
                        var resultadoGen = await generarArchivoPorBanco(cuentas, santander, VariablesGlobales.tanda);
                        if (resultadoGen.RequiereAuditoriaEnvioFallido)
                        {
                            if (resultadoGen.EsTimeoutWs)
                            {
                                await ManejarTimeoutSantander(cuentas, resultadoGen, VariablesGlobales.tanda,
                                    () => ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzonesTanda(cuentas, numTanda));
                                return;
                            }
                            await ServicioAcreditacion.getInstancia().RegistrarSantanderPendienteAuditoriaPorFalloEnvioWs(
                                cuentas,
                                resultadoGen.EstadoEnvioWsParaAuditoria ?? "FALLIDO_WS",
                                resultadoGen.ObservacionParaAuditoria ?? resultadoGen.Motivo,
                                resultadoGen.NombreArchivoOriginalParaAuditoria);
                            return;
                        }

                        await ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzonesTanda(cuentas, numTanda);
                    }
                }
            }

            catch (Exception e)
            {
                throw e;
            }

        }
        public async Task acreditarTandaHendersonScotiabank(TimeSpan horaCierreActual, int numTanda)
        {
            if (horaCierreActual == TimeSpan.Zero)
            {
                throw new Exception("Error en acreditarTandaHendersonSantander: La hora de cierre actual no puede ser cero.");
            }
            Banco scotia = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.scotiabank);

            Cliente henderson = ServicioCliente.getInstancia().getById(164);

            ConfiguracionAcreditacion config = new ConfiguracionAcreditacion(VariablesGlobales.tanda);

            List<CuentaBuzon> cuentasConDepositos = new List<CuentaBuzon>();
            try
            {
                List<CuentaBuzon> cuentas = await getCuentasPorClienteBancoTipoAcreditacionYTanda(henderson.IdCliente, scotia, config, numTanda);

                foreach (CuentaBuzon acc in cuentas)
                {
                    int ultimoIdOperacion = await obtenerUltimaOperacionByNC(acc);

                    if (ultimoIdOperacion > 0)
                    {
                        await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(acc, ultimoIdOperacion, horaCierreActual);
                        if (acc.Depositos.Count > 0)
                        {
                            cuentasConDepositos.Add(acc);
                        }
                    }
                }
                if (cuentasConDepositos.Count > 0)
                {
                    {
                        // Pasar el tipo específico de acreditación según el número de tanda
                        string tipoAcreditacionEspecifico = numTanda == 1 ? "Tanda1" : "Tanda2";
                        var resultadoGenScotia = await generarArchivoPorBanco(cuentasConDepositos, scotia, tipoAcreditacionEspecifico);
                        if (resultadoGenScotia.RequiereAuditoriaEnvioFallido)
                        {
                            await ServicioAcreditacion.getInstancia().RegistrarSantanderPendienteAuditoriaPorFalloEnvioWs(
                                cuentasConDepositos,
                                resultadoGenScotia.EstadoEnvioWsParaAuditoria ?? "FALLIDO_WS",
                                resultadoGenScotia.ObservacionParaAuditoria ?? resultadoGenScotia.Motivo,
                                resultadoGenScotia.NombreArchivoOriginalParaAuditoria);
                            return;
                        }

                        await ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzonesTanda(cuentasConDepositos, numTanda);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public async Task acretidarPorBanco(Banco bank, TimeSpan horaCierre)
        {

            //Este metodo por lo general acredita a la hora del cierre del banco parámetro.

            List<CuentaBuzon> buzonesPorBanco = getAllByBanco(bank);

            if (buzonesPorBanco != null && buzonesPorBanco.Count > 0)
            {

                foreach (CuentaBuzon _buzon in buzonesPorBanco)
                {
                    int ultIdOperacion = await obtenerUltimaOperacionByNC(_buzon);

                    if (ultIdOperacion > 0)
                    {
                        await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(_buzon, ultIdOperacion, horaCierre);
                    }
                }

                var resultadoGenAcre = await generarArchivoPorBanco(buzonesPorBanco, bank, VariablesGlobales.diaxdia);
                if (resultadoGenAcre.RequiereAuditoriaEnvioFallido)
                {
                    if (resultadoGenAcre.EsTimeoutWs)
                    {
                        await ManejarTimeoutSantander(buzonesPorBanco, resultadoGenAcre, VariablesGlobales.diaxdia,
                            () => ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzones(buzonesPorBanco));
                    }
                    else
                    {
                        await ServicioAcreditacion.getInstancia().RegistrarSantanderPendienteAuditoriaPorFalloEnvioWs(
                            buzonesPorBanco,
                            resultadoGenAcre.EstadoEnvioWsParaAuditoria ?? "FALLIDO_WS",
                            resultadoGenAcre.ObservacionParaAuditoria ?? resultadoGenAcre.Motivo,
                            resultadoGenAcre.NombreArchivoOriginalParaAuditoria);
                    }
                }

                return;
            }

            throw new Exception("No se encontaron buzones para el banco : " + bank);

        }
        public async Task acreditarDiaADiaPorCliente(Cliente cli, Banco bank, TimeSpan horaCierreActual)
        {

            if (cli == null)
            {
                throw new Exception("Error en método acreditarDiaADiaPorCliente. Cliente null");
            }

            List<CuentaBuzon> cuentaBuzones = await getCuentaBuzonesByClienteYBanco(cli.IdCliente, bank);


            if (cuentaBuzones != null && cuentaBuzones.Count > 0)
            {

                foreach (CuentaBuzon cu in cuentaBuzones)
                {

                    int ultIdOperacion = await obtenerUltimaOperacionByNC(cu);

                    if (ultIdOperacion > 0)
                    {


                        // ✅ Usar la hora de cierre del buzón individual si horaCierreActual es TimeSpan.Zero
                        // Si el buzón tiene hora de cierre, usarla; si no, usar horaCierreActual como fallback


                        //APLICAR SOLO PARA NIKE DE PRUEBA, SI ANDA BIEN MAÑANA MARTES 23 DEE DICIEMBRE,ENTONCES APLICAR PARA TODOS.
                        if (cli.IdCliente == 998)
                        {
                            TimeSpan horaCierreAUsar = horaCierreActual;

                            if (horaCierreActual == TimeSpan.Zero && cu.Cierre.HasValue)
                            {
                                horaCierreAUsar = cu.Cierre.Value.TimeOfDay;
                            }

                            await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(cu, ultIdOperacion, horaCierreAUsar);
                        }
                        else
                        {
                            await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(cu, ultIdOperacion, horaCierreActual);
                        }

                        //eSTO ESTUVO HASTA EL 22 DE DICIEMBRE, NO TOMA LA HORA DE CIERRE DE LOS BUZONES QUE SON ACREDITADOS POR CLIENTE ESPECIFICO Y BANCO.
                        //await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(cu, ultIdOperacion, horaCierreActual);

                    }
                }

                var resultadoGen = await generarArchivoPorBanco(cuentaBuzones, bank, VariablesGlobales.tanda);
                if (resultadoGen.RequiereAuditoriaEnvioFallido)
                {
                    if (resultadoGen.EsTimeoutWs)
                    {
                        await ManejarTimeoutSantander(cuentaBuzones, resultadoGen, VariablesGlobales.tanda,
                            () => ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzones(cuentaBuzones));
                        return;
                    }
                    await ServicioAcreditacion.getInstancia().RegistrarSantanderPendienteAuditoriaPorFalloEnvioWs(
                        cuentaBuzones,
                        resultadoGen.EstadoEnvioWsParaAuditoria ?? "FALLIDO_WS",
                        resultadoGen.ObservacionParaAuditoria ?? resultadoGen.Motivo,
                        resultadoGen.NombreArchivoOriginalParaAuditoria);
                    return;
                }

                await ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzones(cuentaBuzones);

            }

        }
        //Enviar Excel genérico.
        public async Task enviarExcel(TimeSpan desde, TimeSpan hasta, Cliente cli, Banco bank)
        {


        }
        //Enviar Excel Específico para Henderson. (07:10)T1 (14:35)T2
        public async Task enviarExcelFormatoTanda(TimeSpan desde, TimeSpan hasta, Cliente cli, Banco bank, string city, int numTanda, string tarea, DateTime? diaOperativo = null)
        {
            try
            {
                var baseDia = (diaOperativo ?? DateTime.Today).Date;

                DateTime fechaDesde = baseDia.Add(desde);

                DateTime fechaHasta = baseDia.Add(hasta);

                ConfiguracionAcreditacion config = new ConfiguracionAcreditacion();
                //Si es SANTANDER ES CONFIG TANDA

                //Si es BBVA es config DIA A DIA
                if (bank.NombreBanco.ToUpper() == VariablesGlobales.santander.ToUpper() || bank.NombreBanco.ToUpper() == VariablesGlobales.scotiabank.ToUpper())
                {
                    config = new ConfiguracionAcreditacion(VariablesGlobales.tanda);
                }

                if (bank.NombreBanco.ToUpper() == VariablesGlobales.bbva.ToUpper())
                {
                    config = new ConfiguracionAcreditacion(VariablesGlobales.diaxdia);
                }

                List<DtoAcreditacionesPorEmpresa> acreditacionesFound = await ServicioAcreditacion.getInstancia().getAcreditacionesByFechaBancoClienteYTipoAcreditacion(fechaDesde, fechaHasta, cli, bank, config).ConfigureAwait(false);


                if (bank.NombreBanco.ToUpper() == VariablesGlobales.santander.ToUpper())
                {
                    bool esB2B = tarea.Contains("B2B", StringComparison.OrdinalIgnoreCase);

                    if (esB2B)
                        acreditacionesFound = acreditacionesFound.Where(a => a.Empresa.ToUpper().Contains("B2B")).ToList();
                    else
                        acreditacionesFound = acreditacionesFound.Where(a => !a.Empresa.ToUpper().Contains("B2B")).ToList();
                }


                // Bloque exclusivo BBVA idcliente 242: split en dos envíos (TATA y BAS)
                if (bank.NombreBanco.ToUpper() == VariablesGlobales.bbva.ToUpper() && cli.IdCliente == 242)
                {
                    // Blindar exclusión de FRONTOY (97) y SANROQUE (262) aunque el SQL ya los filtra
                    var acreditacionesFiltradas = acreditacionesFound
                        .Where(x => x.IdCliente != 97 && x.IdCliente != 262)
                        .ToList();

                    // Partición: BAS = empresa contiene "BAS"; TATA = el resto
                    var grupoTata = acreditacionesFiltradas
                        .Where(x => !x.Empresa.ToUpperInvariant().Contains("BAS"))
                        .ToList();
                    var grupoBas = acreditacionesFiltradas
                        .Where(x => x.Empresa.ToUpperInvariant().Contains("BAS"))
                        .ToList();

                    if (grupoTata.Any())
                    {
                        var mvdTata = grupoTata.Where(x => x.Ciudad.ToUpper() == "MONTEVIDEO").ToList();
                        var mldTata = grupoTata.Where(x => x.Ciudad.ToUpper() == "MALDONADO").ToList();
                        generarExcelFormatoTanda(mvdTata, mldTata, numTanda, bank, cli, config, tarea, baseDia, etiqueta: "TATA");
                    }

                    if (grupoBas.Any())
                    {
                        var mvdBas = grupoBas.Where(x => x.Ciudad.ToUpper() == "MONTEVIDEO").ToList();
                        var mldBas = grupoBas.Where(x => x.Ciudad.ToUpper() == "MALDONADO").ToList();
                        // Reutiliza la misma tarea (mismos destinatarios que TATA); solo cambia la etiqueta en filename/asunto
                        generarExcelFormatoTanda(mvdBas, mldBas, numTanda, bank, cli, config, tarea, baseDia, etiqueta: "BAS");
                    }

                    return;
                }

                List<DtoAcreditacionesPorEmpresa> listaMontevideo = acreditacionesFound.Where(x => x.Ciudad.ToUpper() == "MONTEVIDEO").ToList();
                List<DtoAcreditacionesPorEmpresa> listaMaldonado = acreditacionesFound.Where(x => x.Ciudad.ToUpper() == "MALDONADO").ToList();

                generarExcelFormatoTanda(listaMontevideo, listaMaldonado, numTanda, bank, cli, config, tarea, baseDia);

            }

            catch (Exception e)
            {
                throw e;
            }

        }

        private void generarExcelFormatoTanda(List<DtoAcreditacionesPorEmpresa> acreditacionesMontevideo, List<DtoAcreditacionesPorEmpresa> acreditacionesMaldonado, int numTanda, Banco b, Cliente c, ConfiguracionAcreditacion config, string tarea, DateTime? diaOperativo = null, string etiqueta = null)
        {
            // Colores para el formato
            var pastelYellow = XLColor.LightYellow;
            var pastelCyan = XLColor.LightCyan;
            var fechaReferencia = (diaOperativo ?? DateTime.Today).Date;

            void InsertarLogoDesdeRecurso(IXLWorksheet ws, ref int row, string etq = null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourcePath = "ANS.Images.logoTecniFinal.png";

                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream != null)
                    {
                        ws.AddPicture(stream)
                          .MoveTo(ws.Cell(row, 3), 30, 5)
                          .WithPlacement(XLPicturePlacement.FreeFloating)
                          .Scale(0.5);
                        row += 2; // espacio debajo del logo
                    }
                }

                // Fecha
                ws.Range(row, 1, row, 5).Merge();
                var celdaFecha = ws.Cell(row, 1);
                celdaFecha.Value = $"{b.NombreBanco} - Tanda {numTanda} - {fechaReferencia:dd/MM/yyyy}";
                if (b.NombreBanco.ToUpper() == VariablesGlobales.bbva.ToUpper())
                {
                    celdaFecha.Value = $"{b.NombreBanco}  - {fechaReferencia:dd/MM/yyyy}";
                }
                if (c.IdCliente == 40)
                {
                    celdaFecha.Value = $"{b.NombreBanco} - Reporte CASH  - {fechaReferencia:dd/MM/yyyy}";
                }
                celdaFecha.Style.Font.Italic = true;
                celdaFecha.Style.Font.FontSize = 11;
                celdaFecha.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                celdaFecha.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row++; // una fila en blanco

                // Título
                ws.Range(row, 1, row, 5).Merge();
                var celdaTitulo = ws.Cell(row, 1);
                celdaTitulo.Value = string.IsNullOrWhiteSpace(etq)
                    ? "Buzones Inteligentes"
                    : $"Buzones Inteligentes  -  SOLO {etq.ToUpperInvariant()}";
                celdaTitulo.Style.Font.Bold = true;
                celdaTitulo.Style.Font.FontSize = 16;
                celdaTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                celdaTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row++; // una fila en blanco tras título
            }

            void GenerateExcel(List<DtoAcreditacionesPorEmpresa> lista, string ciudad)
            {
                var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add(ciudad);
                ws.ShowGridLines = false;

                int row = 1;
                string tituloEtq = string.IsNullOrWhiteSpace(etiqueta) ? null : etiqueta.ToUpperInvariant();
                InsertarLogoDesdeRecurso(ws, ref row, tituloEtq);

                int freezeRow = 0;

                // Sección PESOS
                var listaPesos = lista.Where(x => x.Moneda.ToUpper().Contains("PESO") || x.Moneda.ToUpper().Contains("UYU")).ToList();
                if (listaPesos.Any())
                {
                    int hdr = row;
                    ws.Cell(hdr, 1).Value = "BUZON";
                    ws.Cell(hdr, 2).Value = "SUCURSAL";
                    ws.Cell(hdr, 3).Value = "CUENTA";
                    ws.Cell(hdr, 4).Value = "MONEDA";
                    ws.Cell(hdr, 5).Value = "MONTO";
                    ws.Range(hdr, 1, hdr, 5).Style
                        .Font.Bold = true;
                    ws.Range(hdr, 1, hdr, 5)
                        .Style.Fill.BackgroundColor = pastelYellow;

                    row++;
                    int dataStart = row;
                    foreach (var i in listaPesos)
                    {
                        ws.Cell(row, 1).Value = i.NN;
                        ws.Cell(row, 2).Value = i.Sucursal;
                        ws.Cell(row, 3).Value = i.NumeroCuenta;
                        ws.Cell(row, 4).Value = i.Moneda;
                        ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(i.Monto);
                        row++;
                    }
                    int lastData = row - 1;

                    // Total
                    double totP = listaPesos.Sum(x => x.Monto);
                    ws.Cell(row, 4).Value = "TOTAL PESOS:";
                    ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(totP);
                    ws.Range(row, 4, row, 5).Style
                        .Fill.BackgroundColor = pastelCyan;
                    ws.Range(row, 4, row, 5).Style.Font.Bold = true;
                    row++; // una fila en blanco entre secciones

                    // Tabla
                    var tblP = ws.Range(hdr, 1, lastData, 5).CreateTable();
                    tblP.Theme = XLTableTheme.TableStyleMedium9;
                    tblP.ShowAutoFilter = false;
                    tblP.ShowHeaderRow = true;

                    freezeRow = hdr;
                }

                // Sección DÓLARES
                var listaDol = lista.Where(x => x.Moneda.ToUpper().Contains("DOLAR") || x.Moneda.ToUpper().Contains("USD")).ToList();
                if (listaDol.Any())
                {
                    int hdr2 = row;
                    ws.Cell(hdr2, 1).Value = "BUZON";
                    ws.Cell(hdr2, 2).Value = "SUCURSAL";
                    ws.Cell(hdr2, 3).Value = "CUENTA";
                    ws.Cell(hdr2, 4).Value = "MONEDA";
                    ws.Cell(hdr2, 5).Value = "MONTO";
                    ws.Range(hdr2, 1, hdr2, 5).Style.Font.Bold = true;
                    ws.Range(hdr2, 1, hdr2, 5).Style.Fill.BackgroundColor = pastelYellow;

                    row++;
                    int data2 = row;
                    foreach (var i in listaDol)
                    {
                        ws.Cell(row, 1).Value = i.NN;
                        ws.Cell(row, 2).Value = i.Sucursal;
                        ws.Cell(row, 3).Value = i.NumeroCuenta;
                        ws.Cell(row, 4).Value = i.Moneda;
                        ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(i.Monto);
                        row++;
                    }
                    int last2 = row - 1;

                    // Total Dólares
                    double totD = listaDol.Sum(x => x.Monto);
                    ws.Cell(row, 4).Value = "TOTAL DOLARES:";
                    ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(totD);
                    ws.Range(row, 4, row, 5).Style.Fill.BackgroundColor = pastelCyan;
                    ws.Range(row, 4, row, 5).Style.Font.Bold = true;
                    row++;

                    var tblD = ws.Range(hdr2, 1, last2, 5).CreateTable();
                    tblD.Theme = XLTableTheme.TableStyleMedium9;
                    tblD.ShowAutoFilter = false;
                    tblD.ShowHeaderRow = true;

                    if (freezeRow == 0) freezeRow = hdr2;
                }

                ws.Columns(1, 5).AdjustToContents();                       // Ajusta solo A–E
                ws.Columns(6, ws.ColumnCount()).Hide();                     // Oculta de F en adelante

                // Si además quieres que al imprimir no salga nada de F hacia la derecha:
                var lastRow = ws.LastRowUsed().RowNumber();
                ws.PageSetup.PrintAreas.Add($"A1:E{lastRow}");
                ws.PageSetup.FitToPages(1, 0);

                if (freezeRow > 0)
                    ws.SheetView.FreezeRows(freezeRow);

                ws.PageSetup.Margins.Left = 0.3;
                ws.PageSetup.Margins.Right = 0.3;
                ws.PageSetup.CenterHorizontally = true;
                ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;

                // Guardar y enviar
                string fn;
                if (b.NombreBanco.Equals(VariablesGlobales.santander, StringComparison.OrdinalIgnoreCase)
                 || b.NombreBanco.Equals(VariablesGlobales.scotiabank, StringComparison.OrdinalIgnoreCase))
                {
                    fn = $"{b.NombreBanco}_Henderson_{ciudad}_Tanda_{numTanda}_{fechaReferencia:yyyyMMdd}_{DateTime.Now:HHmmss}.xlsx";
                    if (tarea.Contains("B2B", StringComparison.OrdinalIgnoreCase))
                    {
                        fn = $"{b.NombreBanco}_B2B_{ciudad}_Tanda_{numTanda}_{fechaReferencia:yyyyMMdd}_{DateTime.Now:HHmmss}.xlsx";
                    }
                }

                else if (b.NombreBanco.Equals(VariablesGlobales.bbva, StringComparison.OrdinalIgnoreCase))
                {
                    string etq = string.IsNullOrWhiteSpace(etiqueta) ? "TATA" : etiqueta.ToUpperInvariant();
                    fn = $"{b.NombreBanco}_{ciudad}_{etq}_{numTanda}_{fechaReferencia:yyyyMMdd}_{DateTime.Now:HHmmss}.xlsx";
                }

                else
                    fn = $"{b.NombreBanco}_{ciudad}_Tanda_{numTanda}_{fechaReferencia:yyyyMMdd}_{DateTime.Now:HHmmss}.xlsx";

                if (c.IdCliente == 40)
                {
                    fn = $"{b.NombreBanco}_CASH_{ciudad}{numTanda}_{fechaReferencia:yyyyMMdd}_{DateTime.Now:HHmmss}.xlsx";
                }

                // ✅ Ruta desde configuración (resuelve según RuntimeMode)
                string path = Path.Combine(ConfiguracionGlobal.Rutas.BaseExcel, fn);

                // Aplicar guardia y prefijo TEST_
                path = AplicarGuardiaYPrefijoRuta(path, "ServicioCuentaBuzon | enviarExcelFormatoTanda");

                wb.SaveAs(path);

                Console.WriteLine($"Excel generado para {ciudad}: {path}");
                try
                {
                    if (b is null) throw new ArgumentNullException(nameof(b));
                    if (string.IsNullOrWhiteSpace(b.NombreBanco))
                        throw new ArgumentException("NombreBanco no puede ser nulo o vacío.", nameof(b));

                    string bank = b.NombreBanco.Trim();
                    string cityUp = (ciudad ?? string.Empty).ToUpperInvariant();

                    string asu;
                    string cue;

                    // 1) Caso especial: Scotiabank CASH (debe evaluarse primero)
                    if (bank.Equals(VariablesGlobales.scotiabank, StringComparison.OrdinalIgnoreCase) &&
                        c?.IdCliente == 40)
                    {
                        asu = $"Acreditaciones {bank} (CASH) - {cityUp}";
                        cue = $"Adjunto archivo de acreditaciones para {bank} (CASH) - {cityUp}.";
                    }
                    // 2) Santander o Scotiabank (HENDERSON con Tanda)
                    else if (bank.Equals(VariablesGlobales.santander, StringComparison.OrdinalIgnoreCase) ||
                             bank.Equals(VariablesGlobales.scotiabank, StringComparison.OrdinalIgnoreCase))
                    {
                        asu = $"Acreditaciones {bank} (HENDERSON) Tanda {numTanda} - {cityUp}";
                        cue = $"Adjunto archivo de acreditaciones para {bank} (HENDERSON) Tanda {numTanda} ciudad {cityUp}.";

                        if (tarea.Contains("B2B", StringComparison.OrdinalIgnoreCase))
                        {
                            asu = $"Acreditaciones {bank} (B2B) Tanda {numTanda} - {cityUp}";
                            cue = $"Adjunto archivo de acreditaciones para {bank} (B2B) Tanda {numTanda} ciudad {cityUp}.";
                        }
                    }
                    // 3) BBVA
                    else if (bank.Equals(VariablesGlobales.bbva, StringComparison.OrdinalIgnoreCase))
                    {
                        string etq = string.IsNullOrWhiteSpace(etiqueta) ? "TATA" : etiqueta.ToUpperInvariant();
                        asu = $"Acreditaciones {bank} ({etq}) - {cityUp}";
                        cue = $"Adjunto archivo de acreditaciones para buzones {etq} ciudad {cityUp} banco {bank}.";
                    }
                    // 4) Default (fallback para otros bancos)
                    else
                    {
                        asu = $"Acreditaciones {bank} - {cityUp}";
                        cue = $"Adjunto archivo de acreditaciones para el banco {bank} en {cityUp}.";
                    }

                    _emailService.enviarExcelPorMail(path, asu, cue, c, b, tarea, ciudad);
                }
                catch (Exception ex)
                {
                    // Manejo/log según tu estándar
                    throw;
                }
            }

            if (acreditacionesMontevideo?.Any() == true)
                GenerateExcel(acreditacionesMontevideo, "MONTEVIDEO");
            if (acreditacionesMaldonado?.Any() == true)
                GenerateExcel(acreditacionesMaldonado, "MALDONADO");

        }
        public Task checkUltimaConexionByIdBuzon(string nc)
        {

            if (nc == null)
            {
                throw new Exception("Error en checkUltimaConexionByIdBuzon : 'nc' vacío.");
            }
            return null;
        }
        public async Task enviarExcelTesoreria(Banco banco, string city, int numTanda, TimeSpan desde, TimeSpan hasta, string tarea, DateTime? dia)
        {
            var baseDia = (dia ?? DateTime.Today).Date;
            DateTime fechaDesde = baseDia.Add(desde);
            DateTime fechaHasta = baseDia.Add(hasta);

            try
            {
                List<DtoAcreditacionesPorEmpresa> lista = await ServicioAcreditacion.getInstancia().getAcreditacionesParaExcelTesoreria(banco, numTanda, new ConfiguracionAcreditacion("Tanda"), dia);


                if (dia != null)
                {
                    //filtro la lista por fecha si vino con un valor de fecha filtrada.
                    lista = lista.Where(x => x.Fecha.Date == dia.Value.Date).ToList();
                }

                generarExcelTesoreriaTanda(lista, numTanda, banco, tarea, dia);

            }

            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static void generarExcelTesoreriaTanda(
    List<DtoAcreditacionesPorEmpresa> datos,
    int numTanda,
    Banco banco,
    string tarea,
    DateTime? fechaFiltro)
        {
            var ciudades = new[] { "MONTEVIDEO", "MALDONADO" };

            // fecha base solo con día/mes/año
            var fechaBase = (fechaFiltro?.Date) ?? DateTime.Today;
            var fechaTexto = fechaBase.ToString("dd/MM/yyyy");

            foreach (var ciudad in ciudades)
            {
                var datosCiudad = datos.Where(d => d.Ciudad.Equals(ciudad, StringComparison.OrdinalIgnoreCase));

                var pesos = datosCiudad.Where(d => d.Divisa == 1).OrderBy(d => d.Empresa).ToList();
                var dolares = datosCiudad.Where(d => d.Divisa != 1).OrderBy(d => d.Empresa).ToList();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add(ciudad);
                int row = 1;

                // Logo
                InsertarLogoParaTesoreriaExcel(ws, ref row);

                // Título (fecha sin hora)
                ws.Range(row, 1, row, 6).Merge().Value =
                    $"TECNISEGUR - Excel para tesorería {numTanda} - {ciudad.ToUpper()} - {fechaTexto}";
                var titleCell = ws.Cell(row, 1);
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 14;
                titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row += 2;

                // Encabezados
                var headers = new[] { "Empresa", "Cuenta", "Sucursal", "Moneda", "Ciudad", "TotalMonto" };
                for (int c = 0; c < headers.Length; c++) ws.Cell(row, c + 1).Value = headers[c];
                row++;

                // Filas PESOS
                foreach (var d in pesos)
                {
                    ws.Cell(row, 1).Value = d.Empresa;
                    ws.Cell(row, 2).Value = d.NumeroCuenta;
                    ws.Cell(row, 3).Value = d.Sucursal;
                    ws.Cell(row, 4).Value = d.Divisa;
                    ws.Cell(row, 5).Value = d.Ciudad;
                    ws.Cell(row, 6).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(d.Monto);
                    row++;
                }

                // Total PESOS
                ws.Range(row, 1, row, 5).Merge().Value = "TOTAL PESOS";
                ws.Cell(row, 6).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(pesos.Sum(d => d.Monto));
                ws.Range(row, 1, row, 6).Style.Font.SetBold();
                row += 2;

                // Filas DÓLARES
                foreach (var d in dolares)
                {
                    ws.Cell(row, 1).Value = d.Empresa;
                    ws.Cell(row, 2).Value = d.NumeroCuenta;
                    ws.Cell(row, 3).Value = d.Sucursal;
                    ws.Cell(row, 4).Value = d.Divisa;
                    ws.Cell(row, 5).Value = d.Ciudad;
                    ws.Cell(row, 6).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(d.Monto);
                    row++;
                }

                // Total DÓLARES
                ws.Range(row, 1, row, 5).Merge().Value = "TOTAL DÓLARES";
                ws.Cell(row, 6).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(dolares.Sum(d => d.Monto));
                ws.Range(row, 1, row, 6).Style.Font.SetBold();

                // Ajuste y guardado
                ws.Columns().AdjustToContents();

                // (opcional) usar fechaBase para el nombre de archivo en vez de Now
                var nombreArchivo = $"Resumen_{banco.NombreBanco}_Tanda{numTanda}_{ciudad}_{fechaBase:yyyyMMdd}.xlsx";

                // ✅ Ruta desde configuración (resuelve según RuntimeMode)
                var filePath = Path.Combine(ConfiguracionGlobal.Rutas.BaseExcel, nombreArchivo);
                filePath = AplicarGuardiaYPrefijoRuta(filePath, "ServicioCuentaBuzon | enviarExcelTesoreria");
                wb.SaveAs(filePath);

                // Envío por correo
                try
                {
                    var asuntoBase = $"Acreditaciones TESORERÍA {banco.NombreBanco} Tanda {numTanda} - {ciudad}";
                    // Solo agrego la fecha al asunto si el filtro es HOY (comparando por fecha)
                    var asunto = (fechaFiltro.HasValue && fechaFiltro.Value.Date == DateTime.Today)
                        ? $"{asuntoBase} - {fechaTexto}"
                        : asuntoBase;

                    var cuerpo = $"Adjunto acreditaciones de la tanda {numTanda} para {ciudad}.";
                    ServicioEmail.getInstancia().enviarExcelPorMail(filePath, asunto, cuerpo, null, banco, tarea, ciudad);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al enviar correo para {ciudad}: {ex.Message}");
                }
            }
        }

        public static void generarExcelTesoreriaTanda_BACKUP(
                 List<DtoAcreditacionesPorEmpresa> datos,
                 int numTanda,
                 Banco banco,
                 string tarea,
                 DateTime? fechaFiltro)
        {
            var ciudades = new[] { "MONTEVIDEO", "MALDONADO" };

            foreach (var ciudad in ciudades)
            {
                var datosCiudad = datos
                    .Where(d => d.Ciudad.Equals(ciudad, StringComparison.OrdinalIgnoreCase));

                var pesos = datosCiudad
                    .Where(d => d.Divisa == 1)
                    .OrderBy(d => d.Empresa)
                    .ToList();
                var dolares = datosCiudad
                    .Where(d => d.Divisa != 1)
                    .OrderBy(d => d.Empresa)
                    .ToList();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add(ciudad);
                int row = 1;

                // Insertar logo
                InsertarLogoParaTesoreriaExcel(ws, ref row);

                // Insertar título
                ws.Range(row, 1, row, 6).Merge().Value =
                    $"TECNISEGUR - Excel para tesorería {numTanda} - {ciudad.ToUpper()} - {fechaFiltro}";
                var titleCell = ws.Cell(row, 1);
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 14;
                titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row += 2;

                // Encabezados
                var headers = new[] { "Empresa", "Cuenta", "Sucursal", "Moneda", "Ciudad", "TotalMonto" };
                for (int c = 0; c < headers.Length; c++)
                {
                    ws.Cell(row, c + 1).Value = headers[c];
                }
                row++;

                // Filas de PESOS
                foreach (var d in pesos)
                {
                    ws.Cell(row, 1).Value = d.Empresa;
                    ws.Cell(row, 2).Value = d.NumeroCuenta;
                    ws.Cell(row, 3).Value = d.Sucursal;
                    ws.Cell(row, 4).Value = d.Divisa;
                    ws.Cell(row, 5).Value = d.Ciudad;
                    ws.Cell(row, 6).Value = ServicioUtilidad.getInstancia()
                        .FormatearDoubleConPuntosYComas(d.Monto);
                    row++;
                }

                // Total PESOS
                ws.Range(row, 1, row, 5).Merge().Value = "TOTAL PESOS";
                ws.Cell(row, 6).Value = ServicioUtilidad.getInstancia()
                    .FormatearDoubleConPuntosYComas(pesos.Sum(d => d.Monto));
                ws.Range(row, 1, row, 6).Style.Font.SetBold();
                row += 2;

                // Filas de DÓLARES
                foreach (var d in dolares)
                {
                    ws.Cell(row, 1).Value = d.Empresa;
                    ws.Cell(row, 2).Value = d.NumeroCuenta;
                    ws.Cell(row, 3).Value = d.Sucursal;
                    ws.Cell(row, 4).Value = d.Divisa;
                    ws.Cell(row, 5).Value = d.Ciudad;
                    ws.Cell(row, 6).Value = ServicioUtilidad.getInstancia()
                        .FormatearDoubleConPuntosYComas(d.Monto);
                    row++;
                }

                // Total DÓLARES
                ws.Range(row, 1, row, 5).Merge().Value = "TOTAL DÓLARES";
                ws.Cell(row, 6).Value = ServicioUtilidad.getInstancia()
                    .FormatearDoubleConPuntosYComas(dolares.Sum(d => d.Monto));
                ws.Range(row, 1, row, 6).Style.Font.SetBold();

                // Ajustar columnas y guardar archivo
                ws.Columns().AdjustToContents();
                var nombreArchivo =
                    $"Resumen_{banco.NombreBanco}_Tanda{numTanda}_{ciudad}_{DateTime.Now:yyyyMMdd}.xlsx";

                // ✅ Ruta desde configuración (resuelve según RuntimeMode)
                var filePath = Path.Combine(ConfiguracionGlobal.Rutas.BaseExcel, nombreArchivo);
                filePath = AplicarGuardiaYPrefijoRuta(filePath, "ServicioCuentaBuzon | enviarExcelTesoreria");
                wb.SaveAs(filePath);

                // Envío por correo
                try
                {
                    var asunto = "";
                    if (fechaFiltro != null)
                    {
                        if (fechaFiltro == DateTime.Now.Date)
                        {
                            asunto =
                        $"Acreditaciones TESORERÍA {banco.NombreBanco} Tanda {numTanda} - {ciudad} - {fechaFiltro}";
                        }
                        else
                        {
                            asunto =
                        $"Acreditaciones TESORERÍA {banco.NombreBanco} Tanda {numTanda} - {ciudad}";
                        }
                    }

                    var cuerpo =
                        $"Adjunto acreditaciones de la tanda {numTanda} para {ciudad}.";
                    ServicioEmail.getInstancia().enviarExcelPorMail(
                        filePath, asunto, cuerpo, null, banco,
                        tarea, ciudad);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al enviar correo para {ciudad}: {ex.Message}");
                }
            }
        }


        public async Task enviarExcelDiaADiaPorBanco(Banco banco, ConfiguracionAcreditacion tipoAcreditacion, string tarea, DateTime? diaOperativo = null)
        {

            List<DtoAcreditacionesPorEmpresa> acreditacionesPorBancoYTipoAcreditacion = getAcreditacionesPorBancoYTipoAcreditacion(banco, tipoAcreditacion, diaOperativo);

            // ✅ Agrupar por Empresa, Sucursal, Cuenta, Moneda, Ciudad, NN (buzón) e IdCliente para evitar duplicados
            // El NN (nombre del buzón) es importante para diferenciar buzones distintos de la misma empresa
            // El IdCliente es importante para diferenciar cuando la misma cuenta es usada por diferentes clientes
            // Esto soluciona el problema cuando hay múltiples configuraciones de acreditación para la misma cuenta
            // pero mantiene la diferenciación entre buzones diferentes (NC diferentes) y clientes diferentes
            var acreditacionesAgrupadas = acreditacionesPorBancoYTipoAcreditacion
                .GroupBy(ac => new
                {
                    Empresa = ac.Empresa?.Trim() ?? "",
                    Sucursal = ac.Sucursal?.Trim() ?? "",
                    NumeroCuenta = ac.NumeroCuenta?.Trim() ?? "",
                    Divisa = ac.Divisa,
                    Ciudad = ac.Ciudad?.Trim() ?? "",
                    NN = ac.NN?.Trim() ?? "", // Incluir NN para diferenciar buzones distintos
                    IdCliente = ac.IdCliente // ✅ Incluir IdCliente para diferenciar clientes con la misma cuenta
                })
                .Select(g => new DtoAcreditacionesPorEmpresa
                {
                    Empresa = g.Key.Empresa,
                    Sucursal = g.Key.Sucursal,
                    NumeroCuenta = g.Key.NumeroCuenta,
                    Divisa = g.Key.Divisa,
                    Ciudad = g.Key.Ciudad,
                    NN = g.Key.NN,
                    Monto = g.First().Monto, // Tomar el primer monto para evitar duplicar cuando hay configuraciones duplicadas
                    IdCliente = g.Key.IdCliente // ✅ Usar el IdCliente de la clave de agrupación
                })
                .ToList();

            // Establecer la moneda para cada registro agrupado
            foreach (var ac in acreditacionesAgrupadas)
            {
                ac.setMoneda();
            }

            List<DtoAcreditacionesPorEmpresa> acreditacionesPesosMaldonado = new List<DtoAcreditacionesPorEmpresa>();

            List<DtoAcreditacionesPorEmpresa> acreditacionesDolaresMaldonado = new List<DtoAcreditacionesPorEmpresa>();

            List<DtoAcreditacionesPorEmpresa> acreditacionesPesosMvd = new List<DtoAcreditacionesPorEmpresa>();

            List<DtoAcreditacionesPorEmpresa> acreditacionesDolaresMvd = new List<DtoAcreditacionesPorEmpresa>();

            foreach (DtoAcreditacionesPorEmpresa _ac in acreditacionesAgrupadas)
            {

                if (_ac.Ciudad.ToUpper() == "MONTEVIDEO")
                {
                    if (_ac.Divisa == 1)
                    {
                        acreditacionesPesosMvd.Add(_ac);
                    }
                    else
                    {
                        acreditacionesDolaresMvd.Add(_ac);
                    }
                }
                else if (_ac.Ciudad.ToUpper() == "MALDONADO")
                {
                    if (_ac.Divisa == 1)
                    {
                        acreditacionesPesosMaldonado.Add(_ac);
                    }
                    else
                    {
                        acreditacionesDolaresMaldonado.Add(_ac);
                    }
                }

            }
            if (acreditacionesPesosMvd.Count > 0 || acreditacionesDolaresMvd.Count > 0)
                GenerarExcelFormatoDiaADia("MONTEVIDEO", acreditacionesPesosMvd, acreditacionesDolaresMvd, banco, tarea, diaOperativo);

            if (acreditacionesPesosMaldonado.Count > 0 || acreditacionesDolaresMaldonado.Count > 0)
                GenerarExcelFormatoDiaADia("MALDONADO", acreditacionesPesosMaldonado, acreditacionesDolaresMaldonado, banco, tarea, diaOperativo);

            await Task.CompletedTask;

        }
        private void GenerarExcelFormatoDiaADia(string ciudad, List<DtoAcreditacionesPorEmpresa> listaPesos, List<DtoAcreditacionesPorEmpresa> listaDolares, Banco banco, string tarea, DateTime? fechaFiltro)
        {


            var servicioCliente = ServicioCliente.getInstancia();
            if (servicioCliente.ListaClientes == null || servicioCliente.ListaClientes.Count == 0)
            {
                servicioCliente.getAllClientes();
            }
            var fechaHoy = (fechaFiltro ?? DateTime.Today).ToString("dd - MM - yy");
            var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Acreditaciones");

            int row = 1;
            // Inserta el logo y el título centrado
            InsertarLogoDesdeRecurso(ws, ref row);

            // Información de fecha y ciudad
            ws.Cell(row, 1).Value = "Fecha Acreditación :";
            ws.Cell(row, 2).Value = $"{fechaHoy}-{ciudad}";
            row += 2;

            // --- Tabla PESOS ---
            ws.Cell(row, 1).Value = "CLIENTE";
            ws.Cell(row, 2).Value = "SUC";
            ws.Cell(row, 3).Value = "CUENTA";
            ws.Cell(row, 4).Value = "MONEDA";
            ws.Cell(row, 5).Value = "MONTO";
            // Pinta los títulos con color pastel amarillo claro
            ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF9C4");

            row++;

            double totalPesos = 0;
            foreach (var item in listaPesos)
            {
                string nombreAMostrar;

                // Para BBVA, mostrar directamente la EMPRESA de la tabla CuentasBuzones
                if (banco.NombreBanco.ToUpper() == VariablesGlobales.bbva.ToUpper())
                {
                    // ✅ Mapeo especial: IdCliente 998 se muestra como "TECNISEGUR" en el Excel
                    if (item.IdCliente == 998)
                    {
                        nombreAMostrar = "TECNISEGUR";
                    }
                    else
                    {
                        nombreAMostrar = item.Empresa ?? "";

                        // ✅ Si hay otros registros con la misma empresa pero diferente IdCliente, agregar nombre del cliente entre paréntesis
                        bool hayDuplicados = listaPesos.Any(x =>
                            x != item &&
                            x.Empresa?.Trim() == item.Empresa?.Trim() &&
                            x.IdCliente != item.IdCliente);

                        if (hayDuplicados)
                        {
                            var cliente = ServicioCliente.getInstancia().getById(item.IdCliente);
                            if (cliente != null && !string.IsNullOrWhiteSpace(cliente.Nombre))
                            {
                                nombreAMostrar = $"{nombreAMostrar} ({cliente.Nombre})";
                            }
                        }
                    }
                }
                else
                {
                    // ✅ Patrón universal: Por defecto mostrar NN (nombre del buzón), pero si Empresa es un RUT, buscar el nombre del cliente
                    // Esto aplica para todos los bancos (Scotiabank, Santander, HSBC, ITAU, BANDES, etc.)
                    nombreAMostrar = item.NN ?? ""; // Por defecto: nombre del buzón (NN)

                    // Si Empresa es un RUT (solo dígitos), buscar el nombre del cliente por RUT
                    if (!string.IsNullOrWhiteSpace(item.Empresa) && item.Empresa.All(char.IsDigit))
                    {
                        var cliente = ServicioCliente.getInstancia().getByRut(item.Empresa);
                        nombreAMostrar = cliente?.Nombre ?? item.NN ?? "";
                    }
                }

                ws.Cell(row, 1).Value = nombreAMostrar;
                ws.Cell(row, 2).Value = item.Sucursal;
                ws.Cell(row, 3).Value = item.NumeroCuenta;
                ws.Cell(row, 4).Value = item.Moneda;
                ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(item.Monto);
                totalPesos += item.Monto;
                row++;
            }

            // Fila de Total PESOS con fondo celeste pastel
            ws.Cell(row, 4).Value = "PESOS";
            ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(totalPesos);
            ws.Range(row, 4, row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#B3E5FC");
            row += 2;

            // --- Tabla DÓLARES ---
            ws.Cell(row, 1).Value = "CLIENTE";
            ws.Cell(row, 2).Value = "SUC";
            ws.Cell(row, 3).Value = "CUENTA";
            ws.Cell(row, 4).Value = "MONEDA";
            ws.Cell(row, 5).Value = "MONTO";
            // Pinta los títulos con color pastel amarillo claro
            ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF9C4");
            row++;

            double totalDolares = 0;
            foreach (var item in listaDolares)
            {
                string nombreAMostrar;

                // Para BBVA, mostrar directamente la EMPRESA de la tabla CuentasBuzones
                if (banco.NombreBanco.ToUpper() == VariablesGlobales.bbva.ToUpper())
                {
                    // ✅ Mapeo especial: IdCliente 998 se muestra como "TECNISEGUR" en el Excel
                    if (item.IdCliente == 998)
                    {
                        nombreAMostrar = "TECNISEGUR";
                    }
                    else
                    {
                        nombreAMostrar = item.Empresa ?? "";

                        // ✅ Si hay otros registros con la misma empresa pero diferente IdCliente, agregar nombre del cliente entre paréntesis
                        bool hayDuplicados = listaDolares.Any(x =>
                            x != item &&
                            x.Empresa?.Trim() == item.Empresa?.Trim() &&
                            x.IdCliente != item.IdCliente);

                        if (hayDuplicados)
                        {
                            var cliente = ServicioCliente.getInstancia().getById(item.IdCliente);
                            if (cliente != null && !string.IsNullOrWhiteSpace(cliente.Nombre))
                            {
                                nombreAMostrar = $"{nombreAMostrar} ({cliente.Nombre})";
                            }
                        }
                    }
                }
                else
                {
                    // ✅ Patrón universal: Por defecto mostrar NN (nombre del buzón), pero si Empresa es un RUT, buscar el nombre del cliente
                    // Esto aplica para todos los bancos (Scotiabank, Santander, HSBC, ITAU, BANDES, etc.)
                    nombreAMostrar = item.NN ?? ""; // Por defecto: nombre del buzón (NN)

                    // Si Empresa es un RUT (solo dígitos), buscar el nombre del cliente por RUT
                    if (!string.IsNullOrWhiteSpace(item.Empresa) && item.Empresa.All(char.IsDigit))
                    {
                        var cliente = ServicioCliente.getInstancia().getByRut(item.Empresa);
                        nombreAMostrar = cliente?.Nombre ?? item.NN ?? "";
                    }
                }

                ws.Cell(row, 1).Value = nombreAMostrar;
                ws.Cell(row, 2).Value = item.Sucursal;
                ws.Cell(row, 3).Value = item.NumeroCuenta;
                ws.Cell(row, 4).Value = item.Moneda;
                ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(item.Monto);
                totalDolares += item.Monto;
                row++;
            }

            // Fila de Total DÓLARES con fondo celeste pastel
            ws.Cell(row, 4).Value = "USD";
            ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(totalDolares);
            ws.Range(row, 4, row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#B3E5FC");

            ws.Columns().AdjustToContents();

            var fechaArchivo = (fechaFiltro ?? DateTime.Today).ToString("yyyyMMdd");
            string nombreArchivo = $"AcreditacionesDiaADia_{banco.NombreBanco}_{ciudad}_{fechaArchivo}_{DateTime.Now:HHmmss}.xlsx";

            //produccion:
            // ✅ Ruta desde configuración (resuelve según RuntimeMode)
            string ruta = Path.Combine(ConfiguracionGlobal.Rutas.BaseExcel, nombreArchivo);
            ruta = AplicarGuardiaYPrefijoRuta(ruta, "ServicioCuentaBuzon | enviarExcelDiaADiaPorBanco");

            workbook.SaveAs(ruta);

            try
            {
                _emailService.enviarExcelPorMail(ruta, $"Acreditaciones Día a día - ({banco.NombreBanco}) - " + ciudad.ToUpper(),
                    $"Reporte de las acreditaciones realizadas día a día del banco: {banco.NombreBanco} (" + ciudad.ToUpper() + ")", null, banco, tarea, ciudad);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }
        private List<DtoAcreditacionesPorEmpresa> getAcreditacionesPorBancoYTipoAcreditacion(Banco banco, ConfiguracionAcreditacion tipoAcreditacion, DateTime? diaOperativo = null)
        {

            List<DtoAcreditacionesPorEmpresa> retorno = new List<DtoAcreditacionesPorEmpresa>();
            var baseDia = (diaOperativo ?? DateTime.Today).Date;

            string query = "";
            bool isBBVA = false;

            if (banco.NombreBanco == VariablesGlobales.bbva.ToUpper() && tipoAcreditacion.TipoAcreditacion.ToUpper() == VariablesGlobales.diaxdia.ToUpper())

            {

                // Se supone que se obtiene acreditaciones para generar el excel, y el excel de BBVA tiene que incluir las acreditaciones de los dia a dia y los punto a punto
                // Por lo tanto, la consulta es muy especifica.
                isBBVA = true;

                query = $@"SELECT 
                cb.CUENTA,
                cb.EMPRESA,
                cb.SUCURSAL,
                acc.MONEDA,
                cc.SUCURSAL as CIUDAD, 
                cb.IDCLIENTE as IDCLIENTE,
                SUM(acc.MONTO) AS TotalMonto 
                FROM ConfiguracionAcreditacion AS config 
                INNER JOIN CUENTASBUZONES AS cb ON config.CuentasBuzonesId = cb.ID 
                INNER JOIN cc ON cc.nc = config.NC 
                INNER JOIN {TableNameResolver.AcreditacionDeposito} AS acc  
                ON acc.IDBUZON = config.NC AND acc.IDCUENTA = cb.ID 
                WHERE cb.BANCO =  @banco
                and config.TipoAcreditacion not in ('tanda') 
                AND CONVERT(DATE, acc.FECHA) = CONVERT(DATE, GETDATE()) 
                GROUP BY 
                cb.CUENTA,
                cb.EMPRESA,
                cb.SUCURSAL,
                cc.SUCURSAL,
                acc.MONEDA,
                cb.IDCLIENTE
                ORDER BY cb.empresa asc";

                query = $@"
                        WITH config_unica AS (
                        SELECT NC, CuentasBuzonesId
                        FROM (
                        SELECT
                        NC, CuentasBuzonesId,
                        ROW_NUMBER() OVER (PARTITION BY NC, CuentasBuzonesId ORDER BY ConfigId DESC) AS rn
                        FROM ConfiguracionAcreditacion
                        WHERE TipoAcreditacion NOT IN ('tanda')
                        ) x
                        WHERE rn = 1
                        ),
                        cc_unica AS (
                        SELECT NC, SUCURSAL
                        FROM (
                        SELECT
                        CC.*,
                        ROW_NUMBER() OVER (
                        PARTITION BY NC
                        ORDER BY CASE WHEN FECHABAJA IS NULL THEN 0 ELSE 1 END, FECHAINST DESC
                        ) AS rn
                        FROM CC
                        ) y
                        WHERE rn = 1
                        )
                        SELECT
                        cb.CUENTA,
                        cb.EMPRESA,
                        cb.SUCURSAL,
                        acc.MONEDA,
                        cc_unica.SUCURSAL AS CIUDAD,
                        cb.IDCLIENTE,
                        SUM(acc.MONTO) AS TotalMonto
                        FROM {TableNameResolver.AcreditacionDeposito} acc
                        JOIN CUENTASBUZONES cb
                        ON cb.ID = acc.IDCUENTA
                        JOIN config_unica cfg
                        ON cfg.NC = acc.IDBUZON
                        AND cfg.CuentasBuzonesId = cb.ID
                        LEFT JOIN cc_unica              -- <== LEFT para no perder filas si falta CC
                        ON cc_unica.NC = acc.IDBUZON
                        WHERE UPPER(cb.BANCO) = UPPER('BBVA')              -- <== normalizado
                        AND acc.FECHA >= CAST(GETDATE() AS date)         -- <== sargable, rango del día
                        AND acc.FECHA <  DATEADD(day, 1, CAST(GETDATE() AS date))
                        GROUP BY
                        cb.CUENTA, cb.EMPRESA, cb.SUCURSAL,
                        acc.MONEDA, cc_unica.SUCURSAL, cb.IDCLIENTE
                        ORDER BY cb.EMPRESA ASC;";
            }

            if (banco.NombreBanco == VariablesGlobales.santander.ToUpper() && tipoAcreditacion.TipoAcreditacion.ToUpper() == VariablesGlobales.diaxdia.ToUpper())
            {
                query = $@"SELECT 
                cb.EMPRESA,
                cc.nn,
                cc.SUCURSAL as CIUDAD,
                cb.CUENTA,
                acc.MONEDA,
                cb.SUCURSAL,
                config.TipoAcreditacion,
                cb.IDCLIENTE as IDCLIENTE,
                SUM(acc.MONTO) AS TOTAL
                FROM ConfiguracionAcreditacion AS config
                INNER JOIN CUENTASBUZONES AS cb ON config.CuentasBuzonesId = cb.ID
                INNER JOIN cc ON cb.IDCLIENTE = cc.IDCLIENTE AND config.NC = cc.NC
                INNER JOIN {TableNameResolver.AcreditacionDeposito} AS acc 
                ON acc.IDBUZON = config.NC AND acc.IDCUENTA = cb.ID
                WHERE config.TipoAcreditacion = @tipoAcreditacion
                AND cb.BANCO = @banco
                AND cc.IDCLIENTE NOT IN ('268')
                AND acc.FECHA >= @fechaDesde
                AND acc.FECHA < DATEADD(day, 1, @fechaDesde)
                GROUP BY 
                cb.BANCO,
                cb.CUENTA,
                cb.EMPRESA,
                cb.SUCURSAL,
                acc.MONEDA,
                cc.sucursal,
                cc.NN,   
                cb.IDCLIENTE,
                config.TipoAcreditacion
                ORDER BY cb.EMPRESA ASC";
            }

            if (banco.NombreBanco.ToUpper() == VariablesGlobales.scotiabank.ToUpper() && tipoAcreditacion.TipoAcreditacion.ToLower() == VariablesGlobales.diaxdia.ToLower())
            {

                //Si es Scotia,sacar 
                query = $@"SELECT 
                cC.NN,
                CB.EMPRESA,
                cc.SUCURSAL as CIUDAD,
                cb.CUENTA,
                acc.MONEDA,
                cb.SUCURSAL,
                cb.IDCLIENTE as IDCLIENTE,
                config.TipoAcreditacion,
                SUM(acc.MONTO) AS TOTAL 
                FROM ConfiguracionAcreditacion AS config 
                INNER JOIN CUENTASBUZONES AS cb ON config.CuentasBuzonesId = cb.ID 
                INNER JOIN cc ON cb.IDCLIENTE = cc.IDCLIENTE AND config.NC = cc.NC 
                INNER JOIN {TableNameResolver.AcreditacionDeposito} AS acc  
                ON acc.IDBUZON = config.NC AND acc.IDCUENTA = cb.ID 
                WHERE config.TipoAcreditacion = @tipoAcreditacion 
                AND cb.BANCO = @banco 
                AND cc.IDCLIENTE NOT IN ('268') 
                AND CB.IDCLIENTE NOT IN('164') 
                AND CONVERT(DATE, acc.FECHA) = CONVERT(DATE, GETDATE()) 
                GROUP BY 
                cb.BANCO, 
                cb.CUENTA,
                cC.NN,
                cb.SUCURSAL,
                acc.MONEDA,
                cc.sucursal,
                CB.EMPRESA,
                cb.IDCLIENTE,
                config.TipoAcreditacion 
                ORDER BY cc.NN ASC";
            }

            if (banco.NombreBanco.ToUpper() == VariablesGlobales.hsbc.ToUpper() ||
                banco.NombreBanco.ToUpper() == VariablesGlobales.itau.ToUpper() ||
                banco.NombreBanco.ToUpper() == VariablesGlobales.bandes.ToUpper())
            {

                //ESTA QUERY ANDA BIEN PERO SI LOS REGISTROS DE CONFIGBUZON ESTAN DUPLICADOS ,L O MUESTRA DOBLE EL RESULTADO.
                //query = @"SELECT  
                //cb.EMPRESA, 
                //cc.nn, 
                //cc.SUCURSAL as CIUDAD, 
                //cb.CUENTA, 
                //acc.MONEDA, 
                //cb.SUCURSAL, 
                //config.TipoAcreditacion,
                //cb.IDCLIENTE as IDCLIENTE,
                //SUM(acc.MONTO) AS TOTAL 
                //FROM ConfiguracionAcreditacion AS config 
                //INNER JOIN CUENTASBUZONES AS cb ON config.CuentasBuzonesId = cb.ID 
                //INNER JOIN cc ON cb.IDCLIENTE = cc.IDCLIENTE AND config.NC = cc.NC 
                //INNER JOIN ACREDITACIONDEPOSITODIEGOTEST AS acc  
                //ON acc.IDBUZON = config.NC AND acc.IDCUENTA = cb.ID 
                //WHERE config.TipoAcreditacion = @tipoAcreditacion 
                //AND cb.BANCO = @banco 
                //AND convert(date,acc.FECHA) = convert(date,getdate()) 
                //GROUP BY 
                //cb.BANCO, 
                //cb.CUENTA, 
                //cb.EMPRESA, 
                //cb.SUCURSAL, 
                //acc.MONEDA, 
                //cc.sucursal, 
                //cc.NN, 
                //config.TipoAcreditacion,
                //cb.IDCLIENTE
                //ORDER BY cb.EMPRESA ASC";


                //Query "mejorada" que evita duplicaciones aunque configAcreditacion tenga registros duplicados para un mismo buzón y cuenta.
                //A chequear...
                // ✅ Usar TableNameResolver para obtener nombre de tabla según RuntimeMode
                var tableName = TableNameResolver.AcreditacionDeposito;
                TableNameResolver.ValidateTableName(tableName, "ServicioCuentaBuzon.getAcreditacionesParaExcelTesoreria");
                query = $@"WITH Acc AS (
                        SELECT acc.IDBUZON, acc.IDCUENTA, acc.MONEDA, 
                        CONVERT(date, acc.FECHA) AS FECHA, 
                        SUM(acc.MONTO) AS TOTAL_ACREDITADO 
                        FROM {tableName} acc 
                        WHERE CONVERT(date, acc.FECHA) = CONVERT(date, getdate()) 
                        GROUP BY acc.IDBUZON, acc.IDCUENTA, acc.MONEDA, CONVERT(date, acc.FECHA) 
                        ), 
                        Config AS ( 
                        SELECT CuentasBuzonesId, NC, TipoAcreditacion 
                        FROM ConfiguracionAcreditacion 
                        WHERE TipoAcreditacion=@tipoAcreditacion 
                        GROUP BY CuentasBuzonesId, NC, TipoAcreditacion 
                        )
                        SELECT 
                        cb.EMPRESA, cc.NN, cc.SUCURSAL AS CIUDAD, 
                        cb.CUENTA, a.MONEDA, cb.SUCURSAL, 
                        cfg.TipoAcreditacion, cb.IDCLIENTE AS IDCLIENTE, 
                        a.TOTAL_ACREDITADO AS TOTAL 
                        FROM Config AS cfg 
                        JOIN CUENTASBUZONES AS cb 
                        ON cfg.CuentasBuzonesId = cb.ID 
                        JOIN Acc AS a 
                        ON a.IDBUZON = cfg.NC 
                        AND a.IDCUENTA = cb.ID 
                        LEFT JOIN ( 
                        SELECT DISTINCT IDCLIENTE, NC, NN, SUCURSAL 
                        FROM cc 
                        ) AS cc 
                        ON cb.IDCLIENTE = cc.IDCLIENTE 
                        AND cfg.NC = cc.NC 
                        WHERE cb.BANCO=@banco  
                        AND a.TOTAL_ACREDITADO > 0 
                        ORDER BY cb.EMPRESA ASC;";
            }

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {

                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@banco", banco.NombreBanco);

                cmd.Parameters.AddWithValue("@tipoAcreditacion", tipoAcreditacion.TipoAcreditacion);
                cmd.Parameters.Add("@fechaDesde", SqlDbType.DateTime).Value = baseDia;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (isBBVA)
                    {
                        // Para BBVA, obtenemos solo los ordinals de las columnas que devuelve su consulta.

                        int cuentaOrdinal = reader.GetOrdinal("CUENTA");
                        int empresaOrdinal = reader.GetOrdinal("EMPRESA");
                        int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");
                        int monedaOrdinal = reader.GetOrdinal("MONEDA");
                        int ciudadOrdinal = reader.GetOrdinal("CIUDAD");
                        int totalMontoOrdinal = reader.GetOrdinal("TotalMonto");
                        int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");

                        // Aquí se lee cada registro de la consulta de BBVA.
                        while (reader.Read())
                        {
                            DtoAcreditacionesPorEmpresa nuevoDto = new DtoAcreditacionesPorEmpresa();

                            // Asigna los campos específicos para BBVA. 
                            // Ajusta los tipos de datos según corresponda.

                            nuevoDto.NumeroCuenta = reader.GetString(cuentaOrdinal);
                            nuevoDto.Empresa = reader.GetString(empresaOrdinal);
                            nuevoDto.Sucursal = reader.GetString(sucursalOrdinal);
                            nuevoDto.Divisa = reader.GetInt32(monedaOrdinal);
                            nuevoDto.Monto = reader.GetDouble(totalMontoOrdinal);
                            nuevoDto.Ciudad = reader.GetString(ciudadOrdinal);
                            nuevoDto.IdCliente = reader.GetInt32(idClienteOrdinal);

                            // Si tienes algún método para ajustar o formatear la moneda.
                            nuevoDto.setMoneda();

                            retorno.Add(nuevoDto);
                        }
                    }
                    else
                    {
                        // Para Santander y Scotiabank (u otros) obtenemos los ordinals correspondientes a sus columnas.
                        int empresaOrdinal = reader.GetOrdinal("EMPRESA");
                        int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");
                        int ciudadOrdinal = reader.GetOrdinal("CIUDAD");
                        int cuentaOrdinal = reader.GetOrdinal("CUENTA");
                        int monedaOrdinal = reader.GetOrdinal("MONEDA");
                        int totalOrdinal = reader.GetOrdinal("TOTAL");
                        int nnOrdinal = reader.GetOrdinal("NN");
                        int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");
                        while (reader.Read())
                        {
                            DtoAcreditacionesPorEmpresa nuevoDto = new DtoAcreditacionesPorEmpresa();

                            nuevoDto.Empresa = reader.GetString(empresaOrdinal);
                            nuevoDto.Sucursal = reader.GetString(sucursalOrdinal);
                            nuevoDto.Ciudad = reader.GetString(ciudadOrdinal);
                            nuevoDto.NumeroCuenta = reader.GetString(cuentaOrdinal);
                            nuevoDto.Divisa = reader.GetInt32(monedaOrdinal);
                            nuevoDto.Monto = reader.GetDouble(totalOrdinal);
                            nuevoDto.NN = reader.GetString(nnOrdinal);
                            nuevoDto.IdCliente = reader.GetInt32(idClienteOrdinal);
                            nuevoDto.setMoneda();

                            retorno.Add(nuevoDto);
                        }
                    }
                }
            }

            return retorno;
        }
        public async Task generarExcelDelResumenDelDiaSantander(string tarea, DateTime? diaOperativo = null)
        {
            // Obtener datos
            var banco = ServicioBanco.getInstancia().getByNombre("SANTANDER");
            var datos = getAcreditacionesDeHoy(banco, diaOperativo);

            // Agrupamos por ciudad de forma case-insensitive
            var porCiudad = datos
                .GroupBy(d => d.Ciudad, StringComparer.OrdinalIgnoreCase);

            foreach (var grupo in porCiudad)
            {
                string ciudad = grupo.Key;                 // p.ej. "Montevideo", "Maldonado", …
                generarReporteDiario(
                    ciudad,
                    grupo,
                    banco,
                    tarea,
                    diaOperativo);
            }

            // pausa breve para asegurar envío
            await Task.Delay(100);

        }


        public void generarReporteDiario(
    string ciudad,
    IEnumerable<DtoAcreditacionesPorEmpresa> datosCiudad,
    Banco banco,
    string tarea,
    DateTime? diaOperativo = null
    )
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Resumen");
            int row = 1;
            var fechaReferencia = (diaOperativo ?? DateTime.Today).Date;

            // -- Encabezado común
            InsertarLogoDesdeRecurso(ws, ref row);
            ws.Cell(row, 1).Value = $"Reporte Diario Santander - {ciudad} - {fechaReferencia:dd/MM/yyyy}";
            ws.Range(row, 1, row, 5)
              .Merge()
              .Style.Font.SetBold().Font.SetFontSize(14)
              .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            row += 2;

            // Aquí forzamos el orden: primero UYU, luego USD, luego otras monedas
            var monedaGroups = datosCiudad
                .GroupBy(d => d.Moneda, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g =>
                    g.Key.Equals("UYU", StringComparison.OrdinalIgnoreCase) ? 0 :
                    g.Key.Equals("USD", StringComparison.OrdinalIgnoreCase) ? 1 :
                    2
                );

            foreach (var monedaGroup in monedaGroups)
            {
                // Encabezado de sección
                ws.Cell(row, 1).Value = "CLIENTE";
                ws.Cell(row, 2).Value = "SUCURSAL";
                ws.Cell(row, 3).Value = "CUENTA";
                ws.Cell(row, 4).Value = "MONEDA";
                ws.Cell(row, 5).Value = "MONTO";
                var hdr = ws.Range(row, 1, row, 5);
                hdr.Style.Font.SetBold()
                   .Fill.SetBackgroundColor(XLColor.LightGray)
                   .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                   .Border.OutsideBorder = XLBorderStyleValues.Thin;
                row++;

                // Filas de datos
                foreach (var it in monedaGroup)
                {
                    ws.Cell(row, 1).Value = it.Empresa;
                    ws.Cell(row, 2).Value = it.Sucursal;
                    ws.Cell(row, 3).Value = it.NumeroCuenta;
                    ws.Cell(row, 4).Value = it.Moneda;      // "UYU" o "USD"
                    ws.Cell(row, 5).Value = ServicioUtilidad
                        .getInstancia()
                        .FormatearDoubleConPuntosYComas(it.Monto);

                    ws.Range(row, 1, row, 5)
                      .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    row++;
                }

                // Total
                ws.Cell(row, 1).Value = $"Total {monedaGroup.Key}:";
                ws.Cell(row, 5).Value = ServicioUtilidad
                    .getInstancia()
                    .FormatearDoubleConPuntosYComas(monedaGroup.Sum(x => x.Monto));
                var tot = ws.Range(row, 1, row, 5);
                tot.Style.Font.SetBold()
                   .Fill.SetBackgroundColor(XLColor.AliceBlue)
                   .Border.OutsideBorder = XLBorderStyleValues.Thin;
                row += 2;
            }

            ws.Columns().AdjustToContents();
            // Guardar + enviar
            string nombre = $"ReporteDiario_Santander_{ciudad}_{fechaReferencia:yyyyMMdd}_{DateTime.Now:HHmmss}.xlsx";

            //produccion:


            // ✅ Ruta desde configuración (resuelve según RuntimeMode)
            string ruta = Path.Combine(ConfiguracionGlobal.Rutas.BaseExcel, nombre);
            ruta = AplicarGuardiaYPrefijoRuta(ruta, "ServicioCuentaBuzon | generarExcelDelResumenDelDiaSantander");

            //testing:
            //string ruta = Path.Combine(@"C: \Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\", nombre);
            wb.SaveAs(ruta);

            ServicioEmail.getInstancia().enviarExcelPorMail(
                ruta,
                $"Reporte Diario Santander {ciudad}",
                $"Acreditaciones Santander - {ciudad}",
                null,
                banco,
                tarea,
                ciudad
                );
        }



        #region UTILITIES
        private void InsertarLogoDesdeRecurso(IXLWorksheet ws, ref int row)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = "ANS.Images.logoTecniFinal.png"; // Ajusta si tu namespace cambia

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    // Insertamos el logo en la celda C1 (col 3) con un pequeño offset
                    var imagen = ws.AddPicture(stream)
                                   .MoveTo(ws.Cell(row, 3), 30, 5) // Offset horizontal y vertical en píxeles
                                   .WithPlacement(XLPicturePlacement.FreeFloating)
                                   .Scale(0.5); // Escala ajustable

                    row += 2; // Espacio debajo del logo
                }
            }

            // Texto "BUZONES INTELIGENTES" centrado
            ws.Range(row, 1, row, 5).Merge();
            var celdaTitulo = ws.Cell(row, 1);
            celdaTitulo.Value = "Buzones Inteligentes";
            celdaTitulo.Style.Font.Bold = true;
            celdaTitulo.Style.Font.FontSize = 16;
            celdaTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            celdaTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            row += 1;
        }


        private static void InsertarLogoParaTesoreriaExcel(IXLWorksheet ws, ref int row)
        {
            var asm = Assembly.GetExecutingAssembly();
            const string resourcePath = "ANS.Images.logoTecniFinal.png";
            using var stream = asm.GetManifestResourceStream(resourcePath);
            if (stream != null)
            {
                ws.AddPicture(stream)
                  .MoveTo(ws.Cell(row, 3), 30, 5)
                  .WithPlacement(XLPicturePlacement.FreeFloating)
                  .Scale(0.5);
                row += 2;
            }
        }

        #endregion
        #region UNUSED
        //METODO PARA OBTENER ACREDITACIONES(EXCEL , VA POR FECHA)
        //private void getAcreditacionesPorBuzones(List<CuentaBuzon> listaCuentasBuzones, TimeSpan hasta, TimeSpan desde, Banco bank)
        //{
        //    if (listaCuentasBuzones != null && listaCuentasBuzones.Count > 0)
        //    {
        //        try
        //        {

        //            DateTime hastaEfectivo = DateTime.Today.Add(hasta);

        //            DateTime desdeEfectivo = DateTime.Today.Add(desde);

        //            using (SqlConnection conn = new SqlConnection(_conexionTSD))
        //            {

        //                conn.Open();

        //                string query;

        //                foreach (CuentaBuzon account in listaCuentasBuzones)
        //                {

        //                    query = "select * " +
        //                            "from acreditaciondepositodiegotest " +
        //                            "where idbuzon = @accNC " +
        //                            "and convert(date,fecha) = convert(date,getdate()) " +
        //                            "and fecha >= @desde " +
        //                            "and fecha <= @hasta " +
        //                            "and idbanco = @bankId " +
        //                            "and idcuenta = @accId";

        //                    SqlCommand cmd = new SqlCommand(query, conn);

        //                    cmd.Parameters.AddWithValue("@accNC", account.NC);

        //                    cmd.Parameters.AddWithValue("@hasta", hastaEfectivo);

        //                    cmd.Parameters.AddWithValue("@bankId", bank.BancoId);

        //                    cmd.Parameters.AddWithValue("@accId", account.IdCuenta);

        //                    cmd.Parameters.AddWithValue("@desde", desdeEfectivo);

        //                    using (SqlDataReader reader = cmd.ExecuteReader())
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            Acreditacion accreditation = new Acreditacion
        //                            {
        //                                Id = reader.GetInt32(0),
        //                                IdBuzon = reader.GetString(1),
        //                                IdOperacion = reader.GetInt64(2),
        //                                Fecha = reader.GetDateTime(3),
        //                                IdBanco = reader.GetInt32(4),
        //                                IdCuenta = reader.GetInt32(5),
        //                                Moneda = reader.GetInt32(6),
        //                                No_Enviado = reader.GetBoolean(7),
        //                                Monto = (float)reader.GetDouble(8) // Se lee como double y se convierte a float.
        //                            };
        //                            account.ListaAcreditaciones.Add(accreditation);
        //                        }
        //                    }

        //                }

        //            }
        //            return;
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("Exception " + ex.Message);
        //        }
        //    }
        //    else
        //    {
        //        throw new Exception("Error en getAcreditacionesPorBuzones: ListaCuentaBuzones vacia o nula.");
        //    }
        //}
        #endregion
    }
}