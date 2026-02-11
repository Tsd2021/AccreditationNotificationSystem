using System;

namespace ANS.Web.Models.DTOs
{
    /// <summary>
    /// DTO para mostrar depósitos con su estado de acreditación en la UI
    /// </summary>
    public class DepositoAcreditacionDto
    {
        public int IdDeposito { get; set; }
        public int IdOperacion { get; set; }
        public string Codigo { get; set; } // NC del buzón
        public string Empresa { get; set; }
        public DateTime FechaDep { get; set; }
        public string Tipo { get; set; }
        public double MontoTotal { get; set; }
        public string Moneda { get; set; } // "PESOS" o "DOLARES"
        public string Divisa { get; set; } // "UYU" o "USD"
        public int IdCuenta { get; set; }
        public string Cuenta { get; set; }
        public string Banco { get; set; }
        public int IdBanco { get; set; }
        public string Usuario { get; set; }
        
        // Estado de acreditación
        public bool IsAcreditado { get; set; }
        public DateTime? FechaAcreditacion { get; set; }
        public double? MontoAcreditado { get; set; } // Para detectar diferencias de monto

        /// <summary>True si el depósito tiene cuenta asignada (matching ServicioDeposito). Si false, no es acreditable.</summary>
        public bool HasCuentaAsignada { get; set; } = true;
    }
}
