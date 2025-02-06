using ANS.Model.Interfaces;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class BandesFileGenerator : IBancoModoAcreditacion
    {

        private ConfiguracionAcreditacion _config;
        public BandesFileGenerator(ConfiguracionAcreditacion config)
        {
            _config = config;
        }
        public async Task GenerarArchivo(List<CuentaBuzon> cb)
        {
            throw new NotImplementedException();
        }
    }
}
