using ANS.Model.Interfaces;

namespace ANS.Model.Services
{
    public class ServicioMensajeria : IServicioMensajeria
    {
        // ✅ Thread-safe: Lazy<T> garantiza inicialización única
        // Mantiene lista de mensajes en memoria, thread-safety previene pérdida de mensajes
        private static readonly Lazy<ServicioMensajeria> _lazy = 
            new Lazy<ServicioMensajeria>(() => new ServicioMensajeria());
        
        public static ServicioMensajeria Instancia => _lazy.Value;
        
        public List<Mensaje> tuplaMensajes { get; set; } = new List<Mensaje>();
        
        public static ServicioMensajeria getInstancia()
        {
            return _lazy.Value;
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
