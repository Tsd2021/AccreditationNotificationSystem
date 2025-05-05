using ANS.Model.Jobs;
using Quartz.Impl;
using Quartz;
using System.Windows;
using ANS.Model.Services;
using ANS.Model.Jobs.BBVA;
using ANS.Model.Jobs.SANTANDER;
using ANS.Model;
using ANS.ViewModel;
using ANS.Model.Jobs.SCOTIABANK;
using System.Runtime.InteropServices;
using ANS.Model.Jobs.ENVIO_MASIVO;


namespace ANS
{
    /// <summary>
    /// </summary>
    public partial class App : Application
    {
        private IScheduler _scheduler;
        protected override async void OnStartup(StartupEventArgs e)
        {

            base.OnStartup(e);

            cargarClientes();

            preCargarBancos();

            preCargarListaNC();

            initServicios();

            var factory = new StdSchedulerFactory();

            _scheduler = await factory.GetScheduler();

            var servicioCuentaBuzon = new ServicioCuentaBuzon();

            //servicioCuentaBuzon.insertarLasUltimas40Operaciones();

            _scheduler.JobFactory = new MyJobFactory(servicioCuentaBuzon);

            await crearJobsBBVA(_scheduler);

            await crearJobsSantander(_scheduler);

            await crearJobsScotiabank(_scheduler);

            await crearJobsEnviosMasivos(_scheduler);

            await crearJobEnviosNiveles(_scheduler);

            if (!_scheduler.IsStarted)
            {
                await _scheduler.Start();

                ServicioMensajeria.getInstancia().agregar(new Mensaje
                {
                    Estado = "Éxito",
                    Tipo = "Agenda",
                    Fecha = DateTime.Now
                });

                VMmainWindow vMmain = new VMmainWindow();

                vMmain.CargarMensajes();

                //List<Tarea> tareas = await ServicioTarea.getInstancia().obtenerTareasDelScheduler(_scheduler);

                Console.WriteLine("Scheduler iniciado correctamente.");
            }

        }

        private async Task crearJobEnviosNiveles(IScheduler scheduler)
        {
            #region Tarea 1: ENVIO NIVELES  ( STARTS 9:10:00 ENDS 22:00:00)

            IJobDetail jobEnvioNiveles = JobBuilder.Create<EnvioMasivo>()
                .WithIdentity("EnvioNivelesJob", "GrupoEnvioNiveles")
                .Build();


            ITrigger triggerEnvioNiveles = TriggerBuilder.Create()
                                            .WithIdentity("EnvioNivelesTrigger", "GrupoEnvioNiveles")
                                            .WithSchedule(DailyTimeIntervalScheduleBuilder.Create()
                                                .WithIntervalInHours(6)                                 // intervalo de 6 horas
                                                .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(9, 10))   // empieza a las 09:10
                                                .EndingDailyAt(TimeOfDay.HourAndMinuteOfDay(22, 0))     // termina a las 22:00
                                                .OnMondayThroughFriday()                                // sólo L–V
                                            )
                                            .Build();

            #endregion

