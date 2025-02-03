using ANS.Model.Interfaces;

namespace ANS.Model.Services
{
    public class ServicioMensajeria : IServicioMensajeria
    {

        public List<TuplaMensaje> tuplaMensajes { get; set; } = new List<TuplaMensaje>();
        public static ServicioMensajeria Instancia { get; set; }

        public static ServicioMensajeria getInstancia()
        {
            if (Instancia == null)
            {
                Instancia = new ServicioMensajeria();
            }
            return Instancia;
        }

        public void agregar(TuplaMensaje tupla)
        {
            if (tupla != null)
            {
                tuplaMensajes.Add(tupla);
            }
        }

        public void quitar(TuplaMensaje tupla)
        {
            if (tupla != null)
            {
                tuplaMensajes.Remove(tupla);
            }
        }

        public List<TuplaMensaje> getMensajes()
        {
            return tuplaMensajes;
        }
    }
}
