﻿using ANS.Model.Interfaces;
using Quartz.Spi;
using Quartz;
using ANS.Model.Jobs.BBVA;
using ANS.Model.Jobs.SANTANDER;
using ANS.Model.Jobs.SCOTIABANK;
using ANS.Model.Jobs.ENVIO_MASIVO;
using ANS.Model.Services;
using ANS.Model.Jobs.ITAU;
using ANS.Model.Jobs.HSBC;
using ANS.Model.Jobs.BANDES;

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
            #region BBVA_JOBS
            #region JOBS_QUE_ACREDITAN
            if (jobType == typeof(AcreditarPuntoAPuntoBBVAJob))
            {

                return new AcreditarPuntoAPuntoBBVAJob(_servicioCuentaBuzon);
            }

            if (jobType == typeof(AcreditarDiaADiaBBVAJob))
            {

                return new AcreditarDiaADiaBBVAJob(_servicioCuentaBuzon);
            }
            #endregion
            #region JOBS_QUE_ENVIAN_EXCEL
            if (jobType == typeof(ExcelBBVAReporteDiario))
            {
                return new ExcelBBVAReporteDiario(_servicioCuentaBuzon);
            }

            if (jobType == typeof(ExcelBBVATata))
            {
                return new ExcelBBVATata(_servicioCuentaBuzon);
            }
            #endregion
            #endregion
            #region SANTANDER_JOBS
            #region JOBS_QUE_ACREDITAN
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

            if (jobType == typeof(AcreditarDiaADiaSantanderDeLasSierras))
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

            #endregion
            #region JOBS_QUE_ENVIAN_EXCEL
            if (jobType == typeof(ExcelHendersonTanda1))
            {
                return new ExcelHendersonTanda1(_servicioCuentaBuzon);
            }

            if (jobType == typeof(ExcelHendersonTanda2))
            {
                return new ExcelHendersonTanda2(_servicioCuentaBuzon);
            }

            if (jobType == typeof(ExcelSantanderDiaADia))
            {
                return new ExcelSantanderDiaADia(_servicioCuentaBuzon);
            }

            if (jobType == typeof(ExcelReporteDiarioSantander))
            {
                return new ExcelReporteDiarioSantander(_servicioCuentaBuzon);
            }

            if (jobType == typeof(ExcelSantanderTesoreria1))
            {
                return new ExcelSantanderTesoreria1(_servicioCuentaBuzon);
            }

            if (jobType == typeof(ExcelSantanderTesoreria2))
            {
                return new ExcelSantanderTesoreria2(_servicioCuentaBuzon);
            }
            #endregion
            #endregion
            #region SCOTIABANK_JOBS
            #region JOBS_QUE_ACREDITAN
            if (jobType == typeof(AcreditarTanda1HendersonScotiabank))
            {
                return new AcreditarTanda1HendersonScotiabank(_servicioCuentaBuzon);
            }

            if (jobType == typeof(AcreditarTanda2HendersonScotiabank))
            {
                return new AcreditarTanda2HendersonScotiabank(_servicioCuentaBuzon);
            }

            if(jobType == typeof(AcreditarDiaADiaScotiabank))
            {
                return new AcreditarDiaADiaScotiabank(_servicioCuentaBuzon);
            }
            #endregion

            #region JOBS_QUE_ENVIAN_EXCEL
            if (jobType == typeof(ExcelTanda1HendersonScotiabank))
            {

                return new ExcelTanda1HendersonScotiabank(_servicioCuentaBuzon);
            }
            if (jobType == typeof(ExcelTanda2HendersonScotiabank))
            {

                return new ExcelTanda2HendersonScotiabank(_servicioCuentaBuzon);
            }

            if(jobType == typeof(ExcelScotiabankDiaADia))
            {
                return new ExcelScotiabankDiaADia(_servicioCuentaBuzon);
            }
            #endregion
            #endregion
            #region ITAU_JOBS
            if (jobType == typeof(AcreditarPorBancoITAU))
            {

                return new AcreditarPorBancoITAU(_servicioCuentaBuzon);
            }

            if (jobType == typeof(EnviarExcelItau))
            {

                return new EnviarExcelItau(_servicioCuentaBuzon);
            }
            #endregion
            #region HSBC_JOBS
            if (jobType == typeof(AcreditarPorBancoHSBC))
            {

                return new AcreditarPorBancoHSBC(_servicioCuentaBuzon);
            }

            if (jobType == typeof(EnviarExcelHsbc))
            {

                return new EnviarExcelHsbc(_servicioCuentaBuzon);
            }
            #endregion
            #region BANDES_JOBS
            if (jobType == typeof(AcreditarPorBancoBANDES))
            {

                return new AcreditarPorBancoBANDES(_servicioCuentaBuzon);
            }

            if (jobType == typeof(EnviarExcelBandes))
            {

                return new EnviarExcelBandes(_servicioCuentaBuzon);
            }
            #endregion
            #region ENVIO_MASIVO
            if (jobType == typeof(EnvioMasivo))
            {
                return new EnvioMasivo(ServicioEnvioMasivo.getInstancia());
            }
            #endregion
            #region ENVIO_NIVELES
            if (jobType == typeof(EnvioNiveles))
            {
                return new EnvioNiveles(ServicioNiveles.getInstancia());
            }
            #endregion
            return (IJob)Activator.CreateInstance(jobType);
        }
        public void ReturnJob(IJob job) { }
    }
}