            try
            {
                await scheduler.ScheduleJob(jobEnvioNiveles, triggerEnvioNiveles);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void initServicios()
        {
            ServicioEmail.getInstancia();
            ServicioNiveles.getInstancia();
        }

        private void preCargarListaNC()
        {
            var s = ServicioCC.getInstancia();

            if (s != null)
            {
                s.loadCC();

                s.loadEmails();
            }
        }

        private async Task crearJobsEnviosMasivos(IScheduler scheduler)
        {
            #region Tarea 1: ENVIO MASIVO 1  (7:30:0)
         
            IJobDetail jobEnvioMasivo1 = JobBuilder.Create<EnvioMasivo>()
                .WithIdentity("EnvioMasivo1Job", "GrupoEnvioMasivo")
                .UsingJobData("numEnvioMasivo", 1)
                .Build();

         
            ITrigger triggerEnvioMasivo1 = TriggerBuilder.Create()
                .WithIdentity("EnvioMasivo1Trigger", "GrupoEnvioMasivo")
                .WithCronSchedule("0 30 7 ? * MON-FRI")
                .Build();

            #endregion

            #region Tarea 2: ENVIO MASIVO 2 (15:00:0)
      
            IJobDetail jobEnvioMasivo2 = JobBuilder.Create<EnvioMasivo>()
                .WithIdentity("EnvioMasivo2Job", "GrupoEnvioMasivo")
                .UsingJobData("numEnvioMasivo", 2)  
                .Build();

     
            ITrigger triggerEnvioMasivo2 = TriggerBuilder.Create()
                .WithIdentity("EnvioMasivo2Trigger", "GrupoEnvioMasivo")
                .WithCronSchedule("0 0 15 ? * MON-FRI")
                .Build();

            #endregion

            #region Tarea 2: ENVIO MASIVO 3 (19:00:0)
       
            IJobDetail jobEnvioMasivo3 = JobBuilder.Create<EnvioMasivo>()
                .WithIdentity("EnvioMasivo3Job", "GrupoEnvioMasivo")
                .UsingJobData("numEnvioMasivo", 3)
                .Build();

            ITrigger triggerEnvioMasivo3 = TriggerBuilder.Create()
                .WithIdentity("EnvioMasivo3Trigger", "GrupoEnvioMasivo")
                .WithCronSchedule("0 0 19 ? * MON-FRI")
                .Build();

            #endregion

            try
            {
                await scheduler.ScheduleJob(jobEnvioMasivo1, triggerEnvioMasivo1);

                await scheduler.ScheduleJob(jobEnvioMasivo2, triggerEnvioMasivo2);

                await scheduler.ScheduleJob(jobEnvioMasivo3, triggerEnvioMasivo3);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void cargarClientes()
        {
            ServicioCliente.getInstancia().getAllClientes();
        }
        private void preCargarBancos()
        {
            Banco santander = new Banco(1, VariablesGlobales.santander.ToUpper());
            Banco scotiabank = new Banco(2, VariablesGlobales.scotiabank.ToUpper());
            Banco hsbc = new Banco(3, VariablesGlobales.hsbc.ToUpper());
            Banco bbva = new Banco(4, VariablesGlobales.bbva.ToUpper());
            Banco heritage = new Banco(5, VariablesGlobales.heritage.ToUpper());
            Banco brou = new Banco(6, VariablesGlobales.brou.ToUpper());
            Banco itau = new Banco(7, VariablesGlobales.itau.ToUpper());
            Banco bandes = new Banco(8, VariablesGlobales.bandes.ToUpper());

            ServicioBanco.getInstancia().agregar(santander);
            ServicioBanco.getInstancia().agregar(scotiabank);
            ServicioBanco.getInstancia().agregar(hsbc);
            ServicioBanco.getInstancia().agregar(bbva);
            ServicioBanco.getInstancia().agregar(heritage);
            ServicioBanco.getInstancia().agregar(brou);
            ServicioBanco.getInstancia().agregar(itau);
            ServicioBanco.getInstancia().agregar(bandes);
        }
        private async Task crearJobsScotiabank(IScheduler scheduler)
        {
            if (scheduler != null)
            {
                #region Tarea 1: ACREDITAR TANDA 1 (7:00 AM)
                // Job para acreditar (método implementado en la clase AcreditarTanda1HendersonScotiabank)
                IJobDetail jobAcreditarTanda1Scotiabank = JobBuilder.Create<AcreditarTanda1HendersonScotiabank>()
                    .WithIdentity("ScotiabankJobAcreditarTAN1", "GrupoTrabajoScotiabank")
                    .Build();

                // Trigger que dispara la ejecución a las 7:02:00 AM de lunes a viernes.
                ITrigger triggerAcreditarTanda1Scotiabank = TriggerBuilder.Create()
                    .WithIdentity("ScotiabankTriggerAcreditarTAN1", "GrupoTrabajoScotiabank")
                    .WithCronSchedule("0 0 7 ? * MON-FRI")
                    .Build();
                #endregion

                #region Tarea 2: EXCEL TANDA 1 (7:01:35 AM)
                // Job para generar Excel a partir de los registros (implementado en ExcelHendersonTanda1)
                IJobDetail jobExcelTanda1Scotiabank = JobBuilder.Create<ExcelTanda1HendersonScotiabank>()
                    .WithIdentity("ScotiabankJobExcelTAN1", "GrupoTrabajoScotiabank")
                    .Build();

                // Trigger que dispara la ejecución a las 7:06:00 AM de lunes a viernes.
                ITrigger triggerExcelTanda1Scotiabank = TriggerBuilder.Create()
                    .WithIdentity("ScotiabankTriggerExcelTAN1", "GrupoTrabajoScotiabank")
                    .WithCronSchedule("35 1 7 ? * MON-FRI")
                    .Build();
                #endregion

                #region Tarea 3: ACREDITAR TANDA 2 (14:31:50)
                // Job para acreditar tanda 2 (implementado en AcreditarTanda2HendersonScotiabank)
                IJobDetail jobAcreditarTanda2Scotiabank = JobBuilder.Create<AcreditarTanda2HendersonScotiabank>()
                    .WithIdentity("ScotiabankJobAcreditarTAN2", "GrupoTrabajoScotiabank")
                    .Build();

                // Trigger que dispara la ejecución a las 14:31:00 de lunes a viernes.
                ITrigger triggerAcreditarTanda2Scotiabank = TriggerBuilder.Create()
                    .WithIdentity("ScotiabankTriggerAcreditarTAN2", "GrupoTrabajoScotiabank")
                    .WithCronSchedule("50 31 14 ? * MON-FRI")
                    .Build();
                #endregion

                #region Tarea 4: EXCEL TANDA 2 (14:33:33)
                // Job para generar Excel a partir de la segunda tanda (implementado en ExcelHendersonTanda2)
                IJobDetail jobExcelTanda2Scotiabank = JobBuilder.Create<ExcelTanda2HendersonScotiabank>()
                    .WithIdentity("ScotiabankJobExcelTAN2", "GrupoTrabajoScotiabank")
                    .Build();

                // Trigger que dispara la ejecución a las 14:33:33 de lunes a viernes.
                ITrigger triggerExcelTanda2Scotiabank = TriggerBuilder.Create()
                    .WithIdentity("ScotiabankTriggerExcelTAN2", "GrupoTrabajoScotiabank")
                    .WithCronSchedule("33 33 14 ? * MON-FRI")
                    .Build();
                #endregion


                //TODO: falta correo del cierre con todos los buzones incluidos reporte diario SOLO PARA TESORERIA 16HS MVD-MAL
                //se llama TANDA 0 , por ahora va con henderson REPORTE DIARIO

                try
                {

                    await scheduler.ScheduleJob(jobAcreditarTanda1Scotiabank, triggerAcreditarTanda1Scotiabank);

                    await scheduler.ScheduleJob(jobExcelTanda1Scotiabank, triggerExcelTanda1Scotiabank);

                    await scheduler.ScheduleJob(jobAcreditarTanda2Scotiabank, triggerAcreditarTanda2Scotiabank);

                    await scheduler.ScheduleJob(jobExcelTanda2Scotiabank, triggerExcelTanda2Scotiabank);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al ejecutar las tareas de SCOTIABANK: {ex.Message}");
                }
            }
        }
        private async Task crearJobsSantander(IScheduler scheduler)
        {

            // Tarea 1: 06:59:00 DE LAS SIERRAS
            #region DXD DeLasSierras
            IJobDetail jobDiaADiaDeLasSierras = JobBuilder.Create<AcreditarDiaADiaSantanderDeLasSierras>().WithIdentity("SantanderDeLasSierrasJob", "GrupoTrabajoSantander")
                .Build();

            ITrigger triggerDiaADiaDeLasSierras = TriggerBuilder.Create()
                                                  .WithIdentity("SantanderDeLasSierrasTrigger", "GrupoTrabajoSantander")
                                                  .WithSchedule(CronScheduleBuilder.CronSchedule("0 59 6 ? * MON-FRI"))
                                                  .Build();
            #endregion DXD DeLasSierras

            // Tarea 2: 00:07:00 ACREDITAR TANDA 1
            #region ACREDITAR_TANDA1_HENDERSON
            IJobDetail jobTanda1Santander = JobBuilder.Create<AcreditarTanda1SantanderHenderson>()
            .WithIdentity("SantanderJobTAN1", "GrupoTrabajoSantander")
            .Build();

            ITrigger triggerTanda1Santander = TriggerBuilder.Create()
            .WithIdentity("SantanderTriggerTAN1", "GrupoTrabajoSantander")
            .WithCronSchedule("0 0 7 ? * MON-FRI") // 7:00 Lun-Vie
            .Build();
            #endregion

            // Tarea 3: 07:01:30 EXCEL TANDA 1
            #region EXCEL_TANDA1_HENDERSON
            IJobDetail jobExcelHendersonTanda1 = JobBuilder.Create<ExcelHendersonTanda1>()
                                        .WithIdentity("JobExcelHendersonTanda1", "GrupoTrabajoSantander")
                                        .UsingJobData("city", "MONTEVIDEO")
                                        .Build();


            ITrigger triggerExcelHendersonTanda1 = TriggerBuilder.Create()
                                                   .WithIdentity("TriggerExcelHendersonTan1", "GrupoTrabajoSantander")
                                                   .WithCronSchedule("30 1 7 ? * MON-FRI")
                                                   .Build();
            #endregion

            // Tarea 4: 07:03:30 EXCEL PARA TESORERIA TANDA 1
            #region EXCEL_TANDA1_TESORERIA
            IJobDetail jobTanda1ExcelTesoreria = JobBuilder.Create<ExcelSantanderTesoreria1>()
            .WithIdentity("SantanderJobTan1Tesoreria", "GrupoTrabajoSantander")
            .Build();


            ITrigger triggerTanda1ExcelTesoreria = TriggerBuilder.Create()
                    .WithIdentity("SantanderTriggerTan1Tesoreria", "GrupoTrabajoSantander")
                    .WithCronSchedule("30 3 7 ? * MON-FRI") // 7:03 L-V
                    .Build();
            #endregion

            // Tarea 5: 14:30:00 ACREDITAR TANDA 2
            #region ACREDITAR_TANDA2_HENDERSON
            IJobDetail jobTanda2Santander = JobBuilder.Create<AcreditarTanda2SantanderHenderson>()
            .WithIdentity("SantanderJobTAN2", "GrupoTrabajoSantander")
            .Build();


            ITrigger triggerTanda2Santander = TriggerBuilder.Create()
            .WithIdentity("SantanderTriggerTAN2", "GrupoTrabajoSantander")
            .WithCronSchedule("0 30 14 ? * MON-FRI") // 14:30 Lun-Vie
            .Build();
            #endregion

            // Tarea 6: 14:31:30 EXCEL TANDA 2
            #region EXCEL_TANDA2_HENDERSON
            IJobDetail jobExcelHendersonTanda2 = JobBuilder.Create<ExcelHendersonTanda2>()
                            .WithIdentity("JobExcelHendersonTanda2", "GrupoTrabajoSantander")
                            .UsingJobData("city", "MONTEVIDEO")
                            .Build();

            ITrigger triggerExcelHendersonTanda2 = TriggerBuilder.Create()
                                                    .WithIdentity("TriggerExcelHendersonTan2", "GrupoTrabajoSantander")
                                                    .WithCronSchedule("30 31 14 ? * MON-FRI")
                                                    .Build();
            #endregion

            // Tarea 7: 14:32:00 EXCEL PARA TESORERIA TANDA 2
            #region EXCEL_TANDA2_TESORERIA
            IJobDetail jobTanda2ExcelTesoreria = JobBuilder.Create<ExcelSantanderTesoreria2>()
                        .WithIdentity("SantanderJobTan2Tesoreria", "GrupoTrabajoSantander")
                        .Build();


            ITrigger triggerTanda2ExcelTesoreria = TriggerBuilder.Create()
                    .WithIdentity("SantanderTriggerTan2Tesoreria", "GrupoTrabajoSantander")
                    .WithCronSchedule("0 32 14 ? * MON-FRI") // 14:32 L-V
                    .Build();
            #endregion

            // Tarea 8: 15:30:00 ACREDITAR DIA A DIA
            #region ACREDITAR DXD Santander

            IJobDetail jobDiaADiaSantander = JobBuilder.Create<AcreditarDiaADiaSantander>().WithIdentity("SantanderJobDAD", "GrupoTrabajoSantander")
                .Build();

            ITrigger triggerDiaADiaSantander = TriggerBuilder.Create()
                .WithIdentity("SantanderTriggerDAD", "GrupoTrabajoSantander")
                .WithCronSchedule("0 30 15 ? * MON-FRI")
                .Build();


            #endregion

            // Tarea 9: 15:31:30 EXCEL DIA A DIA
            #region EXCEL_DXD_SANTANDER
            IJobDetail jobDiaADiaSantanderExcel = JobBuilder.Create<ExcelSantanderDiaADia>().WithIdentity("SantanderJobExcelDAD1", "GrupoTrabajoSantander").Build();

            ITrigger triggerDiaADiaSantanderExcel = TriggerBuilder.Create()
                .WithIdentity("SantanderTriggerExcelDAD1", "GrupoTrabajoSantander")
                .WithCronSchedule("30 31 15 ? * MON-FRI")
                .Build();
            #endregion

            // Tarea 10: 15:40:00 EXCEL REPORTE DIARIO
            #region REPORTE_DIARIO

            IJobDetail jobReporteDiarioSantander = JobBuilder.Create<ExcelReporteDiarioSantander>()
                        .WithIdentity("SantanderJobReporteDiario", "GrupoTrabajoSantander")
                        .Build();

            ITrigger triggerReporteDiarioSantander = TriggerBuilder.Create()
                    .WithIdentity("SantanderTriggerReporteDiario", "GrupoTrabajoSantander")
                    .WithCronSchedule("0 40 15 ? * MON-FRI") // 15:50 L-V
                    .Build();

            #endregion

            // Tarea Continua PUNTO A PUNTO por rango de horas de 8:00 a 15:30
            #region P2P SANTANDER
            IJobDetail jobPuntoAPuntoSantander = JobBuilder.Create<AcreditarPuntoAPuntoSantander>().WithIdentity("SantanderJobP2P", "GrupoTrabajoSantander")
                .Build();

            ITrigger triggerPuntoAPuntoSantander = TriggerBuilder.Create()
                                                    .WithIdentity("SantanderTriggerP2P", "GrupoTrabajoSantander")
                                                    .WithDailyTimeIntervalSchedule(x => x
                                                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(8, 0))
                                                    .EndingDailyAt(TimeOfDay.HourAndMinuteOfDay(15, 30))
                                                    .OnDaysOfTheWeek(new[]
                                                    {
                                                        DayOfWeek.Monday,
                                                        DayOfWeek.Tuesday,
                                                        DayOfWeek.Wednesday,
                                                        DayOfWeek.Thursday,
                                                        DayOfWeek.Friday
                                                    })
                                                    .WithIntervalInMinutes(18))
                                                    .Build();
            #endregion P2P SANTANDER


            try
            {

                await scheduler.ScheduleJob(jobDiaADiaDeLasSierras, triggerDiaADiaDeLasSierras);

                await scheduler.ScheduleJob(jobPuntoAPuntoSantander, triggerPuntoAPuntoSantander);

                await scheduler.ScheduleJob(jobDiaADiaSantander, triggerDiaADiaSantander);

                await scheduler.ScheduleJob(jobDiaADiaSantanderExcel, triggerDiaADiaSantanderExcel);

                await scheduler.ScheduleJob(jobTanda1Santander, triggerTanda1Santander);

                await scheduler.ScheduleJob(jobTanda2Santander, triggerTanda2Santander);

                await scheduler.ScheduleJob(jobExcelHendersonTanda1, triggerExcelHendersonTanda1);

                await scheduler.ScheduleJob(jobExcelHendersonTanda2, triggerExcelHendersonTanda2);

                await scheduler.ScheduleJob(jobTanda1ExcelTesoreria, triggerTanda1ExcelTesoreria);

                await scheduler.ScheduleJob(jobTanda2ExcelTesoreria, triggerTanda2ExcelTesoreria);

                await scheduler.ScheduleJob(jobReporteDiarioSantander, triggerReporteDiarioSantander);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al ejecutar la tarea de SANTANDER: {ex.Message}");

            }
        }
        private async Task crearJobsBBVA(IScheduler scheduler)
        {

            //Tarea 1: Acreditar punto a punto. de 6:30 a 20:30.
            #region TAREA_ACREDITAR_P2P

            IJobDetail jobPuntoAPuntoBBVA = JobBuilder.Create<AcreditarPuntoAPuntoBBVAJob>()
                .WithIdentity("BBVAJobP2P", "GrupoTrabajoBBVA")
                .Build();

            ITrigger triggerBBVAPuntoAPunto = TriggerBuilder.Create()
                .WithIdentity("BBVATriggerP2P", "GrupoTrabajoBBVA")
                .WithDailyTimeIntervalSchedule(x => x
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(6, 30)) // Hora de inicio: 6:30 AM
                    .EndingDailyAt(TimeOfDay.HourAndMinuteOfDay(20, 30)) // Hora de fin: 19:30 PM
                    .OnDaysOfTheWeek(new[]
                    {
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday
                    })
                    .WithIntervalInMinutes(30)).Build(); // Intervalo de 30 minutos
            #endregion

            // Tarea 2: Acreditar dia a dia. 17:00
            #region TAREA_ACREDITAR_DIAADIA

            IJobDetail jobBBVADiaADia = JobBuilder.Create<AcreditarDiaADiaBBVAJob>()
                .WithIdentity("BBVAJobDAD", "GrupoTrabajoBBVA")
                .Build();

            ITrigger triggerBBVADiaADia = TriggerBuilder.Create()
                .WithIdentity("BBVATriggerDAD", "GrupoTrabajoBBVA")
                .WithCronSchedule("45 0 17 ? * MON-FRI")
                .Build();

            #endregion

            //Tarea 3: Enviar excel resumen punto a punto 21:05
            #region TAREA_EXCEL_RESUMENDIARIO
            IJobDetail jobBBVAEnviarExcelResumen = JobBuilder.Create<ExcelBBVAReporteDiario>()
            .WithIdentity("BBVAJobExcelReporteDiario", "GrupoTrabajoBBVA")
            .Build();

            ITrigger triggerBBVAEnviarExcelResumen = TriggerBuilder.Create()
            .WithIdentity("BBVAJTriggerReporteDiario", "GrupoTrabajoBBVA")
            .WithCronSchedule("0 0 21 ? * MON-FRI")
            .Build();
            #endregion

            //Tarea 4: Enviar excel solo Tata formato Henderson ( por nn y empresa )
            #region TAREA_EXCEL_TATA
            IJobDetail jobBBVAEnviarExcelTata = JobBuilder.Create<AcreditarDiaADiaBBVAJob>()
            .WithIdentity("BBVAJobExcelTata", "GrupoTrabajoBBVA")
            .Build();

            ITrigger triggerBBVAEnviarExcelTata = TriggerBuilder.Create()
            .WithIdentity("BBVAJTriggerTata", "GrupoTrabajoBBVA")
            .WithCronSchedule("0 05 21 ? * MON-FRI")
            .Build();
            #endregion

            await _scheduler.ScheduleJob(jobPuntoAPuntoBBVA, triggerBBVAPuntoAPunto);

            await _scheduler.ScheduleJob(jobBBVAEnviarExcelResumen, triggerBBVAEnviarExcelResumen);

            await _scheduler.ScheduleJob(jobBBVADiaADia, triggerBBVADiaADia);

            await _scheduler.ScheduleJob(jobBBVAEnviarExcelTata, triggerBBVAEnviarExcelTata);

        }
        protected override async void OnExit(ExitEventArgs e)
        {
            // Al salir de la app, paramos el Scheduler para liberar recursos

            if (_scheduler != null)
            {
                await _scheduler.Shutdown();
            }

            base.OnExit(e);

        }
    }
}
