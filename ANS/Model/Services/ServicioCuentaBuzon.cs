using ANS.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Http.HttpClient;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using ANS.Model.GeneradorArchivoPorBanco;
using Quartz.Util;

namespace ANS.Model.Services
{
    public class ServicioCuentaBuzon : IServicioCuentaBuzon
    {
        private string _conexionTSD = ConfiguracionGlobal.ConexionTSD;
        private string _conexionWebBuzones = ConfiguracionGlobal.ConexionWebBuzones;
        public static ServicioCuentaBuzon instancia { get; set; }
        public static ServicioCuentaBuzon getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioCuentaBuzon();
            }
            return instancia;
        }
        //Metodo para obtener todas las cuentas de buzones,por cliente,banco,cuenta,moneda,tipo de acreditación,todo completo.
        public List<CuentaBuzon> getAll()
        {

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {

                string query = "SELECT buzon.NC,buzon.BANCO,buzon.CIERRE,buzon.IDCLIENTE,cuentabuzon.CUENTA,cuentabuzon.MONEDA,cuentabuzon.EMPRESA,config.TipoAcreditacion,config.ParametroDiaADia,config.ParametroPuntoAPunto,config.ParametroTanda1,config.ParametroTanda2 " +
                    "FROM CC buzon INNER JOIN CUENTASBUZONES cuentabuzon ON buzon.IDCLIENTE = cuentabuzon.IDCLIENTE " +
                    "INNER JOIN ConfiguracionAcreditacion config ON cuentabuzon.ID = config.CuentasBuzonesId " +
                    "WHERE buzon.BANCO IS NOT NULL " +
                    "AND LTRIM(RTRIM(buzon.BANCO)) <> '' " +
                    "AND buzon.BANCO <> 'SIN ASIGNAR' " +
                    "ORDER BY buzon.BANCO DESC";

                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CuentaBuzon cuentaBuzon = new CuentaBuzon()
                        {

                        };

                    }
                }

            }

            return new List<CuentaBuzon>();
            /*
                 SELECT buzon.NC,buzon.BANCO,buzon.CIERRE,buzon.IDCLIENTE,cuentabuzon.CUENTA,cuentabuzon.MONEDA,cuentabuzon.EMPRESA,config.TipoAcreditacion,config.ParametroDiaADia,config.ParametroPuntoAPunto,config.ParametroTanda1,config.ParametroTanda2
                 FROM CC buzon
                 INNER JOIN CUENTASBUZONES cuentabuzon ON buzon.IDCLIENTE = cuentabuzon.IDCLIENTE
                 INNER JOIN ConfiguracionAcreditacion config ON cuentabuzon.ID = config.CuentasBuzonesId
                 WHERE buzon.BANCO IS NOT NULL
                 AND LTRIM(RTRIM(buzon.BANCO)) <> ''
                 AND buzon.BANCO <> 'SIN ASIGNAR'
                 ORDER BY buzon.BANCO DESC
            */
        }
        public List<CuentaBuzon> getAllByTipoAcreditacion(string tipoAcreditacion)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                string query = "SELECT c.NC, cb.BANCO, c.CIERRE, c.IDCLIENTE, cb.CUENTA, cb.MONEDA, cb.EMPRESA, config.TipoAcreditacion,c.SUCURSAL as CIUDAD, cb.SUCURSAL, cb.TANDA " +
                               "FROM ConfiguracionAcreditacion config " +
                               "INNER JOIN CUENTASBUZONES cb ON config.CuentasBuzonesId = cb.ID " +
                               "INNER JOIN CC c ON config.NC = c.NC " +
                               "WHERE config.TipoAcreditacion = @tipoAcreditacion " +
                               "AND config.CuentasBuzonesId IS NOT NULL " +
                               "AND config.ConfigId IS NOT NULL " +
                               "ORDER BY c.NC";

                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tipoAcreditacion", tipoAcreditacion);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // Obtener los índices de las columnas
                    int ncOrdinal = reader.GetOrdinal("NC");
                    int bancoOrdinal = reader.GetOrdinal("BANCO");
                    int cierreOrdinal = reader.GetOrdinal("CIERRE");
                    int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");
                    int cuentaOrdinal = reader.GetOrdinal("CUENTA");
                    int monedaOrdinal = reader.GetOrdinal("MONEDA");
                    int empresaOrdinal = reader.GetOrdinal("EMPRESA");
                    int tipoAcreditacionOrdinal = reader.GetOrdinal("TipoAcreditacion");
                    int ciudadOrdinal = reader.GetOrdinal("CIUDAD");
                    int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");
                    int tandaOrdinal = reader.GetOrdinal("TANDA");

                    while (reader.Read())
                    {
                        CuentaBuzon cuentaBuzon = new CuentaBuzon
                        {
                            NC = reader.GetString(ncOrdinal),
                            Banco = reader.GetString(bancoOrdinal),
                            Cierre = reader.IsDBNull(cierreOrdinal) ? (DateTime?)null : reader.GetDateTime(cierreOrdinal),
                            IdCliente = reader.GetInt32(idClienteOrdinal),
                            Cuenta = reader.GetString(cuentaOrdinal),
                            Moneda = reader.GetString(monedaOrdinal),
                            Empresa = reader.GetString(empresaOrdinal),
                            SucursalCuenta = reader.GetString(sucursalOrdinal),
                            Ciudad = reader.GetString(ciudadOrdinal),
                            Producto = reader.GetInt32(tandaOrdinal)
                        };

                        cuentaBuzon.setDivisa();

                        // Verificar y asignar la configuración
                        if (!reader.IsDBNull(tipoAcreditacionOrdinal))
                        {
                            cuentaBuzon.Config = new ConfiguracionAcreditacion(reader.GetString(tipoAcreditacionOrdinal));
                        }

                        buzonesFound.Add(cuentaBuzon);
                    }
                }
            }

            return buzonesFound;
        }

        public List<CuentaBuzon> getAllByTipoAcreditacionYBanco(string tipoAcreditacion, string banco)
        {
            List<CuentaBuzon> buzonesFound = new List<CuentaBuzon>();

            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                string query = "SELECT c.NC, cb.BANCO, c.CIERRE, c.IDCLIENTE, cb.CUENTA, cb.MONEDA, cb.EMPRESA, config.TipoAcreditacion,c.SUCURSAL as CIUDAD, cb.SUCURSAL, c.IDCC " +
                               "FROM ConfiguracionAcreditacion config " +
                               "INNER JOIN CUENTASBUZONES cb ON config.CuentasBuzonesId = cb.ID " +
                               "INNER JOIN CC c ON config.NC = c.NC " +
                               "WHERE config.TipoAcreditacion = @tipoAcreditacion " +
                               "AND cb.BANCO = @bank " +
                               "AND config.CuentasBuzonesId IS NOT NULL " +
                               "AND config.ConfigId IS NOT NULL " +
                               "ORDER BY c.NC";

                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@tipoAcreditacion", tipoAcreditacion);

                cmd.Parameters.AddWithValue("@bank", banco);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // Obtener los índices de las columnas
                    int ncOrdinal = reader.GetOrdinal("NC");
                    int bancoOrdinal = reader.GetOrdinal("BANCO");
                    int cierreOrdinal = reader.GetOrdinal("CIERRE");
                    int idClienteOrdinal = reader.GetOrdinal("IDCLIENTE");
                    int cuentaOrdinal = reader.GetOrdinal("CUENTA");
                    int monedaOrdinal = reader.GetOrdinal("MONEDA");
                    int empresaOrdinal = reader.GetOrdinal("EMPRESA");
                    int tipoAcreditacionOrdinal = reader.GetOrdinal("TipoAcreditacion");
                    int ciudadOrdinal = reader.GetOrdinal("CIUDAD");
                    int sucursalOrdinal = reader.GetOrdinal("SUCURSAL");
                    int idReferenciaAlCliente = reader.GetOrdinal("IDCC");

                    while (reader.Read())
                    {
                        CuentaBuzon cuentaBuzon = new CuentaBuzon
                        {
                            NC = reader.GetString(ncOrdinal),
                            Banco = reader.GetString(bancoOrdinal),
                            Cierre = reader.IsDBNull(cierreOrdinal) ? (DateTime?)null : reader.GetDateTime(cierreOrdinal),
                            IdCliente = reader.GetInt32(idClienteOrdinal),
                            Cuenta = reader.GetString(cuentaOrdinal).Replace("\r", "").Replace("\n", ""), // Limpia \r\n
                            Moneda = reader.GetString(monedaOrdinal),
                            Empresa = reader.GetString(empresaOrdinal),
                            SucursalCuenta = reader.GetString(sucursalOrdinal),
                            Ciudad = reader.GetString(ciudadOrdinal),
                            IdReferenciaAlCliente = reader.GetString(idReferenciaAlCliente)
                        };


                        cuentaBuzon.setDivisa();

                        // Verificar y asignar la configuración
                        if (!reader.IsDBNull(tipoAcreditacionOrdinal))
                        {
                            cuentaBuzon.Config = new ConfiguracionAcreditacion(reader.GetString(tipoAcreditacionOrdinal));
                        }

                        buzonesFound.Add(cuentaBuzon);
                    }
                }
            }

            return buzonesFound;
        }

        private void generarArchivoPorBanco(List<CuentaBuzon> listaCuentaBuzones, string banco, string tipoAcreditacion)
        {
            if (listaCuentaBuzones == null)
            {
                throw new Exception("Error en método generarArchivoPorBanco: listaBuzones es null.");
            }

            if (listaCuentaBuzones.Count == 0)
            {
                throw new Exception("Error en método generarArchivoPorBanco: Lista Buzones tiene 0 elementos");
            }

            IBancoModoAcreditacion bank = BankFactory.GetModoAcreditacionByBanco(banco, tipoAcreditacion);

            if (bank != null)
            {

                bank.GenerarArchivo(listaCuentaBuzones);

                return;

            }

            throw new Exception("Error en método generarArchivoPorBanco: el modo de" +
                " acreditacion por banco no fue encontrado.");

        }
        //METODO PROCESAR DEPOSITOS DE CUENTAS PUNTO A PUNTO BBVA(TODA HORA 15 Y 45) Y SANTANDER(5MIN)
        //Método Acreditar punto a punto para Santander (5 mins)
        #region MÉTODOS ACREDITAR POR CONFIGURACIÓN!
        public void acreditarPuntoAPuntoPorBanco(string bank)
        {
            int ultIdOperacionPorBuzon = 0;
            List<CuentaBuzon> buzones = getAllByTipoAcreditacionYBanco(VariablesGlobales.p2p, bank);

            if (buzones != null && buzones.Count > 0)
            {

                foreach (CuentaBuzon unBuzon in buzones)
                {
                    ultIdOperacionPorBuzon = obtenerUltimaOperacionByNC(unBuzon.NC);

                    if (ultIdOperacionPorBuzon > 0)
                    {
                        ServicioDeposito.getInstancia().asignarDepositosAlBuzon(unBuzon, ultIdOperacionPorBuzon);
                    }

                }
                generarArchivoPorBanco(buzones, bank, VariablesGlobales.p2p);

                //luego de generar, tiene que insertar en ACREDITACIONESDEPOSITO los depositos que fueron insertados ok

                return;

            }

            throw new Exception("No se encontaron buzones punto a punto");

        }
        public void acreditarDiaADiaPorBanco(string banco)
        {

            List<CuentaBuzon> cuentaBuzones = getAllByTipoAcreditacionYBanco(VariablesGlobales.diaxdia, banco);

            if (cuentaBuzones != null && cuentaBuzones.Count > 0)
            {
                foreach (var unaCuentaBuzon in cuentaBuzones)
                {

                    int ultIdOperacion = this.obtenerUltimaOperacionByNC(unaCuentaBuzon.NC);

                    if (ultIdOperacion > 0)
                    {
                        ServicioDeposito.getInstancia().asignarDepositosAlBuzon(unaCuentaBuzon, ultIdOperacion);
                    }

                    generarArchivoPorBanco(cuentaBuzones, banco, VariablesGlobales.diaxdia);
                    //luego de generar, tiene que insertar en ACREDITACIONESDEPOSITO los depositos que fueron insertados ok
                    return;
                }
            }
            throw new Exception("No se encontaron buzones día a día");

        }
        public void acreditarTandaPorBanco(string bank)
        {
            List<CuentaBuzon> cuentaBuzones = getAllByTipoAcreditacionYBanco(VariablesGlobales.tanda, bank);

            if (cuentaBuzones != null && cuentaBuzones.Count > 0)
            {
                foreach (var unaCuentaBuzon in cuentaBuzones)
                {

                    int ultIdOperacion = this.obtenerUltimaOperacionByNC(unaCuentaBuzon.NC);

                    if (ultIdOperacion > 0)
                    {
                        ServicioDeposito.getInstancia().asignarDepositosAlBuzon(unaCuentaBuzon, ultIdOperacion);
                    }

                    generarArchivoPorBanco(cuentaBuzones, bank, VariablesGlobales.tanda);

                    //luego de generar, tiene que insertar en ACREDITACIONESDEPOSITO los depositos que fueron insertados ok

                    return;
                }
            }
            throw new Exception("No se encontaron buzones tanda");
        }
        #endregion
        private int obtenerUltimaOperacionByNC(string nc)
        {
            using (SqlConnection conn = new SqlConnection(_conexionTSD))
            {
                string query = "select max(idoperacion) from ACREDITACIONESDEPOSITOS where IDBUZON = @ncFound";

                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@ncFound", nc);

                object result = cmd.ExecuteScalar();
                return result != DBNull.Value && result != null ? Convert.ToInt32(result) : 0;
            }
            return 0;
        }
        //Henderson y relacionados TANDA 1 07:30
        //Henderson y relacionados TANDA 2 14:30
        public void acreditarTandaHendersonYRelacionados(TimeSpan tanda)
        {
            //va a recibir un timespan, que luego hay que buscar desde ese para atrás.

        }

    }
}