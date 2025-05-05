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
            if (a != null)
            {
                DateTime fechaParaInsertar = DateTime.Now;

                if (a.FechaTanda != DateTime.MinValue)
                {
                    fechaParaInsertar = a.FechaTanda;
                }

                using (SqlConnection conn = new SqlConnection(_conexionTSD))
                {
                    string query = @"INSERT INTO AcreditacionDepositoDiegoTest 
                            (IDBUZON, IDOPERACION, FECHA, IDBANCO, IDCUENTA, MONEDA, NO_ENVIADO, MONTO) 
                            VALUES 
                            (@IDBUZON, @IDOPERACION, @FECHA, @IDBANCO, @IDCUENTA, @MONEDA, @NO_ENVIADO, @MONTO)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {

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
            }
        }
        public async Task crearAcreditacionesByListaCuentaBuzones(List<CuentaBuzon> accounts)
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
        public async Task<List<DtoAcreditacionesPorEmpresa>> getAcreditacionesByFechaBancoClienteYTipoAcreditacion(DateTime desde, DateTime hasta, Cliente cli, Banco bank, ConfiguracionAcreditacion tipoAcreditacion)
        {

            List<DtoAcreditacionesPorEmpresa> retorno = new List<DtoAcreditacionesPorEmpresa>();
            //debo obtener todas las acreditaciones de una fecha desde y fecha hasta,por banco
            
            if (desde != DateTime.MinValue && hasta != DateTime.MinValue && bank != null)
            {
                try
                {


                    using (SqlConnection conn = new SqlConnection(_conexionTSD))
                    {
                        //ATENTO EN EL ON DONDE SE JOINEAN LAS TABLAS POR CONFIG.NC = CC.NC PRUEBA
                      string query =    @"SELECT   
                                        cc.NN,      
                                        cb.EMPRESA,cb.cuenta,   
                                        cb.SUCURSAL, 
                                        cb.moneda, 
                                        cc.sucursal as CIUDAD, 
                                        SUM(acc.monto) AS TotalMonto 
                                        FROM ConfiguracionAcreditacion AS config 
                                        INNER JOIN CUENTASBUZONES AS cb  
                                        ON config.CuentasBuzonesId = cb.ID 
                                        INNER JOIN cc  
                                        ON config.NC = cc.NC 
                                        INNER JOIN AcreditacionDepositoDiegoTest AS acc  
                                        ON acc.IDBUZON = config.NC AND acc.IDCUENTA = cb.ID 
                                        WHERE config.TipoAcreditacion = @tipoAcreditacion 
                                        AND cb.BANCO = @banco 
                                        AND acc.FECHA >= @FechaDesde 
                                        AND acc.FECHA <= @FechaHasta 
                                        GROUP BY  
                                        cc.NN, 
                                        cb.EMPRESA, 
                                        cb.cuenta, 
                                        cb.SUCURSAL, 
                                        cb.moneda, 
                                        cc.sucursal";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@banco", bank.NombreBanco);

                            cmd.Parameters.AddWithValue("@tipoAcreditacion", tipoAcreditacion.TipoAcreditacion);

                            cmd.Parameters.AddWithValue("@FechaDesde", desde);

                            cmd.Parameters.AddWithValue("@FechaHasta", hasta);

                            conn.Open();

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                           
                                int ordinalNN = reader.GetOrdinal("NN");
                                int ordinalSucursal = reader.GetOrdinal("sucursal");
                                int ordinalEmpresa = reader.GetOrdinal("EMPRESA");
                                int ordinalCuenta = reader.GetOrdinal("cuenta");
                                int ordinalMoneda = reader.GetOrdinal("moneda");
                                int ordinalCiudad = reader.GetOrdinal("CIUDAD");
                                int ordinalMonto = reader.GetOrdinal("TotalMonto");


                                while (await reader.ReadAsync())
                                {
                                    DtoAcreditacionesPorEmpresa dto = new DtoAcreditacionesPorEmpresa
                                    {

                                        NN = reader.GetString(ordinalNN),
                                        Empresa = reader.GetString(ordinalEmpresa),
                                        Sucursal = reader.GetString(ordinalSucursal),
                                        NumeroCuenta = reader.GetString(ordinalCuenta),
                                        Moneda = reader.GetString(ordinalMoneda),
                                        Ciudad = reader.GetString(ordinalCiudad),
                                        Monto = reader.GetDouble(ordinalMonto)
                                    };


                                    dto.setMoneda();

                                    retorno.Add(dto);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return retorno;
        }
    }
}
