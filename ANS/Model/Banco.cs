using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model
{
    public class Banco
    {
        public int BancoId { get; set; }
        public string NombreBanco { get; set; }
        public List<CuentaBuzon> CuentasBuzones { get; set; } = new();
    }
}
