using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model
{
    public class TuplaMensaje
    {
        public string Estado { get; set; }
        public string Tipo { get; set; }
        public string Banco { get; set; }
        public DateTime Fecha { get; set; }
        public System.Windows.Media.Color Color { get; set; }
        public TuplaMensaje()
        {
            Fecha = DateTime.Now;

        }

    }
}
