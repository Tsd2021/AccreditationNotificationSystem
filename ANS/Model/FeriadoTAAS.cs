namespace ANS.Model
{
    /// <summary>
    /// Feriado TAAS: fecha, activo/inactivo y tipo.
    /// Cuando Activo = true y la fecha es hoy, los jobs BBVA no se ejecutan.
    /// </summary>
    public class FeriadoTAAS
    {
        public int Id { get; set; }
        public DateTime Feriado { get; set; }
        public bool Activo { get; set; }
        public int IdTipoFeriado { get; set; }
    }
}



