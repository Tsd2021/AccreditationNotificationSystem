﻿using Microsoft.Data.SqlClient;
using System.Linq.Expressions;

namespace ANS.Model.Services
{
    public class ServicioCliente
    {
        private string _conexionENCUESTA = ConfiguracionGlobal.ConexionEncuesta;
        private string _conexionTSD22 = ConfiguracionGlobal.Conexion22;
        public static ServicioCliente instancia { get; set; }
        public List<Cliente> ListaClientes { get; set; } = new List<Cliente>();
        public static ServicioCliente getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioCliente();
            }
            return instancia;
        }
        public void agregar(Cliente unCliente)
        {
            ListaClientes.Add(unCliente);
        }
        public Cliente getById(int id)
        {
            foreach (Cliente cli in ListaClientes)
            {
                if (cli.IdCliente == id)
                {
                    return cli;
                }
            }
            return null;
        }
        public Cliente getByNombre(string nombre)
        {
            foreach (Cliente cli in ListaClientes)
            {
                if (cli.Nombre.ToUpper().Contains(nombre.ToUpper()))
                {
                    return cli;
                }
            }
            return null;
        }
        public void getAllClientes()
        {
            using (SqlConnection conn = new SqlConnection(_conexionENCUESTA))
            {
                string query = @"SELECT IDCLIENTE,NOMBRE,RUT FROM CLIENTES WHERE Facturacion IN(1,3)";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Cliente cli = new Cliente
                        {
                            IdCliente = reader.GetInt32(0),
                            Nombre = reader.GetString(1),
                            Rut = reader.IsDBNull(2) ? null : reader.GetString(2)
                        };

                        if (cli.IdCliente == 164)
                        {
                            // Aquí se abre una nueva conexión para obtener los clientes relacionados.
                            cli.ClientesRelacionados = getClientesRelacionados(cli);
                        }
                        ListaClientes.Add(cli);
                    }
                }
            }
        }
        public List<Cliente> getClientesRelacionados(Cliente cli)
        {
            List<Cliente> retorno = new List<Cliente>();
            string query = @"
        SELECT 
            cr.idrazonsocial,
            c.NOMBRE
        FROM 
            clientesrelacionados cr
        INNER JOIN 
            clientes c ON cr.idrazonsocial = c.idcliente
        WHERE 
            cr.IDCLIENTE = @idcli";

            // Se crea una nueva conexión para esta consulta.
            using (SqlConnection cnn = new SqlConnection(_conexionENCUESTA))
            {
                cnn.Open();
                SqlCommand cmd = new SqlCommand(query, cnn);
                cmd.Parameters.AddWithValue("@idcli", cli.IdCliente);

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        Cliente cliente = new Cliente
                        {
                            IdCliente = rdr.GetInt32(0),
                            Nombre = rdr.GetString(1),
                        };
                        retorno.Add(cliente);
                    }
                }
            }
            return retorno;
        }


        public List<Cliente> ObtenerClientesPorBancoYTipoAcreditacion(Banco b, ConfiguracionAcreditacion config)
        {
            List<Cliente> retorno = new List<Cliente>();

            try
            {
                using (SqlConnection cn = new SqlConnection(_conexionTSD22))
                {

                    cn.Open();
                    string q = @"Select * from CLIENTES 
                                where IDCLIENTE in (SELECT DISTINCT CB.IDCLIENTE 
                                FROM ConfiguracionAcreditacion AS config 
                                inner join CUENTASBUZONES as cb on config.CuentasBuzonesId = cb.ID 
                                inner join cc on cb.IDCLIENTE = cc.IDCLIENTE and config.NC = cc.NC 
                                where cb.banco = @banco  
                                and config.TipoAcreditacion = @tipoAcreditacion)";

                    SqlCommand cmd = new SqlCommand(q, cn);
                    cmd.Parameters.AddWithValue("@banco", b.NombreBanco);
                    cmd.Parameters.AddWithValue("@tipoAcreditacion", config.TipoAcreditacion);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        int idClienteOrdinal = r.GetOrdinal("IDCLIENTE");
                        int nombreOrdinal = r.GetOrdinal("NOMBRE");
                        int ciudadOrdinal = r.GetOrdinal("CIUDAD");
                        while (r.Read())
                        {
                            Cliente newCli = new Cliente
                            {
                                IdCliente = r.GetInt32(idClienteOrdinal),
                                Nombre = r.GetString(nombreOrdinal),
                                Ciudad = r.GetString(ciudadOrdinal)
                            };
                            retorno.Add(newCli);
                        }
                    }
                }
                return retorno;
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}