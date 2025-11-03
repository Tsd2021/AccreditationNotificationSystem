using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model
{
    public class CuentaCliente
    {

        public int IdCliente { get; set; }
        public string Cuenta { get; set; }
        public string Moneda { get; set; }
        public string Banco { get; set; }
        public int Tipo { get; set; }
        public string EmpresaBuzon {get;set;}


    }
}
