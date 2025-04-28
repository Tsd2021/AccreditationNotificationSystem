using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Services
{
    public class ServicioCC
    {

        private string _conexionTSD22 = ConfiguracionGlobal.Conexion22;
        private string _conexionTSD20 = ConfiguracionGlobal.ConexionTSD;
        public List<Buzon> listaBuzones { get; private set; } = new List<Buzon>();
        public static ServicioCC instancia { get; set; }
     
        public static ServicioCC getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioCC();
            }
            return instancia;
        }


        public List<Buzon> getBuzones()
        {
            return this.listaBuzones;
        }

        public void loadCC()
        {
            if (listaBuzones != null)
            {

                using(SqlConnection c = new SqlConnection(_conexionTSD22))
                {
                    c.Open();

                    string query = "select distinct nc,nn,email from cc where estado = 'alta';";

                    SqlCommand cmd = new SqlCommand(query, c);

                    using(SqlDataReader r = cmd.ExecuteReader())
                    {

                        int nnOrdinal = r.GetOrdinal("nn");
                        int ncOrdinal = r.GetOrdinal("nc");
                        int emailOrdinal = r.GetOrdinal("email");

                        while (r.Read())
                        {
                            Buzon b = new Buzon
                            {
                                NN = r.GetString(nnOrdinal),
                                NC = r.GetString(ncOrdinal),
                                Email = r.GetString(emailOrdinal)
                            };
                            listaBuzones.Add(b);
                        }
                    }
                }
            }
        }
    }
}
