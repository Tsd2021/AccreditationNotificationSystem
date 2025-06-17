using ANS.Model.GeneradorArchivoPorBanco;
using ANS.Model.Interfaces;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Reflection;

namespace ANS.Model.Services
{
    public class ServicioCuentaBuzon : IServicioCuentaBuzon
    {
        private string _conexionTSD = ConfiguracionGlobal.Conexion22;
        public static ServicioCuentaBuzon instancia { get; set; }
        public ServicioEmail _emailService { get; set; } = new ServicioEmail();
        public static ServicioCuentaBuzon getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioCuentaBuzon();
            }
            return instancia;
        }
        public List<DtoAcreditacionesPorEmpresa> getAcreditacionesDeHoy(Banco b)
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
                                INNER JOIN AcreditacionDepositoDiegoTest AS acc 
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
                string query = @"SELECT c.NC, cb.BANCO, c.CIERRE, c.IDCLIENTE, cb.CUENTA, cb.MONEDA, cb.EMPRESA, config.TipoAcreditacion,c.SUCURSAL as CIUDAD, cb.SUCURSAL, cb.TANDA, c.NN
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
                }

                if (tipoAcreditacion.ToUpper() == VariablesGlobales.diaxdia.ToUpper())
                {
                    if (banco.NombreBanco.ToUpper() == VariablesGlobales.scotiabank.ToUpper())
                    {

                        //Si es dia a dia Scotiabank hay que excluir todo Henderson porq ya fue acreditado.
                        //Y tambien la consulta es especial porque no exluye RELACIONADOS!
                        query = @"select distinct c.NC,
                                cb.BANCO,
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
                                and cb.idcliente not in (164)
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

                        cuentaBuzon.setCashOffice(); // El nombre del Banco define si es CashOffice o no!

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
        public List<CuentaBuzon> getAllByBanco(Banco banco)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                string query = @"
                SELECT DISTINCT 
                c.NC, 
                cb.BANCO, 
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
            string query = @"select config.nc, cb.banco, c.cierre, cb.idcliente, cb.cuenta, cb.moneda, cb.empresa, config.TipoAcreditacion AS CONFIGURACION, c.sucursal as ciudad, cb.sucursal, c.idcc, cb.id, c.nn 
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
            return buzonesFound;
        }
        public async Task<List<CuentaBuzon>> getCuentaBuzonesByClienteYBanco(int idcliente, Banco bank)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();

            string query = @"select DISTINCT config.nc, cb.banco, c.cierre, cb.idcliente, cb.cuenta, cb.moneda, cb.empresa, config.TipoAcreditacion AS CONFIGURACION, c.sucursal as ciudad, cb.sucursal, c.idcc, cb.id, c.nn 
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
        private async Task generarArchivoPorBanco(List<CuentaBuzon> listaCuentaBuzones, Banco banco, string tipoAcreditacion)
        {

            if (listaCuentaBuzones == null)
            {
                throw new Exception("Error en método generarArchivoPorBanco: listaBuzones es null.");
            }

            if (listaCuentaBuzones.Count == 0)
            {
                throw new Exception("Error en método generarArchivoPorBanco: Lista Buzones tiene 0 elementos");
            }

            IBancoModoAcreditacion bank = BankFactory.GetModoAcreditacionByBanco(banco.NombreBanco, tipoAcreditacion);

            if (bank != null)

            {

                await bank.GenerarArchivo(listaCuentaBuzones);

                return;

            }

            throw new Exception("Error en método generarArchivoPorBanco: el modo de" +
                " acreditacion por banco no fue encontrado.");
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
                        if (bank.NombreBanco == VariablesGlobales.santander)
                        {
                            await ServicioDeposito.getInstancia()
                                .asignarDepositosAlBuzon(unBuzon, ultIdOperacionPorBuzon, VariablesGlobales.horaFinPuntoAPuntoSantander);
                        }
                        else
                        {

                            await ServicioDeposito.getInstancia()
                            .asignarDepositosAlBuzon(unBuzon, ultIdOperacionPorBuzon, TimeSpan.Zero);
                        }
                    }
                }

                catch (Exception ex)
                {

                    Console.WriteLine($"Error asignando depósitos para buzón {unBuzon.NC}: {ex.Message}");

                }

            }

            await generarArchivoPorBanco(buzones, bank, VariablesGlobales.p2p);

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
                        if(unaCuentaBuzon.NC == "EA23L0410N12000062")
                        {
                            Console.WriteLine("es fonbay");
                        }
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
                        await generarArchivoPorBanco(cuentasBuzonesConDepositos, banco, VariablesGlobales.diaxdia);

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

                    await generarArchivoPorBanco(cuentaBuzones, bank, VariablesGlobales.tanda);

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
                string query = "select max(idoperacion) from AcreditacionDepositoDiegoTest where IDBUZON = @ncFound and IDCUENTA = @idCuenta and MONEDA = @idMoneda";

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
                    int ultimoIdOperacion = await obtenerUltimaOperacionByNC(acc);

                    if (ultimoIdOperacion > 0)
                    {
                        await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(acc, ultimoIdOperacion, horaCierreActual);
                    }

                }

                if (cuentas.Count > 0)
                {
                    {
                        await generarArchivoPorBanco(cuentas, santander, VariablesGlobales.tanda);

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
                List<CuentaBuzon> cuentas = await getCuentasPorClienteBancoYTipoAcreditacion(henderson.IdCliente, scotia, config);

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
                        await generarArchivoPorBanco(cuentasConDepositos, scotia, VariablesGlobales.tanda);

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

                await generarArchivoPorBanco(buzonesPorBanco, bank, VariablesGlobales.diaxdia);

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

                        await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(cu, ultIdOperacion, horaCierreActual);

                    }
                }

                await generarArchivoPorBanco(cuentaBuzones, bank, VariablesGlobales.tanda);

                await ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzones(cuentaBuzones);

            }

        }
        //Enviar Excel genérico.
        public async Task enviarExcel(TimeSpan desde, TimeSpan hasta, Cliente cli, Banco bank)
        {


        }
        //Enviar Excel Específico para Henderson. (07:10)T1 (14:35)T2
        public async Task enviarExcelFormatoTanda(TimeSpan desde, TimeSpan hasta, Cliente cli, Banco bank, string city, int numTanda)
        {
            try
            {

                DateTime fechaDesde = DateTime.Today.Add(desde);

                DateTime fechaHasta = DateTime.Today.Add(hasta);

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

                List<DtoAcreditacionesPorEmpresa> acreditacionesFound = await ServicioAcreditacion.getInstancia().getAcreditacionesByFechaBancoClienteYTipoAcreditacion(fechaDesde, fechaHasta, cli, bank, config);

                List<DtoAcreditacionesPorEmpresa> listaMontevideo = acreditacionesFound.Where(x => x.Ciudad.ToUpper() == "MONTEVIDEO").ToList();

                List<DtoAcreditacionesPorEmpresa> listaMaldonado = acreditacionesFound.Where(x => x.Ciudad.ToUpper() == "MALDONADO").ToList();

                generarExcelFormatoTanda(listaMontevideo, listaMaldonado, numTanda, bank, cli, config);

            }
            catch (Exception e)
            {
                throw e;
            }
        }
        private void generarExcelFormatoTanda(List<DtoAcreditacionesPorEmpresa> acreditacionesMontevideo, List<DtoAcreditacionesPorEmpresa> acreditacionesMaldonado, int numTanda, Banco b, Cliente c, ConfiguracionAcreditacion config)
        {
            // Colores para el formato
            var pastelYellow = XLColor.LightYellow;
            var pastelCyan = XLColor.LightCyan;

            void InsertarLogoDesdeRecurso(IXLWorksheet ws, ref int row)
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
                celdaFecha.Value = $"{b.NombreBanco} - Tanda {numTanda} - {DateTime.Now}";
                if (b.NombreBanco.ToUpper() == VariablesGlobales.bbva.ToUpper())
                {
                    celdaFecha.Value = $"{b.NombreBanco}  - {DateTime.Now}";
                }
                celdaFecha.Style.Font.Italic = true;
                celdaFecha.Style.Font.FontSize = 11;
                celdaFecha.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                celdaFecha.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row++; // una fila en blanco

                // Título
                ws.Range(row, 1, row, 5).Merge();
                var celdaTitulo = ws.Cell(row, 1);
                celdaTitulo.Value = "Buzones Inteligentes";
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
                InsertarLogoDesdeRecurso(ws, ref row);

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
                    fn = $"{b.NombreBanco}_Henderson_{ciudad}_Tanda_{numTanda}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                else if (b.NombreBanco.Equals(VariablesGlobales.bbva, StringComparison.OrdinalIgnoreCase))
                    fn = $"{b.NombreBanco}_{ciudad}_TATA_{numTanda}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                else
                    fn = $"{b.NombreBanco}_{ciudad}_Tanda_{numTanda}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                //PRODUCCION: 
                string path = Path.Combine(@"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\EXCEL\", fn);

                //TESTING:
                //string path = Path.Combine(@"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\", fn);

                wb.SaveAs(path);

                Console.WriteLine($"Excel generado para {ciudad}: {path}");
                try
                {
                    string asu, cue;

                    if (b.NombreBanco.Equals(VariablesGlobales.santander, StringComparison.OrdinalIgnoreCase)
                     || b.NombreBanco.Equals(VariablesGlobales.scotiabank, StringComparison.OrdinalIgnoreCase))
                    {
                        asu = $"Acreditaciones {b.NombreBanco} (HENDERSON) Tanda {numTanda} - {ciudad.ToUpper()}";
                        cue = $"Adjunto archivo de acreditaciones para {b.NombreBanco}(HENDERSON) Tanda {numTanda} ciudad {ciudad.ToUpper()}.";
                    }
                    else
                    {
                        asu = $"Acreditaciones {b.NombreBanco} (TATA) - {ciudad.ToUpper()}";
                        cue = $"Adjunto archivo de acreditaciones para buzones TATA ciudad {ciudad.ToUpper()} banco {b.NombreBanco}.";
                    }
                    _emailService.enviarExcelPorMail(path, asu, cue, c, b, config);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al enviar el correo: " + ex.Message);
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
        public async Task enviarExcelTesoreria(Banco banco, string city, int numTanda, TimeSpan desde, TimeSpan hasta)
        {

            DateTime fechaDesde = DateTime.Today.Add(desde);

            DateTime fechaHasta = DateTime.Today.Add(hasta);

            List<DtoAcreditacionesPorEmpresa> lista = await ServicioAcreditacion.getInstancia().getAcreditacionesByFechaYBanco(fechaDesde, fechaHasta, banco);

            List<DtoAcreditacionesPorEmpresa> listaMontevideo = lista.Where(x => x.Ciudad.ToUpper() == "MONTEVIDEO").ToList();

            List<DtoAcreditacionesPorEmpresa> listaMaldonado = lista.Where(x => x.Ciudad.ToUpper() == "MALDONADO").ToList();

            generarExcelPorAcreditacionesAgrupadoPorEmpresa(listaMontevideo, listaMaldonado, numTanda, banco);

        }


        private void generarExcelPorAcreditacionesAgrupadoPorEmpresa(
           List<DtoAcreditacionesPorEmpresa> acreditacionesMontevideo,
           List<DtoAcreditacionesPorEmpresa> acreditacionesMaldonado,
           int numTanda,
           Banco b)
        {
            var amarilloPastel = XLColor.LightYellow;
            var celestePastel = XLColor.LightCyan;

            void InsertarLogoDesdeRecurso(IXLWorksheet ws, ref int row)
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
                        row += 1;
                    }
                }

                // Fecha
                ws.Range(row, 1, row, 5).Merge();
                var celdaFecha = ws.Cell(row, 1);
                celdaFecha.Value = "Tesorería - " +
                    DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy",
                        new System.Globalization.CultureInfo("es-ES"));
                celdaFecha.Style.Font.Italic = true;
                celdaFecha.Style.Font.FontSize = 11;
                celdaFecha.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                celdaFecha.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row += 1;

                // Título
                ws.Range(row, 1, row, 5).Merge();
                var celdaTitulo = ws.Cell(row, 1);
                celdaTitulo.Value = "BUZONES INTELIGENTES";
                celdaTitulo.Style.Font.Bold = true;
                celdaTitulo.Style.Font.FontSize = 16;
                celdaTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                celdaTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row += 2;
            }

            void generarArchivoExcel(List<DtoAcreditacionesPorEmpresa> lista, string ciudad)
            {
                if (lista == null || !lista.Any()) return;

                var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add(ciudad);
                ws.ShowGridLines = false;
                int row = 1;

                InsertarLogoDesdeRecurso(ws, ref row);

                void AgregarSeccion(List<DtoAcreditacionesPorEmpresa> datos, string moneda)
                {
                    if (!datos.Any()) return;

                    // Encabezados
                    ws.Cell(row, 1).Value = "CLIENTE";
                    ws.Cell(row, 2).Value = "SUCURSAL";
                    ws.Cell(row, 3).Value = "CUENTA";
                    ws.Cell(row, 4).Value = "MONEDA";
                    ws.Cell(row, 5).Value = "MONTO";

                    var headerRange = ws.Range(row, 1, row, 5);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = amarilloPastel;
                    headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    row++;

                    foreach (var item in datos)
                    {
                        ws.Cell(row, 1).Value = item.Empresa;
                        ws.Cell(row, 2).Value = item.Sucursal;
                        ws.Cell(row, 3).Value = item.NumeroCuenta;
                        ws.Cell(row, 4).Value = item.Moneda;
                        ws.Cell(row, 5).Value = ServicioUtilidad
                            .getInstancia()
                            .FormatearDoubleConPuntosYComas(item.Monto);

                        var dataRange = ws.Range(row, 1, row, 5);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        row++;
                    }

                    // Total
                    ws.Cell(row, 4).Value = $"Total {moneda.ToUpper()}";
                    ws.Cell(row, 5).Value = ServicioUtilidad
                        .getInstancia()
                        .FormatearDoubleConPuntosYComas(datos.Sum(x => x.Monto));

                    var totalRange = ws.Range(row, 4, row, 5);
                    totalRange.Style.Fill.BackgroundColor = celestePastel;
                    totalRange.Style.Font.Bold = true;
                    totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    totalRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    row++;

                    // Separador
                    ws.Range(row, 1, row, 5).Merge();
                    row++;
                }

                // Secciones
                var listaPesos = lista.Where(x => x.Moneda.ToUpper() == "PESOS").ToList();
                var listaDolares = lista.Where(x => x.Moneda.ToUpper() == "DOLARES").ToList();

                AgregarSeccion(listaPesos, "Pesos");
                AgregarSeccion(listaDolares, "Dólares");

                // Ajuste de ancho A–E
                ws.Columns(1, 5).AdjustToContents();

                // 2) Oculta columnas F en adelante
                ws.Columns(6, ws.ColumnCount()).Hide();

                // 3) Fija el área de impresión a A1:E<lastRow>
                var lastRow = ws.LastRowUsed().RowNumber();
                ws.PageSetup.PrintAreas.Add($"A1:E{lastRow}");
                ws.PageSetup.FitToPages(1, 0);

                // Guardar y enviar
                string nombreArchivo = $"Tesoreria_Tanda_{numTanda}_{ciudad.ToUpper()}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                // PRODUCCION:

                string filePath = Path.Combine(@"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\EXCEL\", nombreArchivo);

                //TESTING:
                //string filePath = Path.Combine(@"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\", nombreArchivo);
                wb.SaveAs(filePath);

                Console.WriteLine($"Excel de Tesorería generado: {filePath}");
                try
                {
                    string asunto = $"Acreditaciones para Tesorería {b.NombreBanco} Tanda {numTanda} - {ciudad.ToUpper()}";
                    string cuerpo = $"E-Mail específico para TESORERÍA TECNISEGUR.\n" +
                                    $"Adjunto el archivo de acreditaciones para la tanda {numTanda} de {ciudad.ToUpper()}.";

                    _emailService.enviarExcelPorMail(filePath, asunto, cuerpo, null, b, new ConfiguracionAcreditacion(VariablesGlobales.tanda));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al enviar el correo: " + ex.Message);
                }
            }

            generarArchivoExcel(acreditacionesMontevideo, "Montevideo");
            generarArchivoExcel(acreditacionesMaldonado, "Maldonado");
        }

        public async Task enviarExcelDiaADiaPorBanco(Banco banco, ConfiguracionAcreditacion tipoAcreditacion)
        {

            List<DtoAcreditacionesPorEmpresa> acreditacionesPorBancoYTipoAcreditacion = getAcreditacionesPorBancoYTipoAcreditacion(banco, tipoAcreditacion);

            List<DtoAcreditacionesPorEmpresa> acreditacionesPesosMaldonado = new List<DtoAcreditacionesPorEmpresa>();

            List<DtoAcreditacionesPorEmpresa> acreditacionesDolaresMaldonado = new List<DtoAcreditacionesPorEmpresa>();

            List<DtoAcreditacionesPorEmpresa> acreditacionesPesosMvd = new List<DtoAcreditacionesPorEmpresa>();

            List<DtoAcreditacionesPorEmpresa> acreditacionesDolaresMvd = new List<DtoAcreditacionesPorEmpresa>();

            foreach (DtoAcreditacionesPorEmpresa _ac in acreditacionesPorBancoYTipoAcreditacion)
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

            GenerarExcelFormatoDiaADia("MONTEVIDEO", acreditacionesPesosMvd, acreditacionesDolaresMvd, banco);

            GenerarExcelFormatoDiaADia("MALDONADO", acreditacionesPesosMaldonado, acreditacionesDolaresMaldonado, banco);

            await Task.CompletedTask;

        }
        private void GenerarExcelFormatoDiaADia(string ciudad, List<DtoAcreditacionesPorEmpresa> listaPesos, List<DtoAcreditacionesPorEmpresa> listaDolares, Banco banco)
        {
            var fechaHoy = DateTime.Now.ToString("dd - MM - yy");
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
                if (banco.NombreBanco.ToUpper() == VariablesGlobales.scotiabank.ToUpper())
                {
                    item.Empresa = item.NN;
                }
                ws.Cell(row, 1).Value = item.Empresa;
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
                if (banco.NombreBanco.ToUpper() == VariablesGlobales.scotiabank.ToUpper())
                {
                    item.Empresa = item.NN;
                }
                ws.Cell(row, 1).Value = item.Empresa;
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

            string nombreArchivo = $"AcreditacionesDiaADia_{banco.NombreBanco}_{ciudad}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            //produccion:
            string ruta = Path.Combine(@"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\EXCEL\", nombreArchivo);
            //testing:
            //string ruta = Path.Combine(@"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\", nombreArchivo);


            workbook.SaveAs(ruta);

            try
            {
                _emailService.enviarExcelPorMail(ruta, $"Acreditaciones Día a día - ({banco.NombreBanco}) - " + ciudad.ToUpper(),
                    $"Reporte de las acreditaciones realizadas día a día del banco: {banco.NombreBanco} (" + ciudad.ToUpper() + ")", null, banco, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }


        private List<DtoAcreditacionesPorEmpresa> getAcreditacionesPorBancoYTipoAcreditacion(Banco banco, ConfiguracionAcreditacion tipoAcreditacion)
        {

            List<DtoAcreditacionesPorEmpresa> retorno = new List<DtoAcreditacionesPorEmpresa>();

            string query = "";
            bool isBBVA = false;

            if (banco.NombreBanco == VariablesGlobales.bbva.ToUpper() && tipoAcreditacion.TipoAcreditacion.ToUpper() == VariablesGlobales.diaxdia.ToUpper())

            {

                // Se supone que se obtiene acreditaciones para generar el excel, y el excel de BBVA tiene que incluir las acreditaciones de los dia a dia y los punto a punto
                // Por lo tanto, la consulta es muy especifica.
                isBBVA = true;

                query = @"SELECT 
                        cb.CUENTA,
                        cb.EMPRESA,
                        cb.SUCURSAL,
                        acc.MONEDA,
                        cc.SUCURSAL as CIUDAD, 
                        cb.IDCLIENTE as IDCLIENTE,
                        SUM(acc.MONTO) AS TotalMonto 
                        FROM ConfiguracionAcreditacion AS config 
                        INNER JOIN CUENTASBUZONES AS cb ON config.CuentasBuzonesId = cb.ID 
                        INNER JOIN cc ON cb.IDCLIENTE = cc.IDCLIENTE AND config.NC = cc.NC 
                        INNER JOIN AcreditacionDepositoDiegoTest AS acc  
                        ON acc.IDBUZON = config.NC AND acc.IDCUENTA = cb.ID 
                        WHERE cb.BANCO = @banco 
                        and config.TipoAcreditacion not in ('tanda') 
                        AND CONVERT(DATE, acc.FECHA) = CONVERT(DATE, GETDATE()) 
                        GROUP BY 
                        cb.CUENTA,
                        cb.EMPRESA,
                        cb.SUCURSAL,
                        cc.SUCURSAL,
                        acc.MONEDA,
                        cb.IDCLIENTE
                        ORDER BY cb.empresa asc;";
            }

            if (banco.NombreBanco == VariablesGlobales.santander.ToUpper() && tipoAcreditacion.TipoAcreditacion.ToUpper() == VariablesGlobales.diaxdia.ToUpper())
            {
                query = @"SELECT 
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
                        INNER JOIN ACREDITACIONDEPOSITODIEGOTEST AS acc 
                        ON acc.IDBUZON = config.NC AND acc.IDCUENTA = cb.ID
                        WHERE config.TipoAcreditacion = @tipoAcreditacion
                        AND cb.BANCO = @banco
                        AND cc.IDCLIENTE NOT IN ('268')
                        AND CONVERT(DATE, acc.FECHA) = CONVERT(DATE, GETDATE())
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
                query = @"SELECT 
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
                        INNER JOIN ACREDITACIONDEPOSITODIEGOTEST AS acc  
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
                query = @"SELECT  
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
                        INNER JOIN ACREDITACIONDEPOSITODIEGOTEST AS acc  
                        ON acc.IDBUZON = config.NC AND acc.IDCUENTA = cb.ID 
                        WHERE config.TipoAcreditacion = @tipoAcreditacion 
                        AND cb.BANCO = @banco 
                        AND convert(date,acc.FECHA) = convert(date,getdate()) 
                        GROUP BY 
                        cb.BANCO, 
                        cb.CUENTA, 
                        cb.EMPRESA, 
                        cb.SUCURSAL, 
                        acc.MONEDA, 
                        cc.sucursal, 
                        cc.NN, 
                        config.TipoAcreditacion,
                        cb.IDCLIENTE
                        ORDER BY cb.EMPRESA ASC";
            }

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {

                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@banco", banco.NombreBanco);

                cmd.Parameters.AddWithValue("@tipoAcreditacion", tipoAcreditacion.TipoAcreditacion);

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
        public async Task generarExcelDelResumenDelDiaSantander()
        {
            // Obtener datos
            var banco = ServicioBanco.getInstancia().getByNombre("SANTANDER");
            var datos = getAcreditacionesDeHoy(banco);

            // Agrupamos por ciudad de forma case-insensitive
            var porCiudad = datos
                .GroupBy(d => d.Ciudad, StringComparer.OrdinalIgnoreCase);

            foreach (var grupo in porCiudad)
            {
                string ciudad = grupo.Key;                 // p.ej. "Montevideo", "Maldonado", …
                generarReporteDiario(
                    ciudad,
                    grupo,
                    banco);
            }

            // pausa breve para asegurar envío
            await Task.Delay(100);

        }


        public void generarReporteDiario(
    string ciudad,
    IEnumerable<DtoAcreditacionesPorEmpresa> datosCiudad,
    Banco banco
    )
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Resumen");
            int row = 1;

            // -- Encabezado común
            InsertarLogoDesdeRecurso(ws, ref row);
            ws.Cell(row, 1).Value = $"Reporte Diario Santander - {ciudad} - {DateTime.Now:dd/MM/yyyy}";
            ws.Range(row, 1, row, 5).Merge().Style
              .Font.SetBold().Font.SetFontSize(14)
              .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            row += 2;

            // Para cada moneda, imprimimos sección
            foreach (var monedaGroup in datosCiudad
                .GroupBy(d => d.Moneda, StringComparer.OrdinalIgnoreCase))
            {
                // Encabezado
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
                    ws.Cell(row, 4).Value = it.Moneda;      // p.ej. "UYU" o "USD"
                    ws.Cell(row, 5).Value = ServicioUtilidad
                        .getInstancia()
                        .FormatearDoubleConPuntosYComas(it.Monto);

                    ws.Range(row, 1, row, 5)
                      .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    row++;
                }

                // Total
                ws.Cell(row, 1).Value = $"Total {monedaGroup.First().Moneda}:";
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
            string nombre = $"ReporteDiario_Santander_{ciudad}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            //produccion:


            string ruta = Path.Combine(@"C:\Users\Administrador.ABUDIL\Desktop\TAAS TESTING\EXCEL\", nombre);


            //testing:
            //string ruta = Path.Combine(@"C: \Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\", nombre);
            wb.SaveAs(ruta);

            ServicioEmail.getInstancia().enviarExcelPorMail(
                ruta,
                $"Reporte Diario Santander {ciudad}",
                $"Acreditaciones Santander - {ciudad}",
                null,
                banco,
                null);
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