using ANS.Model.Interfaces;
using Microsoft.Data.SqlClient;

namespace ANS.Model.Services
{
    public class ServicioAcreditacion : IServicioAcreditacion
    {
        private string _conexionTSD = ConfiguracionGlobal.Conexion22;

        public static ServicioAcreditacion instance { get; set; }
        public static ServicioAcreditacion getInstancia()
        {
            if (instance == null)
            {
                instance = new ServicioAcreditacion();
            }
            return instance;
        }
        public void insertar(Acreditacion a)
        {
            if (a == null) return;

            // Determinamos la fecha que vamos a insertar
            DateTime fechaParaInsertar =
                a.FechaTanda != DateTime.MinValue
                ? a.FechaTanda
                : DateTime.Now;

            using (var conn = new SqlConnection(_conexionTSD))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =   @"
                                    IF NOT EXISTS (
                                    SELECT 1
                                    FROM AcreditacionDepositoDiegoTest
                                    WHERE IDBUZON     = @IDBUZON
                                    AND IDOPERACION = @IDOPERACION
                                    AND MONEDA      = @MONEDA
                                    AND IDCUENTA    = @IDCUENTA
                                    )
                                    BEGIN
                                    INSERT INTO AcreditacionDepositoDiegoTest
                                    (IDBUZON, IDOPERACION, FECHA, IDBANCO, IDCUENTA, MONEDA, NO_ENVIADO, MONTO)
                                    VALUES
                                    (@IDBUZON, @IDOPERACION, @FECHA, @IDBANCO, @IDCUENTA, @MONEDA, @NO_ENVIADO, @MONTO);
                                    END";

                // Parámetros
                cmd.Parameters.AddWithValue("@IDBUZON", a.IdBuzon);
                cmd.Parameters.AddWithValue("@IDOPERACION", a.IdOperacion);
                cmd.Parameters.AddWithValue("@FECHA", fechaParaInsertar);
                cmd.Parameters.AddWithValue("@IDBANCO", (object)a.IdBanco ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IDCUENTA", (object)a.IdCuenta ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MONEDA", a.Moneda);
                cmd.Parameters.AddWithValue("@NO_ENVIADO", a.No_Enviado);
                cmd.Parameters.AddWithValue("@MONTO", a.Monto);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public async Task crearAcreditacionesByListaCuentaBuzones(List<CuentaBuzon> accounts)
        {
            if (accounts != null && accounts.Count > 0)
            {
                foreach (var _acc in accounts)
                {
                    if(_acc.NC == "EA23L0410N12000062")
                    {
                        Console.WriteLine("es el que se repite de fonbay");
                    }
                    foreach (var _dep in _acc.Depositos)
                    {
                        int bankId = ServicioBanco.getInstancia().getByNombre(_acc.Banco).BancoId;

                        int moneyId = getMoneyIdByAccount(_acc);

                        double montoTotalDelDeposito = _dep.Totales.Sum(e => e.ImporteTotal);

                        Acreditacion a = new Acreditacion
                        {
                            IdBuzon = _acc.NC,
                            IdOperacion = _dep.IdOperacion,
                            Fecha = _dep.FechaDep,
                            IdBanco = bankId,
                            IdCuenta = _acc.IdCuenta,
                            Moneda = moneyId,
                            No_Enviado = false,
                            Monto = montoTotalDelDeposito,
                        };


                        //Si es cliente de las sierras, asignar FechaTanda 7:00 am en cada acreditacion asi al insertarlo lo inserta a esa hora independientemente de cuando se llamó.

                        if (_acc.IdCliente == 268)
                        {
                            a.FechaTanda = DateTime.Today.AddHours(7);
                        }
                        insertar(a);

                    }
                }
                await Task.Delay(50);
            }
        }
        public async Task crearAcreditacionesByListaCuentaBuzonesTanda(List<CuentaBuzon> accounts, int tanda)
        {
            if (accounts != null && accounts.Count > 0)
            {
                foreach (var _acc in accounts)
                {
                    foreach (var _dep in _acc.Depositos)
                    {
                        int bankId = ServicioBanco.getInstancia().getByNombre(_acc.Banco).BancoId;

                        int moneyId = getMoneyIdByAccount(_acc);

                        double montoTotalDelDeposito = _dep.Totales.Sum(e => e.ImporteTotal);

                        Acreditacion a = new Acreditacion
                        {
                            IdBuzon = _acc.NC,
                            IdOperacion = _dep.IdOperacion,
                            IdBanco = bankId,
                            IdCuenta = _acc.IdCuenta,
                            Moneda = moneyId,
                            No_Enviado = false,
                            Monto = montoTotalDelDeposito,
                        };

                        if (tanda == 1)
                        {
                            //ponerle aqui FechaTanda 1
                            a.FechaTanda = DateTime.Today.AddHours(7);

                        }

                        if (tanda == 2)
                        {
                            //ponerle aqui FechaTanda 2
                            a.FechaTanda = DateTime.Today.AddHours(14).AddMinutes(30);

                        }

                        insertar(a);

                    }
                }
                await Task.Delay(50);
            }
        }
        private int getMoneyIdByAccount(CuentaBuzon acc)
        {
            if (acc.Moneda == VariablesGlobales.pesos)
            {
                return 1;
            }
            if (acc.Moneda == VariablesGlobales.dolares)
            {
                return 2;
            }
            return 0;
        }
        
        //DEVUELVE MONTO TOTAL AGRUPADO POR EMPRESA
        public async Task<List<DtoAcreditacionesPorEmpresa>> getAcreditacionesByFechaYBanco(DateTime desde, DateTime hasta, Banco bank)
        {

            List<DtoAcreditacionesPorEmpresa> retorno = new List<DtoAcreditacionesPorEmpresa>();
            //debo obtener todas las acreditaciones de una fecha desde y fecha hasta,por banco
            //este metodo no tiene en cuenta el tipo de acreditacion, solo el banco y la fecha
            if (desde != DateTime.MinValue && hasta != DateTime.MinValue && bank != null)
            {
                using (SqlConnection conn = new SqlConnection(_conexionTSD))
                {

                    string query = @"SELECT  
                                    cb.EMPRESA, 
                                    cb.sucursal, 
                                    cb.cuenta, 
                                    cb.moneda, 
                                    cc.sucursal as CIUDAD,      
                                    SUM(acc.MONTO) AS TotalMonto 
                                    FROM ConfiguracionAcreditacion AS config 
                                    INNER JOIN CUENTASBUZONES AS cb ON config.CuentasBuzonesId = cb.ID 
                                    INNER JOIN cc ON cb.IDCLIENTE = cc.IDCLIENTE AND config.NC = cc.NC 
                                    INNER JOIN AcreditacionDepositoDiegoTest AS acc  
                                    ON acc.IDBUZON = config.NC AND acc.IDCUENTA = cb.ID 
                                    WHERE cb.BANCO = @bank 
                                    AND acc.FECHA BETWEEN @FechaDesde AND @FechaHasta 
                                    GROUP BY cb.EMPRESA, cb.sucursal, cb.CUENTA, cb.MONEDA, cc.SUCURSAL 
                                    ORDER BY cb.EMPRESA;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@bank", bank.NombreBanco);

                        cmd.Parameters.AddWithValue("@FechaDesde", desde);

                        cmd.Parameters.AddWithValue("@FechaHasta", hasta);

                        conn.Open();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {

                            int empresaOrdinal = reader.GetOrdinal("EMPRESA");
                            int sucursalIdOrdinal = reader.GetOrdinal("SUCURSAL");
                            int cuentaOrdinal = reader.GetOrdinal("CUENTA");
                            int totalMontoOrdinal = reader.GetOrdinal("TotalMonto");
                            int ciudadOrdinal = reader.GetOrdinal("CIUDAD");
                            int monedaOrdinal = reader.GetOrdinal("MONEDA");

                            while (await reader.ReadAsync())
                            {
                                DtoAcreditacionesPorEmpresa dto = new DtoAcreditacionesPorEmpresa
                                {
                                    Empresa = reader.GetString(empresaOrdinal), //ok
                                    Sucursal = reader.GetString(sucursalIdOrdinal),
                                    Moneda = reader.GetString(monedaOrdinal),
                                    NumeroCuenta = reader.GetString(cuentaOrdinal),
                                    Monto = reader.GetDouble(totalMontoOrdinal), //ok
                                    Ciudad = reader.GetString(ciudadOrdinal)
                                };

                                retorno.Add(dto);
                            }
                        }
                    }
                }
            }
            return retorno;
        }

