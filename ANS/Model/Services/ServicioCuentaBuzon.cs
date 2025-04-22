using ANS.Model.Interfaces;
using Microsoft.Data.SqlClient;
using ANS.Model.GeneradorArchivoPorBanco;
using ClosedXML.Excel;
using System.IO;
using ClosedXML.Excel.Drawings;
using System.Reflection;
using System.Diagnostics;

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
            List<DtoAcreditacionesPorEmpresa> listaRetorno = new List<DtoAcreditacionesPorEmpresa>();


            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {

                conn.Open();

                string query = @"SELECT 
                            cb.EMPRESA,
                            cc.SUCURSAL as CIUDAD,
                            cb.CUENTA,
                            acc.MONEDA,
                            cb.SUCURSAL,
                            SUM(acc.MONTO) AS TOTAL
                            FROM ConfiguracionAcreditacion AS config
                            INNER JOIN CUENTASBUZONES AS cb ON config.CuentasBuzonesId = cb.ID
                            INNER JOIN cc ON cb.IDCLIENTE = cc.IDCLIENTE AND config.NC = cc.NC
                            INNER JOIN AcreditacionDepositoDiegoTest AS acc 
                            ON acc.IDBUZON = config.NC AND acc.IDCUENTA = cb.ID
                            WHERE cb.BANCO = @banco
                            AND CONVERT(DATE, acc.FECHA) = CONVERT(DATE, GETDATE())
                            GROUP BY 
                            cb.EMPRESA,
                            cb.CUENTA,
                            acc.MONEDA,
                            cc.SUCURSAL,
                            cb.SUCURSAL
                            ORDER BY 
                            cb.EMPRESA,
                            acc.MONEDA;";

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@banco", b.NombreBanco);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {

                    int empresaOrdinal = reader.GetOrdinal("EMPRESA");

                    int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");

                    int ciudadOrdinal = reader.GetOrdinal("CIUDAD");

                    int cuentaOrdinal = reader.GetOrdinal("CUENTA");

                    int monedaOrdinal = reader.GetOrdinal("MONEDA");

                    int totalOrdinal = reader.GetOrdinal("TOTAL");


                    while (reader.Read())
                    {

                        DtoAcreditacionesPorEmpresa dto = new DtoAcreditacionesPorEmpresa();

                        dto.Empresa = reader.GetString(empresaOrdinal);

                        dto.Sucursal = reader.GetString(sucursalOrdinal);

                        dto.NumeroCuenta = reader.GetString(cuentaOrdinal);

                        dto.Divisa = reader.GetInt32(monedaOrdinal);

                        dto.Ciudad = reader.GetString(ciudadOrdinal);

                        dto.setMoneda();

                        dto.Monto = reader.GetDouble(totalOrdinal);

                        listaRetorno.Add(dto);
                    }
                }
            }
            return listaRetorno;
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
       
        private List<Acreditacion>? obtenerAcreditaciones(CuentaBuzon unBuzon)
        {
            List<Acreditacion> listaAcreditaciones = new List<Acreditacion>();


            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                conn.Open();

                string query = "SELECT " +
                                    "IDBUZON,IDOPERACION,FECHA,IDBANCO,IDCUENTA,MONEDA,NO_ENVIADO,MONTO " +
                               "FROM " +
                                    "ACREDITACIONDEPOSITODIEGOTEST " +
                               "WHERE " +
                                    "IDBUZON = @nc " +
                               "AND " +
                                    "IDCUENTA = @idCuenta " +
                               "AND " +
                                    "FECHA >= @fecha " +
                               "AND " +
                                    "IDBANCO = @bankId " +
                               "AND MONEDA = @fuckingCoin " +
                               "ORDER BY " +
                                    "FECHA DESC";

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@nc", unBuzon.NC);

                cmd.Parameters.AddWithValue("@idCuenta", unBuzon.IdCuenta);

                cmd.Parameters.AddWithValue("@bankId", ServicioBanco.getInstancia().getByNombre(unBuzon.Banco).BancoId);

                cmd.Parameters.AddWithValue("@fecha", DateTime.Now.AddDays(-7).ToString("yyyyMMdd"));

                cmd.Parameters.AddWithValue("@fuckingCoin", unBuzon.getIdMoneda());

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {

                    while (rdr.Read())
                    {
                        Acreditacion acre = new Acreditacion
                        {
                            IdBuzon = rdr.GetString(0),
                            IdOperacion = rdr.GetInt64(1),
                            Fecha = rdr.GetDateTime(2),
                            IdBanco = rdr.GetInt32(3),
                            IdCuenta = rdr.GetInt32(4),
                            Moneda = rdr.GetInt32(5),
                            No_Enviado = rdr.GetBoolean(6),
                            Monto = rdr.GetDouble(7),
                        };

                        listaAcreditaciones.Add(acre);
                    }

                }

            }

            return listaAcreditaciones;
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
                query = @"select c.NC, 
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
                        query = @"select c.NC,
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
                        query = @"select c.NC,
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
                                and cb.idcliente not in(164)
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

                        int ultIdOperacion = await obtenerUltimaOperacionByNC(unaCuentaBuzon);

                        if (ultIdOperacion > 0)
                        {

                            if (!unaCuentaBuzon.Cierre.HasValue)
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

                generarExcelFormatoTanda(listaMontevideo, listaMaldonado, numTanda, bank);

            }
            catch (Exception e)
            {
                throw e;
            }
        }
        //METODO PARA OBTENER ACREDITACIONES(EXCEL , VA POR FECHA)
        private void getAcreditacionesPorBuzones(List<CuentaBuzon> listaCuentasBuzones, TimeSpan hasta, TimeSpan desde, Banco bank)
        {
            if (listaCuentasBuzones != null && listaCuentasBuzones.Count > 0)
            {
                try
                {

                    DateTime hastaEfectivo = DateTime.Today.Add(hasta);

                    DateTime desdeEfectivo = DateTime.Today.Add(desde);

                    using (SqlConnection conn = new SqlConnection(_conexionTSD))
                    {

                        conn.Open();

                        string query;

                        foreach (CuentaBuzon account in listaCuentasBuzones)
                        {

                            query = "select * " +
                                    "from acreditaciondepositodiegotest " +
                                    "where idbuzon = @accNC " +
                                    "and convert(date,fecha) = convert(date,getdate()) " +
                                    "and fecha >= @desde " +
                                    "and fecha <= @hasta " +
                                    "and idbanco = @bankId " +
                                    "and idcuenta = @accId";

                            SqlCommand cmd = new SqlCommand(query, conn);

                            cmd.Parameters.AddWithValue("@accNC", account.NC);

                            cmd.Parameters.AddWithValue("@hasta", hastaEfectivo);

                            cmd.Parameters.AddWithValue("@bankId", bank.BancoId);

                            cmd.Parameters.AddWithValue("@accId", account.IdCuenta);

                            cmd.Parameters.AddWithValue("@desde", desdeEfectivo);

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Acreditacion accreditation = new Acreditacion
                                    {
                                        Id = reader.GetInt32(0),
                                        IdBuzon = reader.GetString(1),
                                        IdOperacion = reader.GetInt64(2),
                                        Fecha = reader.GetDateTime(3),
                                        IdBanco = reader.GetInt32(4),
                                        IdCuenta = reader.GetInt32(5),
                                        Moneda = reader.GetInt32(6),
                                        No_Enviado = reader.GetBoolean(7),
                                        Monto = (float)reader.GetDouble(8) // Se lee como double y se convierte a float.
                                    };
                                    account.ListaAcreditaciones.Add(accreditation);
                                }
                            }

                        }

                    }
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception " + ex.Message);
                }
            }
            else
            {
                throw new Exception("Error en getAcreditacionesPorBuzones: ListaCuentaBuzones vacia o nula.");
            }
        }
        private void generarExcelPorCuentas(List<CuentaBuzon> listaCuentasBuzones, int numTanda, string city, Banco bank)

        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Acreditaciones");
                int currentRow = 1;

                // Escribir la fila de encabezados
                worksheet.Cell(currentRow, 1).Value = "Cliente";
                worksheet.Cell(currentRow, 2).Value = "Sucursal";
                worksheet.Cell(currentRow, 3).Value = "N° Cuenta";
                worksheet.Cell(currentRow, 4).Value = "Moneda";
                worksheet.Cell(currentRow, 5).Value = "Monto";

                // Estilos para el encabezado
                var headerRange = worksheet.Range(currentRow, 1, currentRow, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                currentRow++;

                // Separar listas por divisa
                var listaPesos = listaCuentasBuzones.Where(cb => cb.Moneda == "PESOS").ToList();
                var listaDolares = listaCuentasBuzones.Where(cb => cb.Moneda == "DOLARES").ToList();

                // ============================================================
                // PASO 1 y 2: AGRUPAR POR EMPRESA Y LUEGO POR NN
                // ============================================================
                var agrupadoPesos = listaPesos
                    .GroupBy(cb => cb.Empresa) // Agrupar primero por Empresa
                    .SelectMany(empresa => empresa
                        .GroupBy(cb => cb.NN) // Luego agrupar por NN
                        .Select(grupo => new
                        {

                            NN = grupo.Key,
                            Sucursal = grupo.First().SucursalCuenta,
                            Cuenta = grupo.First().Cuenta, // Obtener la cuenta
                            Moneda = grupo.First().Moneda, // Obtener la moneda
                            TotalMonto = grupo.Sum(cuenta => cuenta.ListaAcreditaciones?.Sum(a => a.Monto) ?? 0)
                        })
                    ).OrderBy(g => g.NN).ToList(); // PASO 3: Guardar en una lista ordenada alfabéticamente por NN

                var agrupadoDolares = listaDolares
                    .GroupBy(cb => cb.Empresa) // Agrupar primero por Empresa
                    .SelectMany(empresa => empresa
                        .GroupBy(cb => cb.NN) // Luego agrupar por NN
                        .Select(grupo => new
                        {
                            NN = grupo.Key,
                            Sucursal = grupo.First().SucursalCuenta,
                            Cuenta = grupo.First().Cuenta, // Obtener la cuenta
                            Moneda = grupo.First().Moneda, // Obtener la moneda
                            TotalMonto = grupo.Sum(cuenta => cuenta.ListaAcreditaciones?.Sum(a => a.Monto) ?? 0)
                        })
                    ).OrderBy(g => g.NN).ToList(); // Ordenado por NN

                // ============================================================
                // PASO 4: CREAR EL EXCEL PARA PESOS
                // ============================================================
                double totalPesos = 0;

                foreach (var grupo in agrupadoPesos)
                {
                    worksheet.Cell(currentRow, 1).Value = grupo.NN;
                    worksheet.Cell(currentRow, 2).Value = grupo.Sucursal;
                    worksheet.Cell(currentRow, 3).Value = grupo.Cuenta;
                    worksheet.Cell(currentRow, 4).Value = grupo.Moneda;
                    string montoFormateado = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(grupo.TotalMonto);
                    worksheet.Cell(currentRow, 5).Value = montoFormateado;
                    worksheet.Range(currentRow, 1, currentRow, 5).Style.Font.Bold = true;
                    worksheet.Range(currentRow, 1, currentRow, 5).Style.Fill.BackgroundColor = XLColor.WhiteSmoke;

                    totalPesos += grupo.TotalMonto;
                    currentRow++;
                }

                // AGREGAR TOTAL DE UYU
                worksheet.Cell(currentRow, 4).Value = "TOTAL UYU:";
                worksheet.Cell(currentRow, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(totalPesos);
                worksheet.Range(currentRow, 4, currentRow, 5).Style.Font.Bold = true;
                worksheet.Range(currentRow, 4, currentRow, 5).Style.Fill.BackgroundColor = XLColor.OrangePeel;
                currentRow += 2; // Separador entre PESOS y DÓLARES

                // ============================================================
                // CREAR EL EXCEL PARA DÓLARES
                // ============================================================
                double totalDolares = 0;

                foreach (var grupo in agrupadoDolares)
                {
                    worksheet.Cell(currentRow, 1).Value = grupo.NN;
                    worksheet.Cell(currentRow, 2).Value = grupo.Sucursal;
                    worksheet.Cell(currentRow, 3).Value = grupo.Cuenta;
                    worksheet.Cell(currentRow, 4).Value = grupo.Moneda;
                    string montoFormateado = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(grupo.TotalMonto);
                    worksheet.Cell(currentRow, 5).Value = montoFormateado;
                    worksheet.Range(currentRow, 1, currentRow, 5).Style.Font.Bold = true;
                    worksheet.Range(currentRow, 1, currentRow, 5).Style.Fill.BackgroundColor = XLColor.WhiteSmoke;

                    totalDolares += grupo.TotalMonto;
                    currentRow++;
                }

                // AGREGAR TOTAL DE USD
                worksheet.Cell(currentRow, 4).Value = "TOTAL USD:";
                worksheet.Cell(currentRow, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(totalDolares);
                worksheet.Range(currentRow, 4, currentRow, 5).Style.Font.Bold = true;
                worksheet.Range(currentRow, 4, currentRow, 5).Style.Fill.BackgroundColor = XLColor.OrangePeel;

                // Ajustar el ancho de las columnas
                worksheet.Columns().AdjustToContents();

                // ============================================================
                // NOMBRE DEL ARCHIVO SIN CARACTERES INVÁLIDOS
                // ============================================================
                string nombreArchivo = "Henderson_Tanda_" + numTanda + "_" + city + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";

                // Guardar el archivo
                string filePath = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\" + nombreArchivo;

                workbook.SaveAs(filePath);

                Console.WriteLine($"Excel generado: {filePath}");



                try
                {

                    string asunto = "Acreditaciones Henderson " + bank.NombreBanco + " Tanda " + numTanda + " - " + city;

                    string cuerpo = "A continuación se adjunta el archivo de acreditaciones correspondiente a la tanda " + numTanda + " de la ciudad de " + city + " para el banco " + bank.NombreBanco + ".";

                    _emailService.enviarExcelPorMail(filePath, asunto, cuerpo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al enviar el correo: " + ex.Message);
                }
            }
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
        private void generarExcelFormatoTanda(List<DtoAcreditacionesPorEmpresa> acreditacionesMontevideo, List<DtoAcreditacionesPorEmpresa> acreditacionesMaldonado, int numTanda, Banco b)
        {
            // Colores para el formato
            var pastelYellow = XLColor.LightYellow;

            var pastelCyan = XLColor.LightCyan;
            void InsertarLogoDesdeRecurso(IXLWorksheet ws, ref int row)
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourcePath = "ANS.Images.logoTecniFinal.png"; // Ajustá si tu namespace cambia

                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream != null)
                    {
                        // Insertamos en celda C1 (col 3) y aplicamos un pequeño offset
                        var imagen = ws.AddPicture(stream)
                                       .MoveTo(ws.Cell(row, 3), 30, 5) // Offset horizontal y vertical en píxeles
                                       .WithPlacement(XLPicturePlacement.FreeFloating)
                                       .Scale(0.5); // Escala ajustable

                        row += 3; // espacio debajo del logo
                    }
                }

                // Texto "BUZONES INTELIGENTES" centrado
                ws.Range(row, 1, row, 5).Merge();
                var celdaTitulo = ws.Cell(row, 1);
                celdaTitulo.Value = "BUZONES INTELIGENTES";
                celdaTitulo.Style.Font.Bold = true;
                celdaTitulo.Style.Font.FontSize = 16;
                celdaTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                celdaTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row += 2;
            }
            // Método interno para generar un Excel para una ciudad dada
            void GenerateExcel(List<DtoAcreditacionesPorEmpresa> lista, string ciudad)
            {


                var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add(ciudad);
                ws.ShowGridLines = false;
                int row = 1;
                InsertarLogoDesdeRecurso(ws, ref row);
                // --- Sección PESOS ---
                // Filtramos los registros de Pesos (puedes ajustar el filtro según el contenido de Moneda)
                var listaPesos = lista.Where(x => x.Moneda.ToUpper().Contains("PESO") || x.Moneda.ToUpper().Contains("UYU")).ToList();

                if (listaPesos.Any())
                {
                    // Encabezados
                    ws.Cell(row, 1).Value = "BUZON";
                    ws.Cell(row, 2).Value = "SUCURSAL";
                    ws.Cell(row, 3).Value = "CUENTA";
                    ws.Cell(row, 4).Value = "MONEDA";
                    ws.Cell(row, 5).Value = "MONTO";
                    var headerRange = ws.Range(row, 1, row, 5);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = pastelYellow;
                    row++;

                    // Datos
                    foreach (var item in listaPesos)
                    {
                        // En la columna BUZON se usa el atributo NN
                        ws.Cell(row, 1).Value = item.NN;
                        ws.Cell(row, 2).Value = item.Sucursal;
                        ws.Cell(row, 3).Value = item.NumeroCuenta;
                        ws.Cell(row, 4).Value = item.Moneda;
                        ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(item.Monto);
                        row++;
                    }

                    // Fila de Total para Pesos
                    double totalPesos = listaPesos.Sum(x => x.Monto);
                    ws.Cell(row, 4).Value = "TOTAL PESOS:";

                    ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(totalPesos);

                    var totalRange = ws.Range(row, 4, row, 5);
                    totalRange.Style.Fill.BackgroundColor = pastelCyan;
                    totalRange.Style.Font.Bold = true;
                    row += 2; // Espacio entre secciones
                }

                // --- Sección DÓLARES ---
                var listaDolares = lista.Where(x => x.Moneda.ToUpper().Contains("DOLAR") || x.Moneda.ToUpper().Contains("USD")).ToList();
                if (listaDolares.Any())
                {
                    // Encabezados
                    ws.Cell(row, 1).Value = "BUZON";
                    ws.Cell(row, 2).Value = "SUCURSAL";
                    ws.Cell(row, 3).Value = "CUENTA";
                    ws.Cell(row, 4).Value = "MONEDA";
                    ws.Cell(row, 5).Value = "MONTO";
                    var headerRangeD = ws.Range(row, 1, row, 5);
                    headerRangeD.Style.Font.Bold = true;
                    headerRangeD.Style.Fill.BackgroundColor = pastelYellow;
                    row++;

                    // Datos 
                    foreach (var item in listaDolares)
                    {
                        ws.Cell(row, 1).Value = item.NN;
                        ws.Cell(row, 2).Value = item.Sucursal;
                        ws.Cell(row, 3).Value = item.NumeroCuenta;
                        ws.Cell(row, 4).Value = item.Moneda;
                        ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(item.Monto);
                        row++;
                    }

                    // Fila de Total para Dólares
                    double totalDolares = listaDolares.Sum(x => x.Monto);
                    ws.Cell(row, 4).Value = "TOTAL DOLARES:";
                    ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(totalDolares);
                    var totalRangeD = ws.Range(row, 4, row, 5);
                    totalRangeD.Style.Fill.BackgroundColor = pastelCyan;
                    totalRangeD.Style.Font.Bold = true;
                    row++;
                }

                ws.Columns().AdjustToContents();

                string fileName = "";

                // Construir nombre y ruta del archivo
                
                if(b.NombreBanco.ToUpper() == VariablesGlobales.santander.ToUpper() || b.NombreBanco.ToUpper() == VariablesGlobales.scotiabank.ToUpper())
                {
                    fileName = $"{b.NombreBanco}_Henderson_{ciudad}_Tanda_{numTanda}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                }

                else if (b.NombreBanco.ToUpper() == VariablesGlobales.bbva.ToUpper())
                {
                    fileName = $"{b.NombreBanco}_{ciudad}_TATA_{numTanda}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                }
              

                string filePath = Path.Combine(@"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\", fileName);


                wb.SaveAs(filePath);
                Console.WriteLine($"Excel generado para {ciudad}: {filePath}");

                // Aquí podrías agregar el envío del correo, si corresponde.
                Console.WriteLine($"Excel de Tesorería generado: {filePath}");
                try
                {
                    string asunto = "";
                    string cuerpo = "";
                    if (b.NombreBanco.ToUpper() == VariablesGlobales.santander.ToUpper() || b.NombreBanco.ToUpper() == VariablesGlobales.scotiabank.ToUpper())
                    {
                        asunto = $"Acreditaciones  {b.NombreBanco} (HENDERSON)  Tanda {numTanda} - {ciudad.ToUpper()}";
                        cuerpo = $"A continuación se adjunta el archivo de acreditaciones correspondiente para {b.NombreBanco}(HENDERSON) siendo la Tanda {numTanda} de la ciudad de {ciudad.ToUpper()} para el banco {b.NombreBanco}.";
                    }
                    else if (b.NombreBanco.ToUpper() == VariablesGlobales.bbva.ToUpper())
                    {
                        asunto = $"Acreditaciones  {b.NombreBanco} (TATA) -  {ciudad.ToUpper()}";
                        cuerpo = $"A continuación se adjunta el archivo de acreditaciones correspondiente para {b.NombreBanco} para los buzones TATA de la ciudad de {ciudad.ToUpper()} para el banco {b.NombreBanco}.";
                    }
               
                    _emailService.enviarExcelPorMail(filePath, asunto, cuerpo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al enviar el correo: " + ex.Message);
                }
            }

            // Genera el Excel para Montevideo
            if (acreditacionesMontevideo != null && acreditacionesMontevideo.Any())
            {
                GenerateExcel(acreditacionesMontevideo, "MONTEVIDEO");
            }

            // Genera el Excel para Maldonado
            if (acreditacionesMaldonado != null && acreditacionesMaldonado.Any())
            {
                GenerateExcel(acreditacionesMaldonado, "MALDONADO");
            }
        }
        private void generarExcelPorAcreditacionesAgrupadoPorEmpresa(List<DtoAcreditacionesPorEmpresa> acreditacionesMontevideo, List<DtoAcreditacionesPorEmpresa> acreditacionesMaldonado, int numTanda, Banco b)
        {
            var amarilloPastel = XLColor.LightYellow;
            var celestePastel = XLColor.LightCyan;

            void InsertarLogoDesdeRecurso(IXLWorksheet ws, ref int row)
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourcePath = "ANS.Images.logoTecniFinal.png"; // Ajustá si tu namespace cambia

                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream != null)
                    {
                        // Insertamos en celda C1 (col 3) y aplicamos un pequeño offset
                        var imagen = ws.AddPicture(stream)
                                       .MoveTo(ws.Cell(row, 3), 30, 5) // Offset horizontal y vertical en píxeles
                                       .WithPlacement(XLPicturePlacement.FreeFloating)
                                       .Scale(0.5); // Escala ajustable

                        row += 6; // espacio debajo del logo
                    }
                }

                // Texto "BUZONES INTELIGENTES" centrado
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
                if (lista == null || !lista.Any())
                    return;

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
                        ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(item.Monto);

                        var dataRange = ws.Range(row, 1, row, 5);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        row++;
                    }

                    // Fila Total
                    ws.Cell(row, 4).Value = $"Total {moneda.ToUpper()}";
                    ws.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(datos.Sum(x => x.Monto));

                    var totalRange = ws.Range(row, 4, row, 5);
                    totalRange.Style.Fill.BackgroundColor = celestePastel;
                    totalRange.Style.Font.Bold = true;
                    totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    totalRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    row++;

                    // Fila separadora
                    var separador = ws.Range(row, 1, row, 5);
                    separador.Merge();

                    row++;
                }

                // Separar en pesos y dólares
                var listaPesos = lista.Where(x => x.Moneda.ToUpper() == "PESOS").ToList();
                var listaDolares = lista.Where(x => x.Moneda.ToUpper() == "DOLARES").ToList();

                AgregarSeccion(listaPesos, "Pesos");
                AgregarSeccion(listaDolares, "Dólares");

                ws.Columns().AdjustToContents();

                string nombreArchivo = $"Tesoreria_Tanda_{numTanda}_{ciudad.ToUpper()}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                string filePath = Path.Combine(@"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\", nombreArchivo);
                wb.SaveAs(filePath);

                Console.WriteLine($"Excel de Tesorería generado: {filePath}");
                try
                {
                    string asunto = $"Acreditaciones para Tesorería {b.NombreBanco} Tanda {numTanda} - {ciudad.ToUpper()}";
                    string cuerpo = $"E-Mail específico para TESORERÍA TECNISEGUR.\nA continuación se adjunta el archivo de acreditaciones correspondiente a la tanda {numTanda} de la ciudad de {ciudad.ToUpper()} para el banco {b.NombreBanco}.";
                    _emailService.enviarExcelPorMail(filePath, asunto, cuerpo);
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

            generarExcel("MONTEVIDEO", acreditacionesPesosMvd, acreditacionesDolaresMvd, banco);

            generarExcel("MALDONADO", acreditacionesPesosMaldonado, acreditacionesDolaresMaldonado, banco);

            await Task.CompletedTask;

        }
        private void generarExcel(string ciudad, List<DtoAcreditacionesPorEmpresa> listaPesos, List<DtoAcreditacionesPorEmpresa> listaDolares, Banco banco)
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

            string nombreArchivo = $"AcreditacionesDiaADia_{ciudad}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            string ruta = Path.Combine(@"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\", nombreArchivo);

            workbook.SaveAs(ruta);


            try
            {
                _emailService.enviarExcelPorMail(ruta, $"Acreditaciones Día a día - ({banco.NombreBanco}) - " + ciudad.ToUpper(),
                    $"Reporte de las acreditaciones realizadas día a día del banco: {banco.NombreBanco} (" + ciudad.ToUpper() + ")");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

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

                    row += 6; // Espacio debajo del logo
                }
            }

            // Texto "BUZONES INTELIGENTES" centrado
            ws.Range(row, 1, row, 5).Merge();
            var celdaTitulo = ws.Cell(row, 1);
            celdaTitulo.Value = "BUZONES INTELIGENTES";
            celdaTitulo.Style.Font.Bold = true;
            celdaTitulo.Style.Font.FontSize = 16;
            celdaTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            celdaTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            row += 2;
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
                        acc.MONEDA
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
                        config.TipoAcreditacion
                        ORDER BY cb.EMPRESA ASC";
            }

            if (banco.NombreBanco.ToUpper() == VariablesGlobales.scotiabank.ToUpper() && tipoAcreditacion.TipoAcreditacion.ToLower() == VariablesGlobales.diaxdia.ToLower())
            {
                query = @"SELECT 
                        cC.NN,
                        CB.EMPRESA,
                        cc.SUCURSAL as CIUDAD,
                        cb.CUENTA,
                        acc.MONEDA,
                        cb.SUCURSAL,
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
                        config.TipoAcreditacion 
                        ORDER BY cC.NN ASC";
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
            // Obtén la lista completa de acreditaciones del día
            Banco santander = ServicioBanco.getInstancia().getByNombre("SANTANDER");

            List<DtoAcreditacionesPorEmpresa> resumenAcreditaciones = getAcreditacionesDeHoy(santander);

            // Separa las acreditaciones en cuatro listas según divisa y ciudad
            List<DtoAcreditacionesPorEmpresa> resumenPesosMontevideo = new List<DtoAcreditacionesPorEmpresa>();
            List<DtoAcreditacionesPorEmpresa> resumenPesosMaldonado = new List<DtoAcreditacionesPorEmpresa>();
            List<DtoAcreditacionesPorEmpresa> resumenDolaresMontevideo = new List<DtoAcreditacionesPorEmpresa>();
            List<DtoAcreditacionesPorEmpresa> resumenDolaresMaldonado = new List<DtoAcreditacionesPorEmpresa>();

            foreach (DtoAcreditacionesPorEmpresa acreditacion in resumenAcreditaciones)
            {
                if (acreditacion.Divisa == 1)
                {
                    if (acreditacion.Ciudad.ToUpper() == VariablesGlobales.montevideo.ToUpper())
                        resumenPesosMontevideo.Add(acreditacion);
                    else
                        resumenPesosMaldonado.Add(acreditacion);
                }
                else // Asumimos que la única otra opción es Divisa == 2 (dólares)
                {
                    if (acreditacion.Ciudad.ToUpper() == VariablesGlobales.montevideo.ToUpper())
                        resumenDolaresMontevideo.Add(acreditacion);
                    else
                        resumenDolaresMaldonado.Add(acreditacion);
                }
            }

            // -------------------------- GENERAR EXCEL PARA MONTEVIDEO --------------------------
            using (var workbookMontevideo = new XLWorkbook())
            {
                var worksheet = workbookMontevideo.Worksheets.Add("Resumen");
                int row = 1;

                // SECCIÓN PESOS MONTEVIDEO
                worksheet.Cell(row, 1).Value = "CLIENTE";
                worksheet.Cell(row, 2).Value = "SUCURSAL";
                worksheet.Cell(row, 3).Value = "CUENTA";
                worksheet.Cell(row, 4).Value = "MONEDA";
                worksheet.Cell(row, 5).Value = "MONTO";
                var headerRange = worksheet.Range(row, 1, row, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                row++;

                foreach (var item in resumenPesosMontevideo)
                {
                    worksheet.Cell(row, 1).Value = item.Empresa;
                    worksheet.Cell(row, 2).Value = item.Sucursal;
                    worksheet.Cell(row, 3).Value = item.NumeroCuenta;
                    worksheet.Cell(row, 4).Value = item.Moneda;
                    worksheet.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(item.Monto);
                    worksheet.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(row, 1, row, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    row++;
                }

                worksheet.Cell(row, 1).Value = "Total Pesos:";
                double totalPesosMontevideo = resumenPesosMontevideo.Sum(x => x.Monto);
                worksheet.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(totalPesosMontevideo);
                worksheet.Range(row, 1, row, 5).Style.Font.Bold = true;
                worksheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.AliceBlue;
                worksheet.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range(row, 1, row, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                row++;

                row++; // Espacio entre secciones

                // SECCIÓN DÓLARES MONTEVIDEO
                worksheet.Cell(row, 1).Value = "CLIENTE";
                worksheet.Cell(row, 2).Value = "SUCURSAL";
                worksheet.Cell(row, 3).Value = "CUENTA";
                worksheet.Cell(row, 4).Value = "MONEDA";
                worksheet.Cell(row, 5).Value = "MONTO";
                var headerRangeUSD = worksheet.Range(row, 1, row, 5);
                headerRangeUSD.Style.Font.Bold = true;
                headerRangeUSD.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRangeUSD.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRangeUSD.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRangeUSD.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                row++;

                foreach (var item in resumenDolaresMontevideo)
                {
                    worksheet.Cell(row, 1).Value = item.Empresa;
                    worksheet.Cell(row, 2).Value = item.Sucursal;
                    worksheet.Cell(row, 3).Value = item.NumeroCuenta;
                    worksheet.Cell(row, 4).Value = item.Moneda;
                    worksheet.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(item.Monto);
                    worksheet.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(row, 1, row, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    row++;
                }

                worksheet.Cell(row, 1).Value = "Total Dólares:";
                double totalDolaresMontevideo = resumenDolaresMontevideo.Sum(x => x.Monto);
                worksheet.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(totalDolaresMontevideo);
                worksheet.Range(row, 1, row, 5).Style.Font.Bold = true;
                worksheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.AliceBlue;
                worksheet.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range(row, 1, row, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                worksheet.Columns().AdjustToContents();

                string nombreArchivoMontevideo = "ReporteDiario_Santander_Montevideo_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
                string filePathMontevideo = Path.Combine(@"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\", nombreArchivoMontevideo);
                workbookMontevideo.SaveAs(filePathMontevideo);

                try
                {
                    _emailService.enviarExcelPorMail(filePathMontevideo, "Reporte Diario Santander Montevideo", "Reporte diario de acreditaciones Santander - Montevideo");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            // -------------------------- GENERAR EXCEL PARA MALDONADO --------------------------
            using (var workbookMaldonado = new XLWorkbook())
            {
                var worksheet = workbookMaldonado.Worksheets.Add("Resumen");
                int row = 1;

                // SECCIÓN PESOS MALDONADO
                worksheet.Cell(row, 1).Value = "CLIENTE";
                worksheet.Cell(row, 2).Value = "SUCURSAL";
                worksheet.Cell(row, 3).Value = "CUENTA";
                worksheet.Cell(row, 4).Value = "MONEDA";
                worksheet.Cell(row, 5).Value = "MONTO";
                var headerRange = worksheet.Range(row, 1, row, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                row++;

                foreach (var item in resumenPesosMaldonado)
                {
                    worksheet.Cell(row, 1).Value = item.Empresa;
                    worksheet.Cell(row, 2).Value = item.Sucursal;
                    worksheet.Cell(row, 3).Value = item.NumeroCuenta;
                    worksheet.Cell(row, 4).Value = item.Moneda;
                    worksheet.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(item.Monto);
                    worksheet.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(row, 1, row, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    row++;
                }

                worksheet.Cell(row, 1).Value = "Total Pesos:";
                double totalPesosMaldonado = resumenPesosMaldonado.Sum(x => x.Monto);
                worksheet.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(totalPesosMaldonado);
                worksheet.Range(row, 1, row, 5).Style.Font.Bold = true;
                worksheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.AliceBlue;
                worksheet.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range(row, 1, row, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                row++;

                row++; // Espacio entre secciones

                // SECCIÓN DÓLARES MALDONADO
                worksheet.Cell(row, 1).Value = "CLIENTE";
                worksheet.Cell(row, 2).Value = "SUCURSAL";
                worksheet.Cell(row, 3).Value = "CUENTA";
                worksheet.Cell(row, 4).Value = "MONEDA";
                worksheet.Cell(row, 5).Value = "MONTO";
                var headerRangeUSD = worksheet.Range(row, 1, row, 5);
                headerRangeUSD.Style.Font.Bold = true;
                headerRangeUSD.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRangeUSD.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRangeUSD.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRangeUSD.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                row++;

                foreach (var item in resumenDolaresMaldonado)
                {
                    worksheet.Cell(row, 1).Value = item.Empresa;
                    worksheet.Cell(row, 2).Value = item.Sucursal;
                    worksheet.Cell(row, 3).Value = item.NumeroCuenta;
                    worksheet.Cell(row, 4).Value = item.Moneda;
                    worksheet.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(item.Monto);
                    worksheet.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(row, 1, row, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    row++;
                }

                worksheet.Cell(row, 1).Value = "Total Dólares:";
                double totalDolaresMaldonado = resumenDolaresMaldonado.Sum(x => x.Monto);
                worksheet.Cell(row, 5).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(totalDolaresMaldonado);
                worksheet.Range(row, 1, row, 5).Style.Font.Bold = true;
                worksheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.AliceBlue;
                worksheet.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range(row, 1, row, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                worksheet.Columns().AdjustToContents();

                string nombreArchivoMaldonado = "ReporteDiario_Santander_Maldonado_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
                string filePathMaldonado = Path.Combine(@"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\", nombreArchivoMaldonado);
                workbookMaldonado.SaveAs(filePathMaldonado);

                try
                {
                    _emailService.enviarExcelPorMail(filePathMaldonado, "Reporte Diario Santander Maldonado", "Reporte diario de acreditaciones Santander - Maldonado");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            await Task.Delay(100);
        }



    }
}