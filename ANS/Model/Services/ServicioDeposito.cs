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
        /*
        public async Task asignarDepositosAlBuzon(CuentaBuzon b, int ultIdOperacion, TimeSpan horaDeCierre)
        {
            if (b != null)
            {
              

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
                        AND d.idoperacion > @ultimaOperacion
                        AND CAST(d.FechaDep AS time) <= @horaDeCierre;;";
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
                    AND d.idoperacion > @ultimaOperacion;";
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
                                IdDeposito = reader.GetInt32(0),    // d.iddeposito
                                IdOperacion = reader.GetInt32(1),   // d.idoperacion
                                Codigo = reader.GetString(2),       // d.codigo
                                Tipo = reader.GetString(3),         // d.tipo
                                Empresa = reader.GetString(4),      // alias empresa (resultado del CASE)
                                FechaDep = reader.GetDateTime(5)    // d.fechadep
                            };

                            if (deposito.Tipo == "Validado")
                            {
                                await buscaYAsignarTotalesPorDepositoYDivisa(deposito, b.Divisa, _conexionWebBuzones);
                                Console.WriteLine("TOTAL POR DEPOSITO ASIGNADO OK");
                                b.Depositos.Add(deposito);
                                Console.WriteLine("DEPOSITO AGREGADO A LA LISTA DE DEPOSITOS DEL BUZON OK");
                            }
                        }
                    }
                    return;
                }
                throw new Exception("Error en método asignarDepositosAlBuzon : CuentaBuzon is null");
            }
        }*/


        public async Task asignarDepositosAlBuzon(CuentaBuzon buzon, int ultIdOperacion, TimeSpan horaDeCierre)
        {
            if (buzon == null)
                throw new Exception("Error en método asignarDepositosAlBuzon: CuentaBuzon es null");

            // Lista para almacenar todos los depósitos obtenidos del query
            List<Deposito> depositosList = new List<Deposito>();


            // Seleccionamos el query según la hora de cierre
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
                relaciondeposito rd ON d.IdDeposito = rd.IdDeposito 
            INNER JOIN 
                Totales t ON rd.IdTotal = t.IdTotal
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
                AND t.Divisas = @divisaActual
                AND d.FechaDep < DATEADD(SECOND, DATEDIFF(SECOND, 0, @horaDeCierre), CONVERT(datetime, CONVERT(date, GETDATE())));";
            }
            else
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
                relaciondeposito rd ON d.IdDeposito = rd.IdDeposito 
            INNER JOIN 
                Totales t ON rd.IdTotal = t.IdTotal
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
                AND t.Divisas = @divisaActual;";
            }

            // Primero leemos todos los depósitos
            using (SqlConnection cnn = new SqlConnection(_conexionWebBuzones))
            {
                await cnn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@nc", buzon.NC);

                    cmd.Parameters.AddWithValue("@ultimaOperacion", ultIdOperacion);

                    cmd.Parameters.AddWithValue("@empresa", buzon.Empresa);

                    cmd.Parameters.AddWithValue("@divisaActual", buzon.Divisa);

                    if (horaDeCierre != TimeSpan.Zero)
                        cmd.Parameters.AddWithValue("@horaDeCierre", horaDeCierre);


                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Deposito deposito = new Deposito
                            {
                                IdDeposito = reader.GetInt32(0),
                                IdOperacion = reader.GetInt32(1),
                                Codigo = reader.GetString(2),
                                Tipo = reader.GetString(3),
                                Empresa = reader.GetString(4),
                                FechaDep = reader.GetDateTime(5)
                            };

                            // Solo agregamos los depósitos que están validados
                            if (deposito.Tipo == "Validado")
                            {
                                depositosList.Add(deposito);
                            }
                        }
                    }
                }
            }

     
                object depositosLock = new object();

                // Para evitar saturar el sistema, limitamos la concurrencia (por ejemplo, 10 tareas a la vez)
                var semaphore = new SemaphoreSlim(10);

                var tasks = depositosList.Select(async deposito =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await BuscaYAsignarTotalesPorDepositoYDivisa(deposito, buzon.Divisa, _conexionWebBuzones);
                        Console.WriteLine("TOTAL POR DEPOSITO ASIGNADO OK");
                        // Se agrega de forma thread-safe
                        lock (depositosLock)
                        {
                            buzon.Depositos.Add(deposito);
                        }
                        Console.WriteLine("DEPOSITO AGREGADO A LA LISTA DE DEPOSITOS DEL BUZON OK");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error en depósito {deposito.IdDeposito}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
            
        }


        private async Task BuscaYAsignarTotalesPorDepositoYDivisa(Deposito deposito, string divisa, string connectionString)
        {
            if (deposito == null)
                throw new Exception("Error en método BuscaYAsignarTotalesPorDepositoYDivisa: deposito es null");

            using (SqlConnection localCnn = new SqlConnection(connectionString))
            {
                await localCnn.OpenAsync();

                if(deposito.IdOperacion == 117467)
                {
                    Console.WriteLine("Este es");
                }
                    string query = @"
            select totales.divisas,totales.importetotal
            from totales
            inner join relaciondeposito on totales.IdTotal = relaciondeposito.IdTotal
            where relaciondeposito.iddeposito = @idDep
            and totales.divisas = @divisa";

                SqlCommand cmdTotales = new SqlCommand(query, localCnn);

                cmdTotales.Parameters.AddWithValue("@idDep", deposito.IdDeposito);
                cmdTotales.Parameters.AddWithValue("@divisa", divisa);

                using (SqlDataReader reader = await cmdTotales.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
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
        }

    }
}
