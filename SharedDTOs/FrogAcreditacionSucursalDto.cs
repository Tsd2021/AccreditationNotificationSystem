using System;

namespace SharedDTOs
{
    public class FrogAcreditacionSucursalDto
    {
        public string Buzon { get; set; } = string.Empty;
        public string NC { get; set; } = string.Empty;
        public string Sucursal { get; set; } = string.Empty;
        public long IdOperacion { get; set; }
        public DateTime FechaOperacion { get; set; }
        public DateTime FechaAcreditacion { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public int Divisa { get; set; }
        public decimal ImporteOperacion { get; set; }
        public decimal MontoTotalDia { get; set; }
        public string Empresa { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
    }
}

