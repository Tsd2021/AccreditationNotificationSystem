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
        public string Ciudad { get; set; }
        public DateTime? Cierre { get; set; }
        public int IdCliente { get; set; }
        public string SucursalCuenta { get; set; }
        public ConfiguracionAcreditacion Config { get; set; }
        public List<Deposito> Depositos { get; set; } = new List<Deposito>();
        public string Divisa { get; set; }
        public int Producto { get; set; }
        public void setDivisa()
        {
            if (Moneda == "PESOS")
            {
                Divisa = "UYU";
            }
            if(Moneda == "DOLARES")
            {
                Divisa = "USD";
            }
        }




    }
}
