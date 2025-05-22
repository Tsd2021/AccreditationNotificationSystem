
namespace ANS.Model
{
    public class DtoAcreditacionesPorEmpresa
    {
        public string Empresa { get; set; }
        public double Monto { get; set; }
        public int Divisa { get; set; }
        public string Moneda { get; set; }
        public string NumeroCuenta { get; set; }
        public string Sucursal { get; set; }
        public string Ciudad { get; set; }
        public int SucursalId { get; set; }
        public string NN { get; set; }
        public string NC { get; set; }
        public int IdCliente { get; set; }


        public DtoAcreditacionesPorEmpresa()
        {
            
        }

        public void setMoneda()
        {
            if (Divisa == 1)
            {
                Moneda = "UYU";
            }

            if (Divisa == 2)
            {
                Moneda = "USD";
            }
        }
    }
}
