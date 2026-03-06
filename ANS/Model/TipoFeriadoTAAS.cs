namespace ANS.Model
{
    /// <summary>
    /// Tipo de feriado TAAS: define nombre y cantidad máxima de días por año.
    /// </summary>
    public class TipoFeriadoTAAS
    {
        public int Id { get; set; }
        public string TipoFeriado { get; set; } = string.Empty;
        public int Dias { get; set; }
    }
}
