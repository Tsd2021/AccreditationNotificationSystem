using System;
using System.Collections.Generic;

namespace ANS.Web.Models.DTOs
{
    /// <summary>
    /// DTO para el resultado de un batch de acreditación por banco
    /// </summary>
    public class ResultadoBatchDto
    {
        public string Banco { get; set; }
        public int IdBanco { get; set; }
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; }
        public string RutaArchivo { get; set; }
        public string NombreArchivo { get; set; }
        public int TotalSeleccionados { get; set; }
        public int TotalInsertados { get; set; }
        public int TotalOmitidos { get; set; }
        public int TotalErrores { get; set; }
        public List<DepositoProcesadoDto> DepositosProcesados { get; set; } = new List<DepositoProcesadoDto>();
        public List<string> Errores { get; set; } = new List<string>();
    }

    /// <summary>
    /// DTO para el detalle de cada depósito procesado
    /// </summary>
    public class DepositoProcesadoDto
    {
        public int IdOperacion { get; set; }
        public string Codigo { get; set; }
        public string Estado { get; set; } // "Insertado", "Omitido", "Error"
        public string Mensaje { get; set; }
        public double Monto { get; set; }
    }
}
