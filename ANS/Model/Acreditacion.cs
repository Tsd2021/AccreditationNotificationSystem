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
        public DateTime FechaDepReal { get; set; }
        // NSU a insertar en la columna NSU de la tabla de acreditaciones.
        // Solo PERMAQUIN (TipoBuzon == 3) lo trae con valor; para el resto queda null → NULL en BD.
        public int? NSU { get; set; }
        // Nombre (base, sin ruta) del archivo TXT en el que se acreditó este depósito. Columna NOMBRE_ARCHIVO.
        // Solo bancos que generan archivo (BBVA/Scotiabank; Santander en etapa 2); el resto queda null.
        public string NombreArchivo { get; set; }
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
