using ANS.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.GeneradorArchivoPorBanco
{
    public class ItauFileGenerator : IBancoModoAcreditacion
    {
        public async Task GenerarArchivo(List<CuentaBuzon> cb)
        {
            throw new NotImplementedException();
        }
    }
}
