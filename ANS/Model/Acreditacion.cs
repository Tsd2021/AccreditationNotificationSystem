namespace ANS.Model
{
    public class Acreditacion
    {

        public int Id { get; set; }
        public string IdBuzon { get; set; }
        public long IdOperacion { get; set; }
        public DateTime Fecha { get; set; }
        public int IdBanco { get; set; }
        public int IdCuenta { get; set; }
        public int Moneda { get; set; }
        public bool No_Enviado { get; set; }
        public double Monto { get; set; } // Cambiado a double
        public string Divisa { get; set; }
        public DateTime FechaTanda { get; set; } = DateTime.MinValue;
        public void setDivisa()
        {
            if (Moneda == 0)
            {
                Divisa = "qcyo";
            }
            if (Moneda == 1)
            {
                Divisa = "que es?";
            }
            if (Moneda == 2)
            {
                Divisa = "noceee";
            }
        }
    }
}
