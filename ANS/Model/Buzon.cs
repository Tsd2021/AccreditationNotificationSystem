using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model
{
    public class Buzon
    {
        public int BuzonId { get; set; }
        public string NombreBuzon { get; set; }
        public string Banco { get; set; }
        public List<CuentaBuzon> CuentasBuzones { get; set; } = new();
       
        public Buzon() { }
    }
}
