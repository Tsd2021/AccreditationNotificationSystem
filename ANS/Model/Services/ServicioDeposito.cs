using ANS.Model.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Services
{
    public class ServicioDeposito : IServicioDeposito
    {
        private string _conexionWebBuzones = ConfiguracionGlobal.ConexionWebBuzones;
        private static ServicioDeposito instancia { get; set; }
        public static ServicioDeposito getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioDeposito();
            }
            return instancia;
        }
        public void asignarDepositosAlBuzon(CuentaBuzon b,int ultIdOperacion)
        {
            if (b != null)
            {

                ultIdOperacion -= 3;
                using (SqlConnection cnn = new SqlConnection(_conexionWebBuzones))
                {

                    string query = "select d.iddeposito, d.idoperacion, d.codigo, " +
                        "CASE " +
                        "  WHEN CHARINDEX('-', d.empresa) > 0 THEN " +
                        "    SUBSTRING(d.empresa, CHARINDEX('-', d.empresa) + 1, LEN(d.empresa)) " +
                        "  ELSE " +
                        "    d.empresa " +
                        "END AS empresa, " +
                        "d.fechadep " +
                        "from Depositos d " +
                        "inner join relaciondeposito rd " +
                        "on d.IdDeposito = rd.IdDeposito " +
                        "where d.codigo = @nc " +
                        "and d.empresa = @empresa " +
                        "and d.idoperacion > @ultimaOperacion";

                    //LA EMPRESA SI TIENE GUION - HAY QUE TOMAR LO QUE ESTA DESPUES DEL GUION.

                    cnn.Open();

                    SqlCommand cmd = new SqlCommand(query, cnn);

                    cmd.Parameters.AddWithValue("@nc", b.NC);

                    cmd.Parameters.AddWithValue("@ultimaOperacion", ultIdOperacion);

                    cmd.Parameters.AddWithValue("@empresa", b.Empresa);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            Deposito deposito = new Deposito()
                            {
                                IdDeposito = reader.GetInt32(0),

                                IdOperacion = reader.GetInt32(1),

                                Codigo = reader.GetString(2),

                                Empresa = reader.GetString(3),

                                FechaDep = reader.GetDateTime(4)
                            };

                            buscaYAsignarTotalesPorDepositoYDivisa(deposito, b.Moneda, _conexionWebBuzones);

                            b.Depositos.Add(deposito);

                        }
                    }
                    return;

                }

                throw new Exception("Error en método asignarDepositosAlBuzon : CuentaBuzon is null");

            }
        }
        private void buscaYAsignarTotalesPorDepositoYDivisa(Deposito deposito, string divisa, string connectionString)
        {
            if (deposito == null)
            {
                throw new Exception("Error en método buscaYAsignarTotalesPorDepositoYDivisa: deposito es null");
            }

            if (divisa == "PESOS")
            {
                divisa = "UYU";
            }

            if(divisa == "DOLARES")
            {
                divisa = "USD";
            }

            int idTotalesFound = 0;

            using (SqlConnection localCnn = new SqlConnection(connectionString))
            {
                localCnn.Open();

                string queryRelacionDepositoTotales = "select IdTotal " +
                                                      "from Relaciondeposito " +
                                                      "where IdDeposito = @idDep";

                SqlCommand cmdRelacionDepositoTotales = new SqlCommand(queryRelacionDepositoTotales, localCnn);
                cmdRelacionDepositoTotales.Parameters.AddWithValue("@idDep", deposito.IdDeposito);

                object result = cmdRelacionDepositoTotales.ExecuteScalar();
                if (result != null)
                {
                    idTotalesFound = Convert.ToInt32(result);
                }

                if (idTotalesFound > 0)
                {
                    string queryTotalesPorDivisa = "Select Divisas, ImporteTotal " +
                                                   "From Totales " +
                                                   "Where IdTotal = @idTotal " +
                                                   "And Divisas = @coin";

                    SqlCommand cmdTotalesPorDivisa = new SqlCommand(queryTotalesPorDivisa, localCnn);
                    cmdTotalesPorDivisa.Parameters.AddWithValue("@idTotal", idTotalesFound);
                    cmdTotalesPorDivisa.Parameters.AddWithValue("@coin", divisa);

                    using (SqlDataReader reader = cmdTotalesPorDivisa.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Total nuevoTotal = new Total
                            {
                                Divisa = reader.GetString(0),
                                ImporteTotal = reader.GetInt32(1)
                            };

                            deposito.Totales.Add(nuevoTotal);
                        }
                    }
                }
                else
                {
                    throw new Exception("Error en buscaYAsignarTotalesPorDepositoYDivisa - No se encontró un idTotal para ese Deposito");
                }
            }
        }
    }
}
