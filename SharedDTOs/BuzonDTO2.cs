using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedDTOs
{
    public class BuzonDTO2
    {

        public string NC { get; set; }
        public string NN { get; set; }
        public string Empresa { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime Cierre { get; set; }
        public double MontoTotal { get; set; }
        public string Moneda { get; set; }
        public string Email { get; set; }
        public int Divisa { get; set; }
        public int IdOperacion { get; set; }
        public string Sucursal { get; set; }
        public long IdOperacionFinal { get; set; }
        public long IdOperacionInicio { get; set; }
        public List<AcreditacionDTO2> Acreditaciones { get; set; } = new List<AcreditacionDTO2>();
        public DateTime UltimaFechaConexion { get; set; }
        public int IdCliente { get; set; }
        public bool EsHenderson { get; set; }
        public int NumeroEnvioMasivo { get; set; }
        public string NombreWS { get; set; }
    }
}
