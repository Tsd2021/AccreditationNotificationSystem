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

                using (SqlConnection c = new SqlConnection(_conexionTSD22))
                {
                    c.Open();

                    string query = "select distinct nc,nn,email from cc where estado = 'alta';";

                    SqlCommand cmd = new SqlCommand(query, c);

                    using (SqlDataReader r = cmd.ExecuteReader())
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


        public void loadEmails()
        {
            if (listaBuzones != null && listaBuzones.Count > 0)
            {
                foreach (var b in listaBuzones)
                {
                    using (SqlConnection c = new SqlConnection(_conexionTSD22))
                    {
                        c.Open();

                        string query = "select email,nccc from ccemail where nccc = @nc;";

                        SqlCommand cmd = new SqlCommand(query, c);

                        cmd.Parameters.AddWithValue("@nc", b.NC);

                        using (SqlDataReader r = cmd.ExecuteReader())
                        {

                            int emailOrdinal = r.GetOrdinal("email");
                            int ncOrdinal = r.GetOrdinal("nccc");

                            while (r.Read())
                            {
                                Email e = new Email()
                                {
                                    
                                    Correo = r.GetString(emailOrdinal),
                                    NC = r.GetString(ncOrdinal)
                                };
                                b._listaEmails.Add(e);
                            }
                        }
                    }
                }
            }
        }


        public int insertarEmail(Email e)
        {
            try
            {
                if (e == null) throw new Exception("El correo o el número de cliente no pueden estar vacíos.");

                using (SqlConnection c = new SqlConnection(_conexionTSD22))
                {

                    c.Open();

                    string query = "insert into buzoncorreo (correo,esprincipal,nc) values (@correo,@esPrincipal,@nc);";

                    SqlCommand cmd = new SqlCommand(query, c);

                    cmd.Parameters.AddWithValue("@correo", e.Correo);

                    cmd.Parameters.AddWithValue("@nc", e.NC);

                    cmd.Parameters.AddWithValue("@esPrincipal", e.EsPrincipal);

                    return cmd.ExecuteScalar() != null ? Convert.ToInt32(cmd.ExecuteScalar()) : 0;

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int eliminarEmail(Email e)
        {
            try
            {

                if (e == null) throw new Exception("El correo o el número de cliente no pueden estar vacíos.");
                using (SqlConnection c = new SqlConnection(_conexionTSD22))
                {
                    c.Open();
                    string query = "delete from buzoncorreo where correo = @correo and nc = @nc;";
                    SqlCommand cmd = new SqlCommand(query, c);
                    cmd.Parameters.AddWithValue("@correo", e.Correo);
                    cmd.Parameters.AddWithValue("@nc", e.NC);
                    return cmd.ExecuteNonQuery();
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int modificarEmail(Email e, string nuevoEmail)
        {
            try
            {
                if (e == null) throw new Exception("El correo o el número de cliente no pueden estar vacíos.");

                using (SqlConnection c = new SqlConnection(_conexionTSD22))
                {
                    c.Open();

                    string query = "update buzoncorreo set esprincipal = @esPrincipal , correo = @nuevoEmail where correo = @viejoEmail and nc = @nc;";

                    SqlCommand cmd = new SqlCommand(query, c);
                    cmd.Parameters.AddWithValue("@viejoEmail", e.Correo);
                    cmd.Parameters.AddWithValue("@nuevoEmail", nuevoEmail);
                    cmd.Parameters.AddWithValue("@nc", e.NC);
                    cmd.Parameters.AddWithValue("@esPrincipal", e.EsPrincipal);
                    return cmd.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
