using ANS.Model.Interfaces;
using Microsoft.Data.SqlClient;
using ANS.Model.GeneradorArchivoPorBanco;
using ClosedXML.Excel;


namespace ANS.Model.Services
{
    public class ServicioCuentaBuzon : IServicioCuentaBuzon
    {
        private string _conexionTSD = ConfiguracionGlobal.ConexionTSD;
        private string _conexionWebBuzones = ConfiguracionGlobal.ConexionWebBuzones;
        public static ServicioCuentaBuzon instancia { get; set; }
        public static ServicioCuentaBuzon getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioCuentaBuzon();
            }
            return instancia;
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
        public void insertarLasUltimas40Operaciones()
        {
            List<CuentaBuzon> listaDeTodosLosBuzones = getAll();

            List<CuentaBuzon> listaDeTodosLosBuzonesQueTienenAcreditacionesRecientemente = new List<CuentaBuzon>();

            foreach (CuentaBuzon unBuzon in listaDeTodosLosBuzones)
            {
                unBuzon.ListaAcreditaciones = obtenerAcreditaciones(unBuzon) ?? new List<Acreditacion>();

                if (unBuzon.ListaAcreditaciones != null && unBuzon.ListaAcreditaciones.Count > 0)
                {
                    listaDeTodosLosBuzonesQueTienenAcreditacionesRecientemente.Add(unBuzon);
                }
            }

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {

                conn.Open();

                string query = "INSERT INTO AcreditacionDepositoDiegoTest (IDBUZON, IDOPERACION, FECHA, IDBANCO, IDCUENTA, MONEDA, MONTO) " +
                                     "VALUES (@idBuzon, @idOperacion, @fecha, @idBanco, @idCuenta, @moneda, @monto)";


                foreach (CuentaBuzon cb in listaDeTodosLosBuzonesQueTienenAcreditacionesRecientemente)
                {
                    foreach (Acreditacion a in cb.ListaAcreditaciones)
                    {
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@idBuzon", a.IdBuzon);
                        cmd.Parameters.AddWithValue("@idOperacion", a.IdOperacion);
                        cmd.Parameters.AddWithValue("@fecha", a.Fecha);
                        cmd.Parameters.AddWithValue("@idBanco", a.IdBanco);
                        cmd.Parameters.AddWithValue("@idCuenta", a.IdCuenta);
                        cmd.Parameters.AddWithValue("@moneda", a.Moneda);
                        cmd.Parameters.AddWithValue("@monto", a.Monto);
                        cmd.ExecuteNonQuery();
                    }
                }
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
                                    "ACREDITACIONESDEPOSITOS " +
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
                string query = "SELECT c.NC, cb.BANCO, c.CIERRE, c.IDCLIENTE, cb.CUENTA, cb.MONEDA, cb.EMPRESA, config.TipoAcreditacion,c.SUCURSAL as CIUDAD, cb.SUCURSAL, cb.TANDA, c.NN " +
                               "FROM ConfiguracionAcreditacion config " +
                               "INNER JOIN CUENTASBUZONES cb ON config.CuentasBuzonesId = cb.ID " +
                               "INNER JOIN CC c ON config.NC = c.NC " +
                               "WHERE config.TipoAcreditacion = @tipoAcreditacion " +
                               "AND config.CuentasBuzonesId IS NOT NULL " +
                               "AND config.ConfigId IS NOT NULL " +
                               "ORDER BY c.NC";

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

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {

                string query;

                // Si es banco santander , debe excluir a Henderson y De las sierras, (ID:164,268) 
                // porque estos fueron acreditados en tanda y en dia a dia a las 7am

                // ERROR , NO HACE FALTA,YA QUE FILTRO POR PUNTO A PUNTO.

                // SI ES DIA A DIA SOLAMENTE DEBE EXCLUIR A DELASSIERRAS ( ID 268 )
                if (banco.BancoId == 1 && tipoAcreditacion == VariablesGlobales.diaxdia)

                {
                    query = "SELECT c.NC, cb.BANCO, c.CIERRE, c.IDCLIENTE, cb.CUENTA, cb.MONEDA, cb.EMPRESA, config.TipoAcreditacion, c.SUCURSAL as CIUDAD, cb.SUCURSAL, c.IDCC, cb.ID, c.NN " +
                               "FROM " +
                               "ConfiguracionAcreditacion CONFIG" +
                               "INNER JOIN " +
                               "CUENTASBUZONES CB " +
                               "ON CB.ID = CONFIG.CuentasBuzonesId " +
                               "INNER JOIN " +
                               "CC C " +
                               "ON CB.IDCLIENTE = C.IDCLIENTE " +
                               "WHERE " +
                               "CONFIG.TipoAcreditacion = 'DiaADia' " +
                               "AND " +
                               "C.IDCLIENTE NOT IN('268') " +
                               "AND CB.banco = @bank ";
                }

                query = "SELECT c.NC, cb.BANCO, c.CIERRE, c.IDCLIENTE, cb.CUENTA, cb.MONEDA, cb.EMPRESA, config.TipoAcreditacion, c.SUCURSAL as CIUDAD, cb.SUCURSAL, c.IDCC, cb.ID, c.NN  " +
                                "FROM ConfiguracionAcreditacion config " +
                                "INNER JOIN CUENTASBUZONES cb ON config.CuentasBuzonesId = cb.ID " +
                                "INNER JOIN CC c ON config.NC = c.NC " +
                                "WHERE config.TipoAcreditacion = @tipoAcreditacion " +
                                "AND cb.BANCO = @bank " +
                                "AND config.CuentasBuzonesId IS NOT NULL " +
                                "AND config.ConfigId IS NOT NULL " +
                                "ORDER BY c.NC";

                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@tipoAcreditacion", tipoAcreditacion);

                cmd.Parameters.AddWithValue("@bank", banco.NombreBanco);

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
            SELECT 
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
                cb.ID 
            FROM CUENTASBUZONES cb
            INNER JOIN CC c ON cb.NC = c.NC
            WHERE cb.BANCO = @bank
            ORDER BY c.NC";

                conn.Open();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@bank", banco.NombreBanco);

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

            string query = @"SELECT
                                    c.NC AS NC_CC,
                                    cb.BANCO, 
                                    c.CIERRE, 
                                    c.IDCLIENTE, 
                                    cb.CUENTA, 
                                    cb.MONEDA, 
                                    cb.EMPRESA, 
                                    c.SUCURSAL as CIUDAD, 
                                    cb.SUCURSAL, 
                                    c.NN, 
                                    c.IDCC AS IDCC, 
                                    cb.ID AS ID, 
                                    config.TipoAcreditacion as CONFIGURACION 
                                FROM 
                                    ConfiguracionAcreditacion config 
                                INNER JOIN 
                                    CUENTASBUZONES cb on config.CuentasBuzonesId = cb.ID 
                                INNER JOIN 
                                    CC c ON config.NC = c.NC 
                                WHERE 
                                    cb.IDCLIENTE = @idcli 
                                AND 
                                    cb.BANCO = @bank;";

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                await conn.OpenAsync();

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idcli", idcliente);

                    cmd.Parameters.AddWithValue("@bank", bank.NombreBanco);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {

                        int ncOrdinal = reader.GetOrdinal("NC_CC");
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

            // Procesamos cada buzón
            foreach (CuentaBuzon unBuzon in buzones)
            {

                // Obtiene la última operación 
                ultIdOperacionPorBuzon = await obtenerUltimaOperacionByNC(unBuzon);

                // Procesa los depósitos para el buzón 
                try
                {
                    if (ultIdOperacionPorBuzon > 0)
                    {
                        await ServicioDeposito.getInstancia()
                            .asignarDepositosAlBuzon(unBuzon, ultIdOperacionPorBuzon, TimeSpan.Zero);
                    }
                }

                catch (Exception ex)
                {
                    // Si un buzón tiene un depósito con error, se registra y se continúa con el siguiente
                    Console.WriteLine($"Error asignando depósitos para buzón {unBuzon.NC}: {ex.Message}");

                }
            }

            // Genera el archivo si se procesaron buzones (incluso si algún buzón tuvo problemas, los demás se procesaron)
            await generarArchivoPorBanco(buzones, bank, VariablesGlobales.p2p);

            // Luego, inserta las acreditaciones para los depósitos que se asignaron correctamente

            await ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzones(buzones);
        }
        public async Task acreditarDiaADiaPorBanco(Banco banco)
        {

            List<CuentaBuzon> cuentaBuzones = getAllByTipoAcreditacionYBanco(VariablesGlobales.diaxdia, banco);

            if (cuentaBuzones != null && cuentaBuzones.Count > 0)
            {
                foreach (var unaCuentaBuzon in cuentaBuzones)
                {

                    int ultIdOperacion = await obtenerUltimaOperacionByNC(unaCuentaBuzon);

                    if (ultIdOperacion > 0)
                    {
                        await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(unaCuentaBuzon, ultIdOperacion, TimeSpan.Zero);
                    }

                    await generarArchivoPorBanco(cuentaBuzones, banco, VariablesGlobales.diaxdia);
                    //luego de generar, tiene que insertar en ACREDITACIONESDEPOSITO los depositos que fueron insertados ok
                    return;
                }
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

        public async Task acreditarTandaHendersonSantander(TimeSpan horaCierreActual)
        {
            if (horaCierreActual == TimeSpan.Zero)
            {
                throw new Exception("Error en acreditarTandaHendersonSantander: La hora de cierre actual no puede ser cero.");
            }
            Banco santander = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

            Cliente henderson = ServicioCliente.getInstancia().getById(164);

            List<CuentaBuzon> cuentaBuzonesHendersonSantander = await getCuentaBuzonesByClienteYBanco(henderson.IdCliente, santander);

            List<CuentaBuzon> listaCuentasFiltradasPorTipoAcreditacion = new List<CuentaBuzon>();

            if (henderson.Nombre.ToUpper().Contains("HENDER") && henderson.IdCliente == 164)
            {

                if (santander.NombreBanco.ToUpper().Contains(VariablesGlobales.santander.ToUpper()))
                {
                    try
                    {
                        foreach (CuentaBuzon unaCuenta in cuentaBuzonesHendersonSantander)
                        {
                            if (unaCuenta.Config.TipoAcreditacion == VariablesGlobales.tanda)
                            {
                                listaCuentasFiltradasPorTipoAcreditacion.Add(unaCuenta);
                            }
                        }

                        foreach (CuentaBuzon unaCuentaYaFiltrada in listaCuentasFiltradasPorTipoAcreditacion)
                        {
                            int ultimoIdOperacion = await obtenerUltimaOperacionByNC(unaCuentaYaFiltrada);

                            if (ultimoIdOperacion > 0)
                            {
                                await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(unaCuentaYaFiltrada, ultimoIdOperacion, TimeSpan.Zero);
                            }

                        }

                        if (listaCuentasFiltradasPorTipoAcreditacion.Count > 0)
                        {
                            {
                                await generarArchivoPorBanco(listaCuentasFiltradasPorTipoAcreditacion, santander, VariablesGlobales.tanda);

                                await ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzones(listaCuentasFiltradasPorTipoAcreditacion);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
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

            List<CuentaBuzon> cuentasFiltradasPorIdCliente = new List<CuentaBuzon>();

            if (cuentaBuzones != null && cuentaBuzones.Count > 0)
            {

                foreach (CuentaBuzon _oneAccount in cuentaBuzones)
                {
                    if (_oneAccount.IdCliente == cli.IdCliente)
                    {
                        cuentasFiltradasPorIdCliente.Add(_oneAccount);
                    }
                }

            }

            if (cuentasFiltradasPorIdCliente != null && cuentasFiltradasPorIdCliente.Count > 0)
            {

                foreach (CuentaBuzon cu in cuentasFiltradasPorIdCliente)
                {

                    int ultIdOperacion = await obtenerUltimaOperacionByNC(cu);

                    if (ultIdOperacion > 0)
                    {

                        await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(cu, ultIdOperacion, horaCierreActual);

                    }

                }



                await generarArchivoPorBanco(cuentasFiltradasPorIdCliente, bank, VariablesGlobales.tanda);

                await ServicioAcreditacion.getInstancia().crearAcreditacionesByListaCuentaBuzones(cuentasFiltradasPorIdCliente);

            }

        }
        //Enviar Excel genérico.
        public async Task enviarExcel(TimeSpan desde, TimeSpan hasta, Cliente cli, Banco bank)
        {


        }
        //Enviar Excel Específico para Henderson. (07:10)T1 (14:35)T2
        public async Task enviarExcelHenderson(TimeSpan desde, TimeSpan hasta, Cliente cli, Banco bank, string city, int numTanda)
        {

            List<CuentaBuzon> listaFiltradaPorCiudadYTanda = new List<CuentaBuzon>();

            List<CuentaBuzon> listaFiltradaPorCiudadYPorAcreditaciones = new List<CuentaBuzon>();

            if (cli.Nombre.ToUpper().Contains("HENDER") && cli.IdCliente == 164)
            {
                if (bank.NombreBanco.ToUpper().Contains(VariablesGlobales.santander.ToUpper()))
                {
                    List<CuentaBuzon> listaCuentasBuzones = await getCuentaBuzonesByClienteYBanco(cli.IdCliente, bank);

                    foreach (var unaCuenta in listaCuentasBuzones)
                    {
                        if (unaCuenta.Ciudad.ToUpper() == city.ToUpper())
                        {
                            if (unaCuenta.Config.TipoAcreditacion == VariablesGlobales.tanda)
                            {
                                listaFiltradaPorCiudadYTanda.Add(unaCuenta);
                            }

                        }
                    }
                    getAcreditacionesPorBuzones(listaFiltradaPorCiudadYTanda, hasta, desde, bank);

                    foreach (var unaCuentaFiltradaPorCiudad in listaFiltradaPorCiudadYTanda)
                    {
                        if (unaCuentaFiltradaPorCiudad.ListaAcreditaciones != null && unaCuentaFiltradaPorCiudad.ListaAcreditaciones.Count > 0)
                        {
                            listaFiltradaPorCiudadYPorAcreditaciones.Add(unaCuentaFiltradaPorCiudad);
                        }
                    }

                    generarExcelPorCuentas(listaFiltradaPorCiudadYPorAcreditaciones, numTanda, city);

                }
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
        private void generarExcelPorCuentas(List<CuentaBuzon> listaCuentasBuzones, int numTanda, string city)
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
        public async Task enviarExcelTesoreria(Banco santander, List<Cliente> clientes, string city, int numTanda, TimeSpan hasta, TimeSpan desde)
        {

            List<CuentaBuzon> listaFiltradaPorCiudadYTanda = new List<CuentaBuzon>();

            List<CuentaBuzon> listaFiltradaPorCiudadYPorAcreditaciones = new List<CuentaBuzon>();

            List<CuentaBuzon> listaCuentasBuzones = new List<CuentaBuzon>();

            foreach (Cliente unCliente in clientes)
            {
                listaCuentasBuzones = await getCuentaBuzonesByClienteYBanco(unCliente.IdCliente, santander);
            }

            foreach (var unaCuenta in listaCuentasBuzones)
            {
                if (unaCuenta.Ciudad.ToUpper() == city.ToUpper())
                {
                    if (unaCuenta.Config.TipoAcreditacion == VariablesGlobales.tanda || unaCuenta.Config.TipoAcreditacion == VariablesGlobales.diaxdia)
                    {
                        listaFiltradaPorCiudadYTanda.Add(unaCuenta);
                    }

                }
            }

            getAcreditacionesPorBuzones(listaFiltradaPorCiudadYTanda, hasta, desde, santander);

            foreach (var unaCuentaFiltradaPorCiudad in listaFiltradaPorCiudadYTanda)
            {
                if (unaCuentaFiltradaPorCiudad.ListaAcreditaciones != null && unaCuentaFiltradaPorCiudad.ListaAcreditaciones.Count > 0)
                {
                    listaFiltradaPorCiudadYPorAcreditaciones.Add(unaCuentaFiltradaPorCiudad);
                }
            }

            generarExcelPorEmpresas(listaFiltradaPorCiudadYPorAcreditaciones, numTanda, city);

        }
        private void generarExcelPorEmpresas(List<CuentaBuzon> cuentas, int numTanda, string city)
        {
            // Agrupar por empresa y sumar los montos por moneda
            var agrupadoTesoreria = cuentas
                 .GroupBy(cb => cb.Empresa)
                 .Select(g => new
                 {
                     Empresa = g.Key,
                     TotalPesos = g.Where(cb => cb.Moneda.Equals("PESOS", StringComparison.OrdinalIgnoreCase))
                                   .Sum(cb => cb.ListaAcreditaciones?.Sum(a => a.Monto) ?? 0),
                     TotalUSD = g.Where(cb => cb.Moneda.Equals("DOLARES", StringComparison.OrdinalIgnoreCase))
                                 .Sum(cb => cb.ListaAcreditaciones?.Sum(a => a.Monto) ?? 0)
                 })
                 .OrderBy(x => x.Empresa)
                 .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Santander 15:30 Día a Día");
                int currentRow = 1;

                // Escribir encabezados
                worksheet.Cell(currentRow, 1).Value = "Empresa";
                worksheet.Cell(currentRow, 2).Value = "Total PESOS";
                worksheet.Cell(currentRow, 3).Value = "Total USD";

                // Estilos para encabezado
                var headerRange = worksheet.Range(currentRow, 1, currentRow, 3);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                currentRow++;

                // Rellenar la información agrupada
                foreach (var grupo in agrupadoTesoreria)
                {
                    worksheet.Cell(currentRow, 1).Value = grupo.Empresa;
                    // Se asume que el método FormatearDoubleConPuntosYComas formatea el monto a string
                    worksheet.Cell(currentRow, 2).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(grupo.TotalPesos);
                    worksheet.Cell(currentRow, 3).Value = ServicioUtilidad.getInstancia().FormatearDoubleConPuntosYComas(grupo.TotalUSD);
                    currentRow++;
                }

                // Ajustar ancho de columnas para mejor visualización
                worksheet.Columns().AdjustToContents();

                // Construir el nombre del archivo y ruta de guardado
                string nombreArchivo = "Tesoreria_Tanda_" + numTanda + "_" + city + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
                string filePath = @"C:\Users\dchiquiar.ABUDIL\Desktop\ANS TEST\EXCEL\" + nombreArchivo;

                workbook.SaveAs(filePath);
                Console.WriteLine($"Excel de Tesorería generado: {filePath}");
            }
        }
        public async Task enviarExcelSantanderDiaADia(string city, Banco banco, ConfiguracionAcreditacion tipoAcreditacion)
        {

            List<CuentaBuzon> cuentasFiltradasPorTipoAcreditacionYBanco = getAllByTipoAcreditacionYBanco(tipoAcreditacion.TipoAcreditacion, banco);

            List<CuentaBuzon> cuentasFiltradasTambienPorCiudad = new List<CuentaBuzon>();

            if (cuentasFiltradasPorTipoAcreditacionYBanco != null && cuentasFiltradasPorTipoAcreditacionYBanco.Count > 0)
            {
                foreach (CuentaBuzon cuentaBuzon in cuentasFiltradasPorTipoAcreditacionYBanco)
                {
                    if (cuentaBuzon.Ciudad.ToUpper().Equals(city.ToUpper()))
                    {

                        cuentasFiltradasTambienPorCiudad.Add(cuentaBuzon);

                    }
                }
            }

            generarExcelPorEmpresas(cuentasFiltradasTambienPorCiudad, 0, city);

        }
    }
}