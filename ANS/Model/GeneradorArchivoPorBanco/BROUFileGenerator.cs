using ANS.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class BROUFileGenerator : IBancoModoAcreditacion
    {

        private ConfiguracionAcreditacion _config { get; set; }

        public BROUFileGenerator()
        {

        }

        public BROUFileGenerator(ConfiguracionAcreditacion config)
        {
            _config = config;
        }

        public  async Task GenerarArchivo(List<CuentaBuzon> cb)
        {
            Console.WriteLine("BROU no hace nada");
        }
    }
}
