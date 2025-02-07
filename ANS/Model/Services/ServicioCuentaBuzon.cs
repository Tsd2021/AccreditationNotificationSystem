using ANS.Model.Interfaces;
using Microsoft.Data.SqlClient;
using ANS.Model.GeneradorArchivoPorBanco;
using System;
using Microsoft.Web.Services3.Referral;
using ClosedXML.Excel;
using System.IO;


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
                if (banco.NombreBanco == VariablesGlobales.santander)
                {
                    query = "SELECT c.NC, cb.BANCO, c.CIERRE, c.IDCLIENTE, cb.CUENTA, cb.MONEDA, cb.EMPRESA, config.TipoAcreditacion, c.SUCURSAL as CIUDAD, cb.SUCURSAL, c.IDCC, cb.ID, c.NN  " +
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
                    query = "SELECT c.NC, cb.BANCO, c.CIERRE, c.IDCLIENTE, cb.CUENTA, cb.MONEDA, cb.EMPRESA, config.TipoAcreditacion, c.SUCURSAL as CIUDAD, cb.SUCURSAL, c.IDCC, cb.ID, c.NN  " +
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
        public List<CuentaBuzon> getCuentaBuzonesByClienteYBanco(int idcliente,Banco bank)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {

                string query;

                query = @"SELECT 
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
                        cb.ID AS ID 
                        FROM CUENTASBUZONES cb
                        INNER JOIN CC c ON cb.IDCLIENTE = c.IDCLIENTE
                        WHERE cb.IDCLIENTE = @idcli
                        AND cb.BANCO = @bank
                        ;";


                conn.Open();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idcli", idcliente);
                    cmd.Parameters.AddWithValue("@bank", bank.NombreBanco);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // Obtener los índices de las columnas
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
                                IdReferenciaAlCliente = reader.GetString(idReferenciaOrdinal),
                                IdCuenta = reader.GetInt32(idCuentaOrdinal),
                                NN = reader.GetString(nnOrdinal),
                                Ciudad = reader.GetString(sucursalCiudadOrdinal)
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
        public async Task acreditarDiaADiaPorCliente(Cliente cli, Banco bank, TimeSpan horaCierreActual)
        {
            if (cli == null)
            {
                throw new Exception("Error en método acreditarDiaADiaPorCliente. Cliente null");
            }

            List<CuentaBuzon> cuentaBuzones = getCuentaBuzonesByClienteYBanco(cli.IdCliente,bank);

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
        //Enviar Excel genérico.
        public async Task enviarExcel(TimeSpan desde, TimeSpan hasta, Cliente cli, Banco bank)
        {


        }
        //Enviar Excel Específico para Henderson. (07:10)T1 (14:35)T2
        public async Task enviarExcelHenderson(TimeSpan desde, TimeSpan hasta, Cliente cli, Banco bank)
        {
            if (cli.Nombre.ToUpper().Contains("HENDER") && cli.IdCliente == 164)
            {
                if (bank.NombreBanco.ToUpper().Contains(VariablesGlobales.santander.ToUpper()))
                {
                    List<CuentaBuzon> listaCuentasBuzones = getCuentaBuzonesByClienteYBanco(cli.IdCliente,bank);

                    getAcreditacionesPorBuzones(listaCuentasBuzones, desde, hasta, bank);

                    generarExcelPorCuentas(listaCuentasBuzones);

                }
            }
        }
        //METODO PARA OBTENER ACREDITACIONES(EXCEL , VA POR FECHA)
        private void getAcreditacionesPorBuzones(List<CuentaBuzon> listaCuentasBuzones, TimeSpan desde, TimeSpan hasta, Banco bank)
        {
            if (listaCuentasBuzones != null && listaCuentasBuzones.Count > 0)
            {

                // Convertimos los TimeSpan en un rango DateTime efectivo según la lógica definida.
                (DateTime effectiveDesde, DateTime effectiveHasta) = ObtenerDateTimeEfectivos(desde, hasta);

                using (SqlConnection conn = new SqlConnection(_conexionTSD))
                {

                    conn.Open();

                    string query;

                    foreach (CuentaBuzon account in listaCuentasBuzones)
                    {

                        query = "select * " +
                                "from acreditacionesdepositos " +
                                "where idbuzon = @accNC " +
                                "and fecha between @desde and @hasta " +
                                "and idbanco = @bankId " +
                                "and idcuenta = @accId";

                        SqlCommand cmd = new SqlCommand(query, conn);

                        cmd.Parameters.AddWithValue("@accNC", account.NC);
                        cmd.Parameters.AddWithValue("@desde", effectiveDesde);
                        cmd.Parameters.AddWithValue("@hasta", effectiveHasta);
                        cmd.Parameters.AddWithValue("@bankId", bank.BancoId);
                        cmd.Parameters.AddWithValue("@accId", account.IdCuenta);

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
            else
            {
                throw new Exception("Error en getAcreditacionesPorBuzones: ListaCuentaBuzones vacia o nula.");
            }
        }
        private void generarExcelPorCuentas(List<CuentaBuzon> listaCuentasBuzones)
        {
            // Filtrar cuentas según la divisa
            List<CuentaBuzon> listaPesos = listaCuentasBuzones
                                            .Where(cb => cb.Divisa == VariablesGlobales.pesos)
                                            .ToList();
            List<CuentaBuzon> listaDolares = listaCuentasBuzones
                                            .Where(cb => cb.Divisa == VariablesGlobales.dolares)
                                            .ToList();

            // Crear un nuevo libro de Excel y agregar una hoja
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Acreditaciones");

                int currentRow = 1;

                // Escribir la fila de encabezados (por ejemplo, para ambas secciones)
                worksheet.Cell(currentRow, 1).Value = "Cliente";
                worksheet.Cell(currentRow, 2).Value = "Sucursal";
                worksheet.Cell(currentRow, 3).Value = "Cuenta";
                worksheet.Cell(currentRow, 4).Value = "Moneda";
                worksheet.Cell(currentRow, 5).Value = "Monto";
                worksheet.Cell(currentRow, 6).Value = "Fecha";

                // Estilos para el encabezado
                var headerRange = worksheet.Range(currentRow, 1, currentRow, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                double totalPesos = 0;

                // Escribir las filas de cuentas en pesos
                foreach (var cuenta in listaPesos)
                {
                    foreach (var acreditacion in cuenta.ListaAcreditaciones)
                    {
                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = cuenta.NN;
                        worksheet.Cell(currentRow, 2).Value = cuenta.SucursalCuenta;
                        worksheet.Cell(currentRow, 3).Value = cuenta.Cuenta;
                        worksheet.Cell(currentRow, 4).Value = cuenta.Divisa;
                        worksheet.Cell(currentRow, 5).Value = acreditacion.Monto;
                        worksheet.Cell(currentRow, 6).Value = acreditacion.Fecha;
                        totalPesos += acreditacion.Monto;
                    }
                }

                // Agregar fila de total para pesos
                currentRow++;
                worksheet.Cell(currentRow, 4).Value = "Total Pesos:";
                worksheet.Cell(currentRow, 5).Value = totalPesos;

                // Agregar una fila vacía para separar las secciones
                currentRow++;

                double totalDolares = 0;

                // Escribir las filas de cuentas en dólares
                foreach (var cuenta in listaDolares)
                {
                    foreach (var acreditacion in cuenta.ListaAcreditaciones)
                    {
                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = cuenta.NN;
                        worksheet.Cell(currentRow, 2).Value = cuenta.SucursalCuenta;
                        worksheet.Cell(currentRow, 3).Value = cuenta.Cuenta;
                        worksheet.Cell(currentRow, 4).Value = cuenta.Divisa;
                        worksheet.Cell(currentRow, 5).Value = acreditacion.Monto;
                        worksheet.Cell(currentRow, 6).Value = acreditacion.Fecha;
                        totalDolares += acreditacion.Monto;
                    }
                }

                // Agregar fila de total para dólares
                currentRow++;
                worksheet.Cell(currentRow, 4).Value = "Total USD:";
                worksheet.Cell(currentRow, 5).Value = totalDolares;

                // Ajustar el ancho de las columnas para que se vean los datos
                worksheet.Columns().AdjustToContents();

                string filePath = @"C:\Users\dchiquiar\Desktop\EXCEL TEST\EXCEL_TESTacreditaciones.xlsx";
                workbook.SaveAs(filePath);

                Console.WriteLine($"Excel generado: {filePath}");
            }
        }
        private (DateTime effectiveDesde, DateTime effectiveHasta) ObtenerDateTimeEfectivos(TimeSpan desde, TimeSpan hasta)
        {

            // Obtenemos la fecha de hoy sin la hora.
            DateTime today = DateTime.Today;

            // Caso especial: Job diario, donde la hora de corte es la misma (por ejemplo, 16:30).
            // Entonces se debe acreditar desde:
            //    - Si hoy es lunes: desde el viernes a las 16:30
            //    - Si no es lunes: desde el día anterior a las 16:30
            // Hasta: hoy a las 16:30.
            if (desde == hasta)
            {
                DateTime effectiveDesde = (today.DayOfWeek == DayOfWeek.Monday
                                           ? today.AddDays(-3)
                                           : today.AddDays(-1)).Add(desde);
                DateTime effectiveHasta = today.Add(desde);
                return (effectiveDesde, effectiveHasta);
            }
            // Caso 1: Rango sin cruce de medianoche (Tanda 2)
            // Ejemplo: desde = 07:00 y hasta = 15:30.
            // Se toma el rango del día actual.
            else if (desde < hasta)
            {
                DateTime effectiveDesde = today.Add(desde);
                DateTime effectiveHasta = today.Add(hasta);
                return (effectiveDesde, effectiveHasta);
            }
            // Caso 2: Rango que cruza la medianoche (Tanda 1)
            // Ejemplo: desde = 15:30 y hasta = 07:00.
            // Se toma:
            //    - effectiveDesde: día anterior (o viernes si hoy es lunes) a las 15:30.
            //    - effectiveHasta: hoy a las 07:00.
            else
            {
                DateTime effectiveDesde = (today.DayOfWeek == DayOfWeek.Monday
                                           ? today.AddDays(-3)
                                           : today.AddDays(-1)).Add(desde);
                DateTime effectiveHasta = today.Add(hasta);
                return (effectiveDesde, effectiveHasta);
            }

        }

    }
}