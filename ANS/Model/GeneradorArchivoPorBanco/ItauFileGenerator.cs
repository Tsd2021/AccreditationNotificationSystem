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

        public async Task GenerarArchivo(List<CuentaBuzon> cb)
        {
            Console.WriteLine("Itau no hace nada");
        }
    }
}
