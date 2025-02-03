using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Interfaces
{
    public interface IServicioMensajeria
    {

        public void agregar(TuplaMensaje msj);
        public void quitar(TuplaMensaje msj);
    }
}
