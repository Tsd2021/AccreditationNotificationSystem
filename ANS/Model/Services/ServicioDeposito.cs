using ANS.Model.Interfaces;
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

            // Primero leemos todos los depósitos
            using (SqlConnection cnn = new SqlConnection(_conexionWebBuzones))
            {
                await cnn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@nc", buzon.NC);
                    cmd.Parameters.AddWithValue("@ultimaOperacion", ultIdOperacion);
                    cmd.Parameters.AddWithValue("@empresa", buzon.Empresa);
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

            // Ahora procesamos los depósitos de forma concurrente
            // Si buzon.Depositos es una lista que se comparte, usamos un lock para evitar problemas de concurrencia.
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

            int idTotalesFound = 0;
            using (SqlConnection localCnn = new SqlConnection(connectionString))
            {
                await localCnn.OpenAsync();
                string queryRelacionDepositoTotales = @"
            SELECT IdTotal 
            FROM Relaciondeposito 
            WHERE IdDeposito = @idDep";

                using (SqlCommand cmdRelacion = new SqlCommand(queryRelacionDepositoTotales, localCnn))
                {
                    cmdRelacion.Parameters.AddWithValue("@idDep", deposito.IdDeposito);
                    object result = await cmdRelacion.ExecuteScalarAsync();
                    if (result != null)
                        idTotalesFound = Convert.ToInt32(result);
                }

                if (idTotalesFound > 0)
                {
                    string queryTotalesPorDivisa = @"
                SELECT Divisas, ImporteTotal 
                FROM Totales 
                WHERE IdTotal = @idTotal 
                  AND Divisas = @coin";

                    using (SqlCommand cmdTotales = new SqlCommand(queryTotalesPorDivisa, localCnn))
                    {
                        cmdTotales.Parameters.AddWithValue("@idTotal", idTotalesFound);
                        cmdTotales.Parameters.AddWithValue("@coin", divisa);

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
                else
                {
                    // En lugar de lanzar una excepción, se registra el aviso y se continúa.
                    Console.WriteLine($"Warning: No se encontró un idTotal para el depósito {deposito.IdDeposito}. Se omitirá la asignación de totales.");
                    // Si lo prefieres, podrías asignar un valor predeterminado o marcar el depósito de alguna forma.
                }
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
