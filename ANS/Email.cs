using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS
{
    public class Email
    {
        public string Correo { get; set; }
        public bool EsPrincipal { get; set; }
        public string NC { get; set; }
        public string Ciudad { get; set; }
        public string Tarea { get; set; }
        public bool Activo { get; set; }
        public string Banco { get; set; }
        public Email()
        {
        }
    }
}
