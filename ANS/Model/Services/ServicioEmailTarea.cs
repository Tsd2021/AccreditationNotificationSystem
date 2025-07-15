using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Services
{
    public class ServicioEmailTarea
    {
        public static ServicioEmailTarea instancia { get; set; }
        private string _conexionTSD = ConfiguracionGlobal.Conexion22;
        public List<Email> ListaEmailTarea { get; set; } = new List<Email>();
        public static ServicioEmailTarea Instancia
        {
            get
            {
                if (instancia == null)
                {
                    instancia = new ServicioEmailTarea();
                }
                return instancia;
            }
        }

        public List<Email> ObtenerTodosLosEmailTarea()
        {

            using (var conexion = new SqlConnection(_conexionTSD))
            {
                conexion.Open();
                string query = "Select Email,Banco,Tarea,Ciudad,Activo From Email_Tarea";

                using (var comando = new SqlCommand(query, conexion))
                {
                    using (var lector = comando.ExecuteReader())
                    {
                        List<Email> listaEmails = new List<Email>();

                        while (lector.Read())
                        {
                            Email email = new Email
                            {
                                Correo = lector["Email"].ToString(),
                                Activo = Convert.ToBoolean(lector["Activo"]),
                                NC = lector["Banco"].ToString(),
                                Ciudad = lector["Ciudad"].ToString(),
                                Tarea = lector["Tarea"].ToString(),
                                Banco = lector["Banco"].ToString(),
                            };
                            listaEmails.Add(email);
                        }

                        ListaEmailTarea = listaEmails;
                         
                        return listaEmails;

                    }
                }
            }
        }


    }
}
