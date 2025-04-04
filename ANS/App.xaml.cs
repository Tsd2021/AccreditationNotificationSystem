using ANS.Model.Jobs;
using Quartz.Impl;
using Quartz;
using System.Windows;
using ANS.Model.Services;
using ANS.Model.Jobs.BBVA;
using ANS.Model.Jobs.SANTANDER;
using ANS.Model;
using ANS.ViewModel;


namespace ANS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IScheduler _scheduler;
        protected override async void OnStartup(StartupEventArgs e)
        {

            base.OnStartup(e);

            cargarClientes();

            preCargarBancos();

            var factory = new StdSchedulerFactory();

            _scheduler = await factory.GetScheduler();

            var servicioCuentaBuzon = new ServicioCuentaBuzon();

            //servicioCuentaBuzon.insertarLasUltimas40Operaciones();

            _scheduler.JobFactory = new MyJobFactory(servicioCuentaBuzon);

            //await crearJobsBBVA(_scheduler);

            await crearJobsSantander(_scheduler);

            //await crearJobsScotiabank(_scheduler);

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

                Console.WriteLine("Scheduler iniciado correctamente.");
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
                IJobDetail jobPuntoAPuntoScotiabank = JobBuilder
                                                      .Create<AcreditarPuntoAPuntoScotiabank>().WithIdentity("ScotiabankJobP2P", "GrupoTrabajoScotiabank")
                                                      .Build();

                ITrigger triggerPuntoAPuntoScotiabank = TriggerBuilder.Create().WithIdentity("ScotiabankTriggerP2P", "GrupoTrabajoScotiabank")
                                                        .WithDailyTimeIntervalSchedule(x => x
                                                        .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(8, 0))
                                                        .EndingDailyAt(TimeOfDay.HourAndMinuteOfDay(19, 30))
                                                        .OnDaysOfTheWeek(new[]
                                                        {
                                                            DayOfWeek.Monday,
                                                            DayOfWeek.Tuesday,
                                                            DayOfWeek.Wednesday,
                                                            DayOfWeek.Thursday,
                                                            DayOfWeek.Friday
                                                        })
                                                        .WithIntervalInMinutes(1))
                                                        .Build();

                try
                {

                    await _scheduler.ScheduleJob(jobPuntoAPuntoScotiabank, triggerPuntoAPuntoScotiabank);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al ejecutar la tarea de SANTANDER: {ex.Message}");

                }
            }
        }
        private async Task crearJobsSantander(IScheduler scheduler)
        {

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

            #region DXD Santander

            IJobDetail jobDiaADiaSantander = JobBuilder.Create<AcreditarDiaADiaSantander>().WithIdentity("SantanderJobDAD", "GrupoTrabajoSantander")
                .Build();

            ITrigger triggerDiaADiaSantander = TriggerBuilder.Create()
                .WithIdentity("SantanderTriggerDAD", "GrupoTrabajoSantander")
                .WithCronSchedule("0 30 15 ? * MON-FRI")
                .Build();

            // ################## EXCEL DIA A DIA SANTANDER ##################

            // MONTEVIDEO

            IJobDetail jobDiaADiaSantanderExcelMontevideo = JobBuilder.Create<ExcelSantanderDiaADia>().WithIdentity("SantanderJobExcelDAD1", "GrupoTrabajoSantander").Build();

            ITrigger triggerDiaADiaSantanderExcelMontevideo = TriggerBuilder.Create()
                .WithIdentity("SantanderTriggerExcelDAD1", "GrupoTrabajoSantander")
                .WithCronSchedule("0 37 15 ? * MON-FRI")
                .Build();

            #endregion DXD Santander

            #region TANDA1_HENDERSON
            // ################## TANDA 1 HENDERSON TXT ################## //

            IJobDetail jobTanda1Santander = JobBuilder.Create<AcreditarTanda1SantanderHenderson>()
            .WithIdentity("SantanderJobTAN1", "GrupoTrabajoSantander")
            .Build();

            ITrigger triggerTanda1Santander = TriggerBuilder.Create()
            .WithIdentity("SantanderTriggerTAN1", "GrupoTrabajoSantander")
            .WithCronSchedule("0 0 7 ? * MON-FRI") // 7:00 Lun-Vie
            .Build();

            // ################## TANDA 1 HENDERSON EXCEL ################## //

            // ################## EXCEL MONTEVIDEO ################## //

            IJobDetail jobExcelHendersonTanda1Montevideo = JobBuilder.Create<ExcelHendersonTanda1>()
                                                    .WithIdentity("JobExcelHendersonTanda1Montevideo", "GrupoTrabajoSantander")
                                                    .UsingJobData("city", "MONTEVIDEO")
                                                    .Build();


            ITrigger triggerExcelHendersonTanda1Montevideo = TriggerBuilder.Create()
                                                   .WithIdentity("TriggerExcelHendersonTan1Montevideo", "GrupoTrabajoSantander")
                                                   .WithCronSchedule("0 10 7 ? * MON-FRI")
                                                   .Build();

            // ################## EXCEL MALDONADO ################## //

            IJobDetail jobExcelHendersonTanda1Maldonado = JobBuilder.Create<ExcelHendersonTanda1>()
                                        .WithIdentity("JobExcelHendersonTanda1Maldonado", "GrupoTrabajoSantander")
                                        .UsingJobData("city", "MALDONADO")
                                        .Build();



            ITrigger triggerExcelHendersonTanda1Maldonado = TriggerBuilder.Create()
                                       .WithIdentity("TriggerExcelHendersonTan1Maldonado", "GrupoTrabajoSantander")
                                       .WithCronSchedule("15 10 7 ? * MON-FRI")
                                       .Build();
            #endregion TANDA1_HENDERSON

            #region TANDA2_HENDERSON

            // ################## TANDA 2 HENDERSON TXT ################## //

            IJobDetail jobTanda2Santander = JobBuilder.Create<AcreditarTanda2SantanderHenderson>()
        .WithIdentity("SantanderJobTAN2", "GrupoTrabajoSantander")
        .Build();


            ITrigger triggerTanda2Santander = TriggerBuilder.Create()
                    .WithIdentity("SantanderTriggerTAN2", "GrupoTrabajoSantander")
                    .WithCronSchedule("0 30 14 ? * MON-FRI") // 14:30 Lun-Vie
                    .Build();

            // ################## TANDA 2 HENDERSON EXCEL ################## //

            // ##################  EXCEL MONTEVIDEO ################## //
            IJobDetail jobExcelHendersonTanda2Montevideo = JobBuilder.Create<ExcelHendersonTanda2>()
                                        .WithIdentity("JobExcelHendersonTanda2Montevideo", "GrupoTrabajoSantander")
                                        .UsingJobData("city", "MONTEVIDEO")
                                        .Build();

            ITrigger triggerExcelHendersonTanda2Montevideo = TriggerBuilder.Create()
                                                    .WithIdentity("TriggerExcelHendersonTan2Montevideo", "GrupoTrabajoSantander")
                                                    .WithCronSchedule("0 33 14 ? * MON-FRI")
                                                    .Build();

            // ##################  EXCEL MALDONADO ################## //

            IJobDetail jobExcelHendersonTanda2Maldonado = JobBuilder.Create<ExcelHendersonTanda2>()
                                            .WithIdentity("JobExcelHendersonTanda2Maldonado", "GrupoTrabajoSantander")
                                            .UsingJobData("city", "MALDONADO")
                                            .Build();


            ITrigger triggerExcelHendersonTanda2Maldonado = TriggerBuilder.Create()
                                       .WithIdentity("TriggerExcelHendersonTan2Maldonado", "GrupoTrabajoSantander")
                                       .WithCronSchedule("15 33 14 ? * MON-FRI")
                                       .Build();
            #endregion TANDA2_HENDERSON

            #region DXD DeLasSierras
            IJobDetail jobDiaADiaDeLasSierras = JobBuilder.Create<AcreditarDiaADiaSantanderDeLasSierras>().WithIdentity("SantanderDeLasSierrasJob", "GrupoTrabajoSantander")
                .Build();

            ITrigger triggerDiaADiaDeLasSierras = TriggerBuilder.Create()
                                                  .WithIdentity("SantanderDeLasSierrasTrigger", "GrupoTrabajoSantander")
                                                  .WithSchedule(CronScheduleBuilder.CronSchedule("0 59 6 ? * MON-FRI"))
                                                  .Build();
            #endregion DXD DeLasSierras

            #region TANDA1_TESORERIA
            IJobDetail jobTanda1ExcelTesoreria = JobBuilder.Create<ExcelSantanderTesoreria1>()
            .WithIdentity("SantanderJobTan1Tesoreria", "GrupoTrabajoSantander")
            .Build();


            ITrigger triggerTanda1ExcelTesoreria = TriggerBuilder.Create()
                    .WithIdentity("SantanderTriggerTAN1", "GrupoTrabajoSantander")
                    .WithCronSchedule("0 5 7 ? * MON-FRI") // 7:05 L-V
                    .Build();
            #endregion

            #region TANDA2_TESORERIA
            IJobDetail jobTanda2ExcelTesoreria = JobBuilder.Create<ExcelSantanderTesoreria1>()
                        .WithIdentity("SantanderJobTan2Tesoreria", "GrupoTrabajoSantander")
                        .Build();




            ITrigger triggerTanda2ExcelTesoreria = TriggerBuilder.Create()
                    .WithIdentity("SantanderTriggerTAN2", "GrupoTrabajoSantander")
                    .WithCronSchedule("0 32 14 ? * MON-FRI") // 14:32 L-V
                    .Build();
            #endregion

            #region REPORTE_DIARIO

            IJobDetail jobReporteDiarioSantander = JobBuilder.Create<ExcelReporteDiarioSantander>()
                        .WithIdentity("SantanderJobReporteDiario", "GrupoTrabajoSantander")
                        .Build();

            ITrigger triggerReporteDiarioSantander = TriggerBuilder.Create()
                    .WithIdentity("SantanderTriggerReporteDiario", "GrupoTrabajoSantander")
                    .WithCronSchedule("0 45 15 ? * MON-FRI") // 15:45 L-V
                    .Build();

            #endregion

            try
            {

                await _scheduler.ScheduleJob(jobDiaADiaDeLasSierras, triggerDiaADiaDeLasSierras);

                await _scheduler.ScheduleJob(jobPuntoAPuntoSantander, triggerPuntoAPuntoSantander);

                await _scheduler.ScheduleJob(jobDiaADiaSantander, triggerDiaADiaSantander);

                await _scheduler.ScheduleJob(jobDiaADiaSantanderExcelMontevideo, triggerDiaADiaSantanderExcelMontevideo);

                await _scheduler.ScheduleJob(jobTanda1Santander, triggerTanda1Santander);

                await _scheduler.ScheduleJob(jobTanda2Santander, triggerTanda2Santander);

                await _scheduler.ScheduleJob(jobExcelHendersonTanda1Montevideo, triggerExcelHendersonTanda1Montevideo);

                await _scheduler.ScheduleJob(jobExcelHendersonTanda1Maldonado, triggerExcelHendersonTanda1Maldonado);

                await _scheduler.ScheduleJob(jobExcelHendersonTanda2Montevideo, triggerExcelHendersonTanda2Montevideo);

                await _scheduler.ScheduleJob(jobExcelHendersonTanda2Maldonado, triggerExcelHendersonTanda2Maldonado);

                await _scheduler.ScheduleJob(jobReporteDiarioSantander, triggerReporteDiarioSantander);

                await _scheduler.ScheduleJob(jobTanda1ExcelTesoreria, triggerTanda1ExcelTesoreria);

                await _scheduler.ScheduleJob(jobTanda2ExcelTesoreria, triggerTanda2ExcelTesoreria);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al ejecutar la tarea de SANTANDER: {ex.Message}");

            }
        }
        private async Task crearJobsBBVA(IScheduler scheduler)
        {

            IJobDetail jobPuntoAPuntoBBVA = JobBuilder.Create<AcreditarPuntoAPuntoBBVAJob>()
                .WithIdentity("BBVAJobP2P", "GrupoTrabajoBBVA")
                .Build();

            ITrigger triggerBBVAPuntoAPunto = TriggerBuilder.Create()
                .WithIdentity("BBVATriggerP2P", "GrupoTrabajoBBVA")
                .WithDailyTimeIntervalSchedule(x => x
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(8, 0)) // Hora de inicio: 8:00 AM
                    .EndingDailyAt(TimeOfDay.HourAndMinuteOfDay(19, 30)) // Hora de fin: 15:30 PM
                    .OnDaysOfTheWeek(new[]
                    {
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday
                    })
                    .WithIntervalInMinutes(1)).Build(); // Intervalo de 1 minuto

            //IJobDetail jobBBVADiaADia = JobBuilder.Create<AcreditarDiaADiaBBVAJob>()
            //    .WithIdentity("BBVAJobDAD", "GrupoTrabajoBBVA")
            //    .Build();

            //ITrigger triggerBBVADiaADia = TriggerBuilder.Create()
            //    .WithIdentity("BBVATriggerDAD", "GrupoTrabajoBBVA")
            //    .WithCronSchedule("0 35 16 ? * MON-FRI")
            //    .Build();

            IJobDetail jobBBVADiaADia = JobBuilder.Create<AcreditarDiaADiaBBVAJob>()
                .WithIdentity("BBVAJobDAD", "GrupoTrabajoBBVA")
                .Build();

            ITrigger triggerBBVADiaADia = TriggerBuilder.Create()
                .WithIdentity("BBVATriggerDAD", "GrupoTrabajoBBVA")
                .WithCronSchedule("0 0 17 ? * MON-FRI")
                .Build();

            await _scheduler.ScheduleJob(jobPuntoAPuntoBBVA, triggerBBVAPuntoAPunto);

            await _scheduler.ScheduleJob(jobBBVADiaADia, triggerBBVADiaADia);

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
