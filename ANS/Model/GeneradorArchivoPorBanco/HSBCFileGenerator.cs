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
        public async Task GenerarArchivo(List<CuentaBuzon> cb)
        {
            Console.WriteLine("Hsbc no hace nada");
        }
    }
}
