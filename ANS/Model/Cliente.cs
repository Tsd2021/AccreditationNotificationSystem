using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model
{
    public class Cliente
    {

        public int IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Ciudad { get; set; }
        public List<Cliente> ClientesRelacionados { get; set; } = new List<Cliente> { };

    }
}
