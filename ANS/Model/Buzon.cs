using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model
{
    public class Buzon
    {
        public string NN { get; set; }
        public string NC { get; set; }
        public string Email { get; set; }       
        public DateTime UltimaVezConectado { get; set; }
        public double HorasDesconectado { get; set; }
        public List<Email> _listaEmails { get; set; } = new List<Email>();
        public DateTime Cierre { get; set; }
        public bool EsHenderson { get; set; }
        public Buzon() { }

        public bool estaOnline()
        {

            TimeSpan diff = DateTime.Now -  UltimaVezConectado;

            HorasDesconectado = diff.TotalHours;

            return diff <= TimeSpan.FromHours(4);

        }

    }
}
