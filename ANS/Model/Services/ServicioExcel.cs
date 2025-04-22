using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANS.Model.Services
{
    public class ServicioExcel
    {

        public static ServicioExcel instancia { get; set; }

        public static ServicioExcel getInstancia()
        {

            if (instancia == null)
            {
                instancia = new ServicioExcel();
            }
            return instancia;

        }

        #region EXCEL_SANTANDER



        #endregion

        #region EXCEL_SCOTIABANK




        #endregion

        #region EXCEL_BBVA





        #endregion


    }
}
