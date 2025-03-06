using ANS.Model.Interfaces;
using Microsoft.Data.SqlClient;

namespace ANS.Model.Services
{
    public class ServicioAcreditacion : IServicioAcreditacion
    {
        private string _conexionTSD = ConfiguracionGlobal.ConexionTSD;
       
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
                        cmd.Parameters.AddWithValue("@FECHA", (object)a.Fecha ?? DBNull.Value);
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

                        if(montoTotalDelDeposito == 0)
                        {
                            Console.WriteLine("ATENCION ES 0 ");
                        }

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

                        insertar(a);

                    }
                }
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
    }
}