using System;
using System.Threading.Tasks;

namespace ANS.Model.Interfaces
{
    /// <summary>
    /// Proveedor de consulta "¿es feriado activo?" para una fecha.
    /// Usado por el listener Quartz BBVA para vetar ejecución en feriados.
    /// </summary>
    public interface IFeriadosProvider
    {
        /// <summary>
        /// Indica si la fecha dada es un feriado activo (cache en memoria).
        /// </summary>
        bool IsFeriadoActivo(DateTime fechaLocal);

        /// <summary>
        /// Recarga el cache desde BD (inicio de app o tras cambios).
        /// </summary>
        Task RefreshAsync();
    }
}
