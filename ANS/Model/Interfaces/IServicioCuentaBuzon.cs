using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Interfaces
{
    public interface IServicioCuentaBuzon
    {

        List<CuentaBuzon> getAll();
        List<CuentaBuzon> getAllByTipoAcreditacion(string tipoAcreditacion);
        List<CuentaBuzon> getAllByTipoAcreditacionYBanco(string tipoAcreditacion, string banco);
        void acreditarPuntoAPuntoPorBanco(string bank);
        void acreditarTandaHendersonYRelacionados(TimeSpan tanda);
        void acreditarDiaADiaPorBanco(string bank);
        void acreditarTandaPorBanco(string bank);
    }
}
