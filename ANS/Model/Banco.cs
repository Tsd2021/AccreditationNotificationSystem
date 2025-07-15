namespace ANS.Model
{
    public class Banco
    {
        public int BancoId { get; set; }
        public string NombreBanco { get; set; }
        public Banco(int bancoId, string nombreBanco)
        {
            BancoId = bancoId;
            NombreBanco = nombreBanco;
        }


        public List<string> TareasEmail { get; set; } = new List<string>();
    }
}