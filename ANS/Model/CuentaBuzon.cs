using ANS.Model.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model
{
    public class CuentaBuzon
    {
        public int CuentasBuzonesId { get; set; }
        public string Banco { get; set; }
        public string IdReferenciaAlCliente { get; set; } // ID CC
        public string Cuenta { get; set; }
        public string Moneda { get; set; }
        public string Empresa { get; set; }
        public string NC { get; set; }
        public string NN { get; set; }
        public string Ciudad { get; set; }
        public DateTime? Cierre { get; set; }
        public int IdCliente { get; set; }
        public string SucursalCuenta { get; set; }
        public ConfiguracionAcreditacion Config { get; set; }
        public List<Deposito> Depositos { get; set; } = new List<Deposito>();
        public string Divisa { get; set; }
        public int Producto { get; set; }
        public int IdCuenta { get; set; }
        public bool CashOffice { get; set; }
        public List<Acreditacion> ListaAcreditaciones { get; set; } = new List<Acreditacion>();
        public void setDivisa()
        {
            if (Moneda == "PESOS")
            {
                Divisa = "UYU";
            }
            if (Moneda == "DOLARES")
            {
                Divisa = "USD";
            }
        }
        public bool esCashOffice()
        {
            return CashOffice;
        }
        public void setCashOffice()
        {
            if (Banco == VariablesGlobales.cashoffice)
            {
                CashOffice = true;
            }
            else
            {
                CashOffice = false;
            }
        }
        public int getIdMoneda()
        {
            if(this.Moneda == VariablesGlobales.pesos)
            {
                return 1;
            }
            if(this.Moneda == VariablesGlobales.dolares)
            {
                return 2;
            }
            return -1;
        }

        public bool esHenderson()
        {
            bool es = false;

            if(ServicioCliente.getInstancia().getClientesRelacionados(new Cliente { IdCliente = this.IdCliente }).Where(c => c.IdCliente == IdCliente).Any(c => c.Nombre.ToUpper().Contains("HENDERSON")))
            {
                es = true;
            }

            return es;
        }
    }
}
