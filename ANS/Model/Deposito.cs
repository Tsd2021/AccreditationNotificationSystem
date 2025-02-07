using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model
{
    public class Deposito
    {
        public int IdOperacion { get; set; }
        public int IdDeposito { get; set; }
        public string Codigo { get; set; } //Esto es NC de CuentaBuzon
        public string Empresa { get; set; }
        public DateTime FechaDep { get; set; }
        public string Tipo { get; set; }
        public List<Total> Totales { get; set; } = new List<Total>();
    }
}
