
namespace ANS.Model
{
    public class ConfiguracionAcreditacion
    {
        public string TipoAcreditacion { get; set; }


        public ConfiguracionAcreditacion(string typeAccreditation)
        {
            TipoAcreditacion = typeAccreditation;
        }
        public ConfiguracionAcreditacion() { }
    }
}
