using ANS.Model;
using ANS.Model.Interfaces;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class ItauFileGenerator : IBancoModoAcreditacion
    {
        private ConfiguracionAcreditacion _config { get; set; }
        public ItauFileGenerator() { }
        public ItauFileGenerator(ConfiguracionAcreditacion cfg)
        {
            _config = cfg;
        }

        public Task<GeneracionArchivoBancoResult> GenerarArchivo(List<CuentaBuzon> cb)
        {
            Console.WriteLine("Itau no hace nada");
            return Task.FromResult(GeneracionArchivoBancoResult.ExitoSinRestricciones());
        }
    }
}
