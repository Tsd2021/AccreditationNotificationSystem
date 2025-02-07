

namespace ANS.Model.Services
{
    public class ServicioBanco
    {
        public static ServicioBanco instancia { get; set; }
        public List<Banco> ListaBancos { get; set; } = new List<Banco>();
        public static ServicioBanco getInstancia()
        {
            if (instancia == null)
            {
                instancia = new ServicioBanco();
            }
            return instancia;
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
