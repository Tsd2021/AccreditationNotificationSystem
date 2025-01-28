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
        Task acreditarPuntoAPuntoPorBanco(string bank);
        Task acreditarTandaHendersonYRelacionados(TimeSpan tanda);
        Task acreditarDiaADiaPorBanco(string bank);
        Task acreditarTandaPorBanco(string bank);
    }
}
