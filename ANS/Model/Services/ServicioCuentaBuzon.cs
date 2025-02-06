using ANS.Model.Interfaces;
using Microsoft.Data.SqlClient;
using ANS.Model.GeneradorArchivoPorBanco;
using System;


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

                string query = "SELECT buzon.NC,buzon.BANCO,buzon.CIERRE,buzon.IDCLIENTE,cuentabuzon.CUENTA,cuentabuzon.MONEDA,cuentabuzon.EMPRESA,config.TipoAcreditacion, config.ParametroDiaADia, config.ParametroPuntoAPunto, config.ParametroTanda1, config.ParametroTanda2 " +
                    "FROM CC buzon INNER JOIN CUENTASBUZONES cuentabuzon ON buzon.IDCLIENTE = cuentabuzon.IDCLIENTE " +
                    "INNER JOIN ConfiguracionAcreditacion config ON cuentabuzon.ID = config.CuentasBuzonesId " +
                    "WHERE buzon.BANCO IS NOT NULL " +
                    "AND LTRIM(RTRIM(buzon.BANCO)) <> '' " +
                    "AND buzon.BANCO <> 'SIN ASIGNAR' " +
                    "ORDER BY buzon.BANCO DESC";

                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CuentaBuzon cuentaBuzon = new CuentaBuzon()
                        {

                        };

                    }
                }

            }

            return new List<CuentaBuzon>();
            /*
                 SELECT buzon.NC,buzon.BANCO,buzon.CIERRE,buzon.IDCLIENTE,cuentabuzon.CUENTA,cuentabuzon.MONEDA,cuentabuzon.EMPRESA,config.TipoAcreditacion,config.ParametroDiaADia,config.ParametroPuntoAPunto,config.ParametroTanda1,config.ParametroTanda2
                 FROM CC buzon
                 INNER JOIN CUENTASBUZONES cuentabuzon ON buzon.IDCLIENTE = cuentabuzon.IDCLIENTE
                 INNER JOIN ConfiguracionAcreditacion config ON cuentabuzon.ID = config.CuentasBuzonesId
                 WHERE buzon.BANCO IS NOT NULL
                 AND LTRIM(RTRIM(buzon.BANCO)) <> ''
                 AND buzon.BANCO <> 'SIN ASIGNAR'
                 ORDER BY buzon.BANCO DESC
            */
        }
        public List<CuentaBuzon> getAllByTipoAcreditacion(string tipoAcreditacion)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                string query = "SELECT c.NC, cb.BANCO, c.CIERRE, c.IDCLIENTE, cb.CUENTA, cb.MONEDA, cb.EMPRESA, config.TipoAcreditacion,c.SUCURSAL as CIUDAD, cb.SUCURSAL, cb.TANDA " +
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
                            Producto = reader.GetInt32(tandaOrdinal)
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
                if (banco.NombreBanco == VariablesGlobales.santander)
                {
                    query = "SELECT c.NC, cb.BANCO, c.CIERRE, c.IDCLIENTE, cb.CUENTA, cb.MONEDA, cb.EMPRESA, config.TipoAcreditacion, c.SUCURSAL as CIUDAD, cb.SUCURSAL, c.IDCC, cb.ID " +
                            "FROM ConfiguracionAcreditacion config " +
                            "INNER JOIN CUENTASBUZONES cb ON config.CuentasBuzonesId = cb.ID " +
                            "INNER JOIN CC c ON config.NC = c.NC " +
                            "WHERE config.TipoAcreditacion = @tipoAcreditacion " +
                            "AND cb.BANCO = @bank " +
                            "AND config.CuentasBuzonesId IS NOT NULL " +
                            "AND config.ConfigId IS NOT NULL " +
                            "AND cb.EMPRESA NOT IN (164, 268) " +
                            "ORDER BY c.NC";
                }
                else
                {
                    query = "SELECT c.NC, cb.BANCO, c.CIERRE, c.IDCLIENTE, cb.CUENTA, cb.MONEDA, cb.EMPRESA, config.TipoAcreditacion, c.SUCURSAL as CIUDAD, cb.SUCURSAL, c.IDCC, cb.ID " +
                            "FROM ConfiguracionAcreditacion config " +
                            "INNER JOIN CUENTASBUZONES cb ON config.CuentasBuzonesId = cb.ID " +
                            "INNER JOIN CC c ON config.NC = c.NC " +
                            "WHERE config.TipoAcreditacion = @tipoAcreditacion " +
                            "AND cb.BANCO = @bank " +
                            "AND config.CuentasBuzonesId IS NOT NULL " +
                            "AND config.ConfigId IS NOT NULL " +
                            "ORDER BY c.NC";
                }


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
                                IdCuenta = reader.GetInt32(idCuentaOrdinal)
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
        public List<CuentaBuzon> getCuentaBuzonesByCliente(int cli)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {

                string query;

                    query = @"SELECT 
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
                                    WHERE cb.IDCLIENTE = @id
                                    ORDER BY c.NC";
               

                conn.Open();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", cli);

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
                                IdCuenta = reader.GetInt32(idCuentaOrdinal)
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
        //METODO PROCESAR DEPOSITOS DE CUENTAS PUNTO A PUNTO BBVA(TODA HORA 15 Y 45) Y SANTANDER(5MIN)
        //Método Acreditar punto a punto para Santander (5 mins)
        #region MÉTODOS ACREDITAR POR CONFIGURACIÓN!
        public async Task acreditarPuntoAPuntoPorBanco(Banco bank)
        {
            int ultIdOperacionPorBuzon = 0;
            List<CuentaBuzon> buzones = getAllByTipoAcreditacionYBanco(VariablesGlobales.p2p, bank);

            if (buzones != null && buzones.Count > 0)
            {

                foreach (CuentaBuzon unBuzon in buzones)
                {
                    ultIdOperacionPorBuzon = obtenerUltimaOperacionByNC(unBuzon.NC, unBuzon.IdCuenta);

                    if (ultIdOperacionPorBuzon > 0)
                    {
                        await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(unBuzon, ultIdOperacionPorBuzon, TimeSpan.Zero);
                    }

                }
                await generarArchivoPorBanco(buzones, bank, VariablesGlobales.p2p);

                //luego de generar, tiene que insertar en ACREDITACIONESDEPOSITO los depositos que fueron insertados ok

                return;

            }

            throw new Exception("No se encontaron buzones punto a punto");

        }
        public async Task acreditarDiaADiaPorBanco(Banco banco)
        {

            List<CuentaBuzon> cuentaBuzones = getAllByTipoAcreditacionYBanco(VariablesGlobales.diaxdia, banco);

            if (cuentaBuzones != null && cuentaBuzones.Count > 0)
            {
                foreach (var unaCuentaBuzon in cuentaBuzones)
                {

                    int ultIdOperacion = this.obtenerUltimaOperacionByNC(unaCuentaBuzon.NC, unaCuentaBuzon.IdCuenta);

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

                    int ultIdOperacion = this.obtenerUltimaOperacionByNC(unaCuentaBuzon.NC, unaCuentaBuzon.IdCuenta);

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
        private int obtenerUltimaOperacionByNC(string nc, int idCuenta)
        {
            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                string query = "select max(idoperacion) from ACREDITACIONESDEPOSITOS where IDBUZON = @ncFound and IDCUENTA = @idCuenta";

                if (nc == "GRAMAR")
                {
                    Console.Write("ESTA ES!");
                }

                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@ncFound", nc);

                cmd.Parameters.AddWithValue("@idCuenta", idCuenta);

                object result = cmd.ExecuteScalar();

                return result != DBNull.Value && result != null ? Convert.ToInt32(result) : 0;
            }
        }
        public async Task acreditarTanda1HendersonSantander(TimeSpan horaCierreActual)
        {

        }
        public async Task acreditarTanda2HendersonSantander(TimeSpan horaCierreActual)
        {

        }
        public async Task acretidarPorBanco(Banco bank, TimeSpan horaCierre)
        {

            //Este metodo por lo general acredita a la hora del cierre del banco parámetro.

            List<CuentaBuzon> buzonesPorBanco = getAllByBanco(bank);

            if (buzonesPorBanco != null && buzonesPorBanco.Count > 0)
            {

                foreach (CuentaBuzon _buzon in buzonesPorBanco)
                {
                    int ultIdOperacion = this.obtenerUltimaOperacionByNC(_buzon.NC, _buzon.IdCuenta);

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
        public async Task acreditarDiaADiaPorCliente(string nombreCliente, Banco bank, TimeSpan horaCierreActual)
        {
            if (string.IsNullOrEmpty(nombreCliente))
            {
                throw new Exception("Error en método acreditarDiaADiaPorCliente . Nombre Cliente vacío");
            }


            int idClienteFound = getIdClienteByNombre(nombreCliente);

            if (idClienteFound > 0)
            {

                List<CuentaBuzon> cuentaBuzones = getCuentaBuzonesByCliente(idClienteFound);

                if (cuentaBuzones != null && cuentaBuzones.Count > 0)
                {
                    foreach (CuentaBuzon cu in cuentaBuzones)
                    {

                        int ultIdOperacion = this.obtenerUltimaOperacionByNC(cu.NC, cu.IdCuenta);

                        if (ultIdOperacion > 0)
                        {

                            await ServicioDeposito.getInstancia().asignarDepositosAlBuzon(cu, ultIdOperacion, horaCierreActual);

                        }

                    }
                    await generarArchivoPorBanco(cuentaBuzones, bank, VariablesGlobales.diaxdia);
                }

                //obtener cuentabuzones del cliente obtenido y buscar depositos

            }
            else throw new Exception("Error en acreditarDiaADiaPorCliente: No se encontró idcliente para el nombre: " + nombreCliente);
        }
        private int getIdClienteByNombre(string nombreCliente)
        {
            if (nombreCliente != null)
            {

                using (SqlConnection conn = new SqlConnection(_conexionTSD))

                {

                    string query = @"select IDCLIENTE
                                     from clientes
                                     where nombre like '%@nombreCliente%'";

                    conn.Open();

                    SqlCommand cmd = new SqlCommand(query, conn);

                    return cmd.ExecuteNonQuery();
                }

            }
            return 0;
        }
        public async Task enviarExcel(TimeSpan desde, TimeSpan hasta, Cliente cli, Banco bank)
        {

            List<CuentaBuzon> listaCuentasBuzones = new List<CuentaBuzon>();

            if (bank.NombreBanco.ToUpper() == VariablesGlobales.santander)
            {

                if (cli.Nombre.Contains("HENDERS")) // Henderson
                {
                    //obtener los buzones de henderson y luego ir a la tabhla ACREDITACIONESDEPOSITOS por ese id. traer todos 
                    // filtrar por fecha desde hasta , y bank.
                    listaCuentasBuzones = getCuentaBuzonesByCliente(cli.IdCliente);
                   
                    return;

                }

                listaCuentasBuzones = getAllByBanco(bank);

            }

        }
    }
}