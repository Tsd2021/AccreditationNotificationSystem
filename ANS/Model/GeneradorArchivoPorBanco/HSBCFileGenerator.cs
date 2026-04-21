using ANS.Model;
using ANS.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class HSBCFileGenerator : IBancoModoAcreditacion
    {
        private ConfiguracionAcreditacion _config { get; set; }

        public HSBCFileGenerator(ConfiguracionAcreditacion cfg)
        {
            _config = cfg;
        }
        public Task<GeneracionArchivoBancoResult> GenerarArchivo(List<CuentaBuzon> cb)
        {
            Console.WriteLine("Hsbc no hace nada");
            return Task.FromResult(GeneracionArchivoBancoResult.ExitoSinRestricciones());
        }
    }
}
