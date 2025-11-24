using Microsoft.Data.SqlClient;


namespace ANS.Model.Services
{
    public class ServicioSucursalesClientes
    {
        // ✅ Thread-safe: Lazy<T> garantiza una sola instancia
        // listaSucursalCliente se carga en memoria, múltiples instancias causarían duplicación
        private static readonly Lazy<ServicioSucursalesClientes> _lazy = 
            new Lazy<ServicioSucursalesClientes>(() => new ServicioSucursalesClientes());
        
        public static ServicioSucursalesClientes instancia => _lazy.Value;

        private string _conexionTSD22 = ConfiguracionGlobal.Conexion22;
        public List<DtoSucursalCliente> listaSucursalCliente { get; set; } = new List<DtoSucursalCliente>();
        
        public static ServicioSucursalesClientes getInstancia()
        {
            return _lazy.Value;
        }
        public void CargarSucursalesCliente()
        {

            SqlConnection con = new SqlConnection(_conexionTSD22);

            using (con)
            {
                string query = "Select * from nc_emp_cli_suc";


                con.Open();

                SqlCommand cmd = new SqlCommand(query, con);

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {

                        DtoSucursalCliente dto = new DtoSucursalCliente
                        {
                            NC = r["NC"].ToString(),
                            Empresa = r["EMPRESA"].ToString(),
                            IdCliente = (int)r["IDCLIENTE"],
                            Sucursal = r["SUCURSAL"].ToString()
                        };

                        listaSucursalCliente.Add(dto);
                    }

                }
            }

        }

        public class DtoSucursalCliente
        {
            public string NC { get; set; }
            public string Empresa { get; set; }
            public int IdCliente { get; set; }
            public string Sucursal { get; set; }

            public DtoSucursalCliente()
            {

            }
        }
    }
}