        //DEVUELVE DE CADA NN Y SU EMPRESA, EL MONTO TOTAL
        public async Task<List<DtoAcreditacionesPorEmpresa>>
    getAcreditacionesByFechaBancoClienteYTipoAcreditacion(
        DateTime desde,
        DateTime hasta,
        Cliente cli,
        Banco bank,
        ConfiguracionAcreditacion tipoAcreditacion)
        {
            var retorno = new List<DtoAcreditacionesPorEmpresa>();
            bool esBBVA = bank.NombreBanco
                .Equals(VariablesGlobales.bbva, StringComparison.OrdinalIgnoreCase);

            // 1) Elige la consulta según sea BBVA o no
            string sql = esBBVA
                ? @"
            SELECT
                cc.NN,
                cb.CUENTA,
                acc.MONEDA     AS MonedaCode,
                cc.SUCURSAL as CIUDAD,
                SUM(acc.MONTO) AS TotalMonto
            FROM ConfiguracionAcreditacion config
            INNER JOIN CUENTASBUZONES cb
                ON config.CuentasBuzonesId = cb.ID
            INNER JOIN cc
                ON config.NC = cc.NC
            INNER JOIN AcreditacionDepositoDiegoTest acc
                ON acc.IDBUZON   = config.NC
               AND acc.IDCUENTA  = cb.ID
            WHERE UPPER(cb.BANCO)     = UPPER(@banco)
              AND cb.IDCLIENTE        = @idCliente
              AND CAST(acc.FECHA AS date) = CAST(GETDATE() AS date)
            GROUP BY
                cc.NN, cb.CUENTA, acc.MONEDA, cc.SUCURSAL;"
                : @"
            SELECT
                cc.NN,
                cb.EMPRESA,
                cb.CUENTA,
                cb.SUCURSAL,
                acc.MONEDA    AS MonedaCode,
                cc.SUCURSAL  AS Ciudad,
                SUM(acc.MONTO) AS TotalMonto
            FROM ConfiguracionAcreditacion config
            INNER JOIN CUENTASBUZONES cb
                ON config.CuentasBuzonesId = cb.ID
            INNER JOIN cc
                ON config.NC = cc.NC
            INNER JOIN AcreditacionDepositoDiegoTest acc
                ON acc.IDBUZON   = config.NC
               AND acc.IDCUENTA  = cb.ID
            WHERE UPPER(config.TipoAcreditacion) = UPPER(@tipoAcred)
              AND UPPER(cb.BANCO)                 = UPPER(@banco)
              AND acc.FECHA >= @FechaDesde
              AND acc.FECHA <= @FechaHasta
            GROUP BY
                cc.NN,
                cb.EMPRESA,
                cb.CUENTA,
                cb.SUCURSAL,
                acc.MONEDA,
                cc.SUCURSAL;";

            using var conn = new SqlConnection(_conexionTSD);
            using var cmd = new SqlCommand(sql, conn);

            // 2) Parámetros comunes
            cmd.Parameters.AddWithValue("@banco", bank.NombreBanco);

            if (esBBVA)
            {
                cmd.Parameters.AddWithValue("@idCliente", cli.IdCliente);
            }
            else
            {
                cmd.Parameters.AddWithValue("@tipoAcred", tipoAcreditacion.TipoAcreditacion);
                cmd.Parameters.AddWithValue("@FechaDesde", desde);
                cmd.Parameters.AddWithValue("@FechaHasta", hasta);
            }

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            // 3) Mapear según esquema de columnas
            if (esBBVA)
            {
                int ordNN = reader.GetOrdinal("NN");
                int ordCuenta = reader.GetOrdinal("CUENTA");
                int ordMonedaCode = reader.GetOrdinal("MonedaCode");
                int ordTotal = reader.GetOrdinal("TotalMonto");
                int ordCiudad = reader.GetOrdinal("CIUDAD");

                while (await reader.ReadAsync())
                {
                    var dto = new DtoAcreditacionesPorEmpresa
                    {
                        NN = reader.GetString(ordNN).Trim(),
                        NumeroCuenta = reader.GetString(ordCuenta).Trim(),
                        Divisa = reader.GetInt32(ordMonedaCode),
                        Monto = reader.GetDouble(ordTotal),
                        Ciudad = reader.GetString(ordCiudad).Trim()
                        // Empresa, Sucursal y Ciudad no interesan para BBVA
                    };
                    dto.setMoneda();
                    retorno.Add(dto);
                }
            }
            else
            {
                int ordNN = reader.GetOrdinal("NN");
                int ordEmpresa = reader.GetOrdinal("EMPRESA");
                int ordCuenta = reader.GetOrdinal("CUENTA");
                int ordSucursal = reader.GetOrdinal("SUCURSAL");
                int ordMonedaCode = reader.GetOrdinal("MonedaCode");
                int ordCiudad = reader.GetOrdinal("Ciudad");
                int ordTotal = reader.GetOrdinal("TotalMonto");

                while (await reader.ReadAsync())
                {
                    var dto = new DtoAcreditacionesPorEmpresa
                    {
                        NN = reader.GetString(ordNN).Trim(),
                        Empresa = reader.GetString(ordEmpresa).Trim(),
                        NumeroCuenta = reader.GetString(ordCuenta).Trim(),
                        Sucursal = reader.GetString(ordSucursal).Trim(),
                        Divisa = reader.GetInt32(ordMonedaCode),
                        Ciudad = reader.GetString(ordCiudad).Trim(),
                        Monto = reader.GetDouble(ordTotal)
                    };
                    dto.setMoneda();
                    retorno.Add(dto);
                }
            }

            return retorno;
        }

