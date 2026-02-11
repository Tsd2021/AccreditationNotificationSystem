using System;

namespace ANS.Model.DTOs
{
    /// <summary>
    /// DTO liviano para mostrar operaciones/depósitos del buzón en pantalla de envío manual.
    /// Usado para grilla con estado Acreditado (verde) / No acreditado (rojo).
    /// </summary>
    public class OperacionEnvioDto
    {
        public int IdOperacion { get; set; }
        public DateTime FechaDep { get; set; }
        public string Empresa { get; set; }
        public double Monto { get; set; }
        /// <summary>True si existe registro en tabla de acreditaciones (IDBUZON + IDOPERACION).</summary>
        public bool IsAcreditado { get; set; }
    }
}
