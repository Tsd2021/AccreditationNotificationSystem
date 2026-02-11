
namespace ANS.Model
{
    public class ConfiguracionAcreditacion
    {
        public string TipoAcreditacion { get; set; }
        
        /// <summary>
        /// Indica si es una acreditación manual de excepción para Scotiabank (depósitos viejos)
        /// </summary>
        public bool EsExcepcionScotiabank { get; set; }


        public ConfiguracionAcreditacion(string typeAccreditation)
        {
            TipoAcreditacion = typeAccreditation;
            EsExcepcionScotiabank = false;
        }
        public ConfiguracionAcreditacion() { }
    }
}
