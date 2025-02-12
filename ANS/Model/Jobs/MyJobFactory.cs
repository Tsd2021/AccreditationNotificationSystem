using ANS.Model.Interfaces;
using Quartz.Spi;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ANS.Model.Jobs.BBVA;
using ANS.Model.Jobs.SANTANDER;

namespace ANS.Model.Jobs
{
    public class MyJobFactory : IJobFactory
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;

        public MyJobFactory(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            Type jobType = bundle.JobDetail.JobType;

            if (jobType == typeof(AcreditarPuntoAPuntoBBVAJob))
            {
          
                return new AcreditarPuntoAPuntoBBVAJob(_servicioCuentaBuzon);
            }

            if (jobType == typeof(AcreditarDiaADiaBBVAJob))
            {
            
                return new AcreditarDiaADiaBBVAJob(_servicioCuentaBuzon);
            }


            if (jobType == typeof(AcreditarDiaADiaSantander))
            {
              
                return new AcreditarDiaADiaSantander(_servicioCuentaBuzon);
            }

            if (jobType == typeof(AcreditarPuntoAPuntoSantander))
            {
      
                return new AcreditarPuntoAPuntoSantander(_servicioCuentaBuzon);
            }

            if (jobType == typeof(AcreditarTandaSantander))
            {
         
                return new AcreditarTandaSantander(_servicioCuentaBuzon);
            }      

            if (jobType == typeof(AcreditarPuntoAPuntoScotiabank))
            {
                return new AcreditarPuntoAPuntoScotiabank(_servicioCuentaBuzon);
            }

            if(jobType == typeof(AcreditarDiaADiaSantanderDeLasSierras))
            {
                return new AcreditarDiaADiaSantanderDeLasSierras(_servicioCuentaBuzon);
            }

            if (jobType == typeof(AcreditarTanda1SantanderHenderson))
            {
                return new AcreditarTanda1SantanderHenderson(_servicioCuentaBuzon);
            }

            if (jobType == typeof(AcreditarTanda2SantanderHenderson))
            {
                return new AcreditarTanda2SantanderHenderson(_servicioCuentaBuzon);
            }

            if (jobType == typeof(ExcelHendersonTanda1))
            {
                return new ExcelHendersonTanda1(_servicioCuentaBuzon);
            }

            if (jobType == typeof(ExcelHendersonTanda2))
            {
                return new ExcelHendersonTanda2(_servicioCuentaBuzon);
            }
            // Para otros jobs, si los hay:
            return (IJob)Activator.CreateInstance(jobType);
        }

        public void ReturnJob(IJob job) { /* normalmente vacío o dispose*/ }
    }
}
