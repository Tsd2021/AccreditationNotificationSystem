using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Services
{
    public class ServicioExcel
    {

        // ✅ Thread-safe: Lazy<T> garantiza inicialización única
        // Servicio para generación de Excel (actualmente vacío, pero preparado para futuras funcionalidades)
        private static readonly Lazy<ServicioExcel> _lazy = 
            new Lazy<ServicioExcel>(() => new ServicioExcel());
        
        public static ServicioExcel instancia => _lazy.Value;

        public static ServicioExcel getInstancia()
        {
            return _lazy.Value;
        }

        #region EXCEL_SANTANDER



        #endregion

        #region EXCEL_SCOTIABANK




        #endregion

        #region EXCEL_BBVA





        #endregion


    }
}