        public async Task<List<DtoAcreditacionesPorEmpresa>> getAcreditacionesParaExcelTesoreria(Banco banco,int numTanda, ConfiguracionAcreditacion configuracionAcreditacion)
        {
            if(banco == null || configuracionAcreditacion == null)
            {
                throw new Exception("Banco o Configuración vacías");
            }

            List<DtoAcreditacionesPorEmpresa> retorno = new List<DtoAcreditacionesPorEmpresa>();
            string query = "";
            using (var conn = new SqlConnection(_conexionTSD))
            {

                await conn.OpenAsync();

                if (numTanda == 1)
                {
                    //Si numTanda es 1, entonces debemos incluir DELASSIERRAS que es día a día en el excel para tesorería.
                    query = @"SELECT
                            cb.EMPRESA,
                            cb.CUENTA,
                            cb.SUCURSAL,
                            acc.MONEDA             AS MonedaCode,
                            cc.SUCURSAL            AS Ciudad,
                            SUM(acc.MONTO)         AS TotalMonto
                            FROM ConfiguracionAcreditacion AS config
                            INNER JOIN CUENTASBUZONES AS cb
                            ON config.CuentasBuzonesId = cb.ID
                            INNER JOIN CC AS cc
                            ON config.NC = cc.NC
                            INNER JOIN AcreditacionDepositoDiegoTest AS acc
                            ON acc.IDBUZON  = config.NC
                            AND acc.IDCUENTA = cb.ID
                            WHERE
                            UPPER(cb.BANCO) = @banco
                            AND CAST(acc.FECHA AS date) = CAST(GETDATE() AS date)
                            AND CONVERT(time, acc.FECHA) = '07:00:00'
                            GROUP BY
                            cb.EMPRESA,
                            cb.CUENTA,
                            cb.SUCURSAL,
                            acc.MONEDA,
                            cc.SUCURSAL;";
                }

                else
                {
                    query = @"SELECT
                            cb.EMPRESA,
                            cb.CUENTA,
                            cb.SUCURSAL,
                            acc.MONEDA             AS MonedaCode,
                            cc.SUCURSAL            AS Ciudad,
                            SUM(acc.MONTO)         AS TotalMonto
                            FROM ConfiguracionAcreditacion AS config
                            INNER JOIN CUENTASBUZONES AS cb
                            ON config.CuentasBuzonesId = cb.ID
                            INNER JOIN CC AS cc
                            ON config.NC = cc.NC
                            INNER JOIN AcreditacionDepositoDiegoTest AS acc
                            ON acc.IDBUZON  = config.NC
                            AND acc.IDCUENTA = cb.ID
                            WHERE
                            UPPER(cb.BANCO) = @banco
                            AND CAST(acc.FECHA AS date) = CAST(GETDATE() AS date)
                            and config.TipoAcreditacion = @tipoAcreditacion
                            AND CONVERT(time, acc.FECHA) = '14:30:00'
                            GROUP BY
                            cb.EMPRESA,
                            cb.CUENTA,
                            cb.SUCURSAL,
                            acc.MONEDA,
                            cc.SUCURSAL";
                }



                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@banco", banco.NombreBanco);

                cmd.Parameters.AddWithValue("@tipoAcreditacion", configuracionAcreditacion.TipoAcreditacion);

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    int empresaOrdinal = r.GetOrdinal("EMPRESA");

                    int cuentaOrdinal = r.GetOrdinal("CUENTA"); 

                    int sucursalOrdinal = r.GetOrdinal("SUCURSAL"); 

                    int monedaOrdinal = r.GetOrdinal("MonedaCode");

                    int ciudadOrdinal = r.GetOrdinal("Ciudad");

                    int totalMontoOrdinal = r.GetOrdinal("TotalMonto");

                    while (r.Read())
                    {
                        DtoAcreditacionesPorEmpresa dto  = new DtoAcreditacionesPorEmpresa
                        {
                            Empresa = r.GetString(empresaOrdinal).Trim(),
                            NumeroCuenta = r.GetString(cuentaOrdinal).Trim(),
                            Sucursal = r.GetString(sucursalOrdinal).Trim(),
                            Divisa = r.GetInt32(monedaOrdinal),
                            Ciudad = r.GetString(ciudadOrdinal).Trim(),
                            Monto = r.GetDouble(totalMontoOrdinal)
                        };
                        dto.setMoneda();
                        retorno.Add(dto);
                    }
                }

                return retorno;
            }
        }
    }
}
