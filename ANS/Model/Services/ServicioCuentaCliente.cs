using Microsoft.Data.SqlClient;

namespace ANS.Model.Services
{
    public class ServicioCuentaCliente
    {

        private string _conexionTSD22 = ConfiguracionGlobal.Conexion22;
        public List<CuentaCliente> listaCuentasClientes { get; private set; } = new List<CuentaCliente>();
        public static ServicioCuentaCliente instancia { get; set; }

        public static ServicioCuentaCliente getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioCuentaCliente();
            }
            return instancia;
        }

        public void CargarListaCuentasClientes()
        {
            using (SqlConnection c = new SqlConnection(_conexionTSD22))

            {
                c.Open();

                string query = "select idcliente,cuenta,moneda,banco,tipo,empresabuzon from clientescuentas;";

                SqlCommand cmd = new SqlCommand(query, c);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    int idClienteOrdinal = r.GetOrdinal("idcliente");

                    int cuentaOrdinal = r.GetOrdinal("cuenta");

                    int monedaOrdinal = r.GetOrdinal("moneda");

                    int bancoOrdinal = r.GetOrdinal("banco");

                    int tipoOrdinal = r.GetOrdinal("tipo");

                    int empresaBuzonOrdinal = r.GetOrdinal("empresabuzon");

                    while (r.Read())
                    {
                        CuentaCliente cc = new CuentaCliente
                        {
                            IdCliente = r.GetInt32(idClienteOrdinal),
                            Cuenta = r.GetString(cuentaOrdinal),
                            Moneda = r.GetString(monedaOrdinal),
                            Banco = r.GetString(bancoOrdinal),
                            Tipo = r.GetInt32(tipoOrdinal),
                            EmpresaBuzon = r.GetString(empresaBuzonOrdinal)
                        };

                        if (!string.IsNullOrEmpty(cc.EmpresaBuzon) || cc.EmpresaBuzon != "NINGUNA")
                        {
                            listaCuentasClientes.Add(cc);
                        }

                    }
                }
            }
        }

    }
}
