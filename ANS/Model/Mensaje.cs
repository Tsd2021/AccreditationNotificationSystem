using MaterialDesignThemes.Wpf;

namespace ANS.Model
{
    public class Mensaje
    {
        public string Estado { get; set; }
        public string Tipo { get; set; }
        public string Banco { get; set; }
        public DateTime Fecha { get; set; }
        public System.Windows.Media.Color Color { get; set; }
        public PackIconKind Icon { get; set; }
        public Mensaje()
        {
            Fecha = DateTime.Now;
        }

    }
}
