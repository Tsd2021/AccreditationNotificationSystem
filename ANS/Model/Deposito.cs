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
        public double getTotalPesos() {             double totalPesos = 0;
            foreach (var total in Totales)
            {
                if (total.Divisa == "UYU") // Suponiendo que 0 representa pesos
                {
                    totalPesos += total.ImporteTotal;
                }
            }
            return totalPesos;
        }

        public double getTotalDolares()
        {
            double totalDolares = 0;
            foreach (var total in Totales)
            {
                if (total.Divisa == "USD") // Suponiendo que 1 representa dólares
                {
                    totalDolares += total.ImporteTotal;
                }
            }
            return totalDolares;
        }
    }
}
