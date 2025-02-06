using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Interfaces
{
    public interface IServicioDeposito
    {
        Task asignarDepositosAlBuzon(CuentaBuzon b, int ultIdOperacion,TimeSpan hc);

    }
}
