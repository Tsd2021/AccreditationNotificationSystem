
using ANS.Model;

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
            // Normaliza ambos lados: así "hsbc", "HSBC", "btg pactual" y "BTG PACTUAL"
            // resuelven al MISMO banco en memoria (no se duplica la entidad durante la migración).
            // Para el resto de bancos la normalización es un no-op (upper/trim).
            string objetivo = IdentidadBanco.Normalizar(nombre);
            foreach (Banco b in ListaBancos)
            {
                if (IdentidadBanco.Normalizar(b.NombreBanco) == objetivo)
                {
                    return b;
                }
            }
            return null;
        }
    }
}
