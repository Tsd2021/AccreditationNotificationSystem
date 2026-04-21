using ANS.Model;
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
        public Task<GeneracionArchivoBancoResult> GenerarArchivo(List<CuentaBuzon> cb)
        {
            Console.WriteLine("Bandes no hace nada");
            return Task.FromResult(GeneracionArchivoBancoResult.ExitoSinRestricciones());
        }
    }
}
