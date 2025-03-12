using ANS.Model.Interfaces;

namespace ANS.Model.Services
{
    public class ServicioMensajeria : IServicioMensajeria
    {

        public List<Mensaje> tuplaMensajes { get; set; } = new List<Mensaje>();
        public static ServicioMensajeria Instancia { get; set; }
        public static ServicioMensajeria getInstancia()
        {
            if (Instancia == null)
            {
                Instancia = new ServicioMensajeria();
            }
            return Instancia;
        }

        public void agregar(Mensaje tupla)
        {
            if (tupla != null)
            {
                tuplaMensajes.Add(tupla);
            }
        }

        public void quitar(Mensaje tupla)
        {
            if (tupla != null)
            {
                tuplaMensajes.Remove(tupla);
            }
        }

        public List<Mensaje> getMensajes()
        {
            return tuplaMensajes;
        }
    }
}
