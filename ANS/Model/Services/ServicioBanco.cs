

namespace ANS.Model.Services
{
    public class ServicioBanco
    {
        // ✅ Thread-safe: Lazy<T> garantiza una sola instancia
        // ListaBancos se carga en memoria al inicio, múltiples instancias causarían duplicación de datos
        private static readonly Lazy<ServicioBanco> _lazy = 
            new Lazy<ServicioBanco>(() => new ServicioBanco());
        
        public static ServicioBanco instancia => _lazy.Value;
        
        public List<Banco> ListaBancos { get; set; } = new List<Banco>();
        
        public static ServicioBanco getInstancia()
        {
            return _lazy.Value;
        }
        public void agregar(Banco b)
        {
            ListaBancos.Add(b);
        }
        public Banco getById(int id)
        {
            foreach (Banco b in ListaBancos)
            {
                if (b.BancoId == id)
                {
                    return b;
                }
            }
            return null;
        }
        public Banco getByNombre(string nombre)
        {
            foreach (Banco b in ListaBancos)
            {
                if (b.NombreBanco.ToUpper() == nombre.ToUpper())
                {
                    return b;
                }
            }
            return null;
        }
    }
}
