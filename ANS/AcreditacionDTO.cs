using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS
{
    public class AcreditacionDTO
    {

        public long IdOperacion { get; set; }
        public double Monto { get; set; }
        public string Moneda { get; set; }
        public int Divisa { get; set; }
        public int IdCuenta { get; set; }
        public string NC { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaDep { get; set; }
        public string Empresa { get; set; }
        public void setMoneda()
        {
            if (Divisa == 1)
            {
                Moneda = "UYU";
            }
            if (Divisa == 2)
            {
                Moneda = "USD";
            }
        }
    }
}
