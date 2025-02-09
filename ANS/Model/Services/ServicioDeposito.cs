﻿using ANS.Model.Interfaces;
using Microsoft.Data.SqlClient;

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
        public async Task asignarDepositosAlBuzon(CuentaBuzon b, int ultIdOperacion, TimeSpan horaDeCierre)
        {
            if (b != null)
            {
                ultIdOperacion -= 3;

                using (SqlConnection cnn = new SqlConnection(_conexionWebBuzones))
                {
                    string query;

                    if (horaDeCierre != TimeSpan.Zero)
                    {

                        query = @"
                        SELECT 
                        d.iddeposito, 
                        d.idoperacion, 
                        d.codigo, 
                        d.tipo, 
                        CASE 
                        WHEN CHARINDEX('-', d.empresa) > 0 
                        THEN LTRIM(RTRIM(SUBSTRING(d.empresa, LEN(d.empresa) - CHARINDEX('-', REVERSE(d.empresa)) + 2, LEN(d.empresa))))
                        ELSE LTRIM(RTRIM(d.empresa))
                        END AS empresa, 
                        d.fechadep 
                        FROM 
                        Depositos d
                        INNER JOIN 
                        relaciondeposito rd 
                        ON 
                        d.IdDeposito = rd.IdDeposito 
                        WHERE 
                        d.codigo = @nc 
                        AND 
                        (
                        CASE 
                            WHEN CHARINDEX('-', d.empresa) > 0 
                            THEN LTRIM(RTRIM(SUBSTRING(d.empresa, LEN(d.empresa) - CHARINDEX('-', REVERSE(d.empresa)) + 2, LEN(d.empresa))))
                            ELSE LTRIM(RTRIM(d.empresa))
                        END
                        ) = @empresa
                        AND d.idoperacion > @ultimaOperacion;";
                    }

                    else
                    {
                        // Si hc es TimeSpan.Zero, se usa la consulta sin el filtro adicional por FechaDep.

                        //-- Filtramos por la hora de cierre: solo se traen depósitos cuya hora (parte de FechaDep) sea menor o igual a @hc
                        query = @"
                SELECT 
                    d.iddeposito, 
                    d.idoperacion, 
                    d.codigo, 
                    d.tipo, 
                    CASE 
                        WHEN CHARINDEX('-', d.empresa) > 0 
                            THEN LTRIM(RTRIM(SUBSTRING(d.empresa, LEN(d.empresa) - CHARINDEX('-', REVERSE(d.empresa)) + 2, LEN(d.empresa))))
                        ELSE LTRIM(RTRIM(d.empresa))
                    END AS empresa, 
                    d.fechadep 
                FROM 
                    Depositos d
                INNER JOIN 
                    relaciondeposito rd 
                        ON d.IdDeposito = rd.IdDeposito 
                WHERE 
                    d.codigo = @nc 
                    AND (
                        CASE 
                            WHEN CHARINDEX('-', d.empresa) > 0 
                                THEN LTRIM(RTRIM(SUBSTRING(d.empresa, LEN(d.empresa) - CHARINDEX('-', REVERSE(d.empresa)) + 2, LEN(d.empresa))))
                            ELSE LTRIM(RTRIM(d.empresa))
                        END
                    ) = @empresa
                    AND d.idoperacion > @ultimaOperacion
                    
                    AND CAST(d.FechaDep AS time) <= @horaDeCierre;";
                    }

                    //LA EMPRESA SI TIENE GUION - HAY QUE TOMAR LO QUE ESTA DESPUES DEL GUION.

                    cnn.Open();

                    SqlCommand cmd = new SqlCommand(query, cnn);

                    cmd.Parameters.AddWithValue("@nc", b.NC);

                    cmd.Parameters.AddWithValue("@ultimaOperacion", ultIdOperacion);

                    cmd.Parameters.AddWithValue("@empresa", b.Empresa);

                    cmd.Parameters.AddWithValue("@horaDeCierre", horaDeCierre);

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

                                FechaDep = reader.GetDateTime(4),

                                Tipo = reader.GetString(5)
                            };

                            if (deposito.Tipo == "Validado")
                            {
                                await buscaYAsignarTotalesPorDepositoYDivisa(deposito, b.Divisa, _conexionWebBuzones);

                                b.Depositos.Add(deposito);
                            }
                        }
                    }
                    return;
                }
                throw new Exception("Error en método asignarDepositosAlBuzon : CuentaBuzon is null");
            }
        }
        private async Task buscaYAsignarTotalesPorDepositoYDivisa(Deposito deposito, string divisa, string connectionString)
        {
            if (deposito == null)
            {
                throw new Exception("Error en método buscaYAsignarTotalesPorDepositoYDivisa: deposito es null");
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
                    await Task.Delay(500);
                }
                else
                {
                    throw new Exception("Error en buscaYAsignarTotalesPorDepositoYDivisa - No se encontró un idTotal para ese Deposito");
                }
            }
        }
    }
}
