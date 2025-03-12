
namespace ANS.Model
{
    public class ConfiguracionAcreditacion
    {
        public int ConfigId { get; set; }
        public int CuentasBuzonesId { get; set; }
        public string TipoAcreditacion { get; set; }
        public TimeSpan? HoraDiaDia { get; set; }
        public TimeSpan? Hora1 { get; set; }
        public TimeSpan? Hora2 { get; set; }
        public int? FrecuenciaMinutos { get; set; }

        public ConfiguracionAcreditacion(string typeAccreditation)
        {
            TipoAcreditacion = typeAccreditation;
        }
    }
}
