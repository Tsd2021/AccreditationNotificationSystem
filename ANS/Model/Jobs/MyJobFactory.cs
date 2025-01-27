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
                // Aquí inyectas la dependencia
                return new AcreditarPuntoAPuntoBBVAJob(_servicioCuentaBuzon);
            }

            if (jobType == typeof(AcreditarDiaADiaBBVAJob))
            {
                // Aquí inyectas la dependencia
                return new AcreditarDiaADiaBBVAJob(_servicioCuentaBuzon);
            }


            if (jobType == typeof(AcreditarDiaADiaSantander))
            {
                // Aquí inyectas la dependencia
                return new AcreditarDiaADiaSantander(_servicioCuentaBuzon);
            }

            if (jobType == typeof(AcreditarPuntoAPuntoSantander))
            {
                // Aquí inyectas la dependencia
                return new AcreditarPuntoAPuntoSantander(_servicioCuentaBuzon);
            }

            if (jobType == typeof(AcreditarTandaSantander))
            {
                // Aquí inyectas la dependencia
                return new AcreditarTandaSantander(_servicioCuentaBuzon);
            }

            // Para otros jobs, si los hay:
            return (IJob)Activator.CreateInstance(jobType);
        }

        public void ReturnJob(IJob job) { /* normalmente vacío o dispose*/ }
    }
}
