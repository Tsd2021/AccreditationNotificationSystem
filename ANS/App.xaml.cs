using ANS.Model.Jobs;
using Quartz.Impl;
using Quartz;

using System.Windows;
using ANS.Model.Services;
using ANS.Model.Jobs.BBVA;
using ANS.Model.Jobs.SANTANDER;
using ANS.Model;


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

            _scheduler.JobFactory = new MyJobFactory(servicioCuentaBuzon);

            await crearJobsBBVA(_scheduler);

            await crearJobsSantander(_scheduler);

            await crearJobsScotiabank(_scheduler);

            if (!_scheduler.IsStarted)
            {
                await _scheduler.Start();

                Console.WriteLine("Scheduler iniciado correctamente.");
            }

        }

        private void cargarClientes()
        {
            ServicioCliente.getInstancia().getAllClientes();
        }

        private void preCargarBancos()
        {
            Banco santander = new Banco(1, VariablesGlobales.santander);
            Banco scotiabank = new Banco(2, VariablesGlobales.scotiabank);
            Banco hsbc = new Banco(3, VariablesGlobales.hsbc);
            Banco bbva = new Banco(4, VariablesGlobales.bbva);
            Banco heritage = new Banco(5, VariablesGlobales.heritage);
            Banco brou = new Banco(6, VariablesGlobales.brou);
            Banco itau = new Banco(7, VariablesGlobales.itau);
            Banco bandes = new Banco(8, VariablesGlobales.bandes);

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

            IJobDetail jobPuntoAPuntoSantander = JobBuilder.Create<AcreditarPuntoAPuntoSantander>().WithIdentity("SantanderJobP2P", "GrupoTrabajoSantander")
                .Build();

            ITrigger triggerPuntoAPuntoSantander = TriggerBuilder.Create()
                                                    .WithIdentity("SantanderTriggerP2P", "GrupoTrabajoSantander")
                                                    .WithDailyTimeIntervalSchedule(x => x
                                                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(8, 0))
                                                    .EndingDailyAt(TimeOfDay.HourAndMinuteOfDay(15, 29))
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

            IJobDetail jobDiaADiaDeLasSierras = JobBuilder.Create<AcreditarDiaADiaSantanderDeLasSierras>().WithIdentity("SantanderDeLasSierrasJob", "GrupoTrabajoSantander")
                .Build();

            ITrigger triggerDiaADiaDeLasSierras = TriggerBuilder.Create()
                                                  .WithIdentity("SantanderDeLasSierrasTrigger", "GrupoTrabajoSantander")
                                                  .WithSchedule(CronScheduleBuilder.CronSchedule("0 0 7 ? * MON-FRI"))
                                                  .Build();

            IJobDetail jobDiaADiaSantander = JobBuilder.Create<AcreditarDiaADiaSantander>().WithIdentity("SantanderJobDAD", "GrupoTrabajoSantander")
                .Build();

            ITrigger triggerDiaADiaSantander = TriggerBuilder.Create()
                .WithIdentity("SantanderTriggerDAD", "GrupoTrabajoSantander")
                .WithCronSchedule("0 32 15 ? * MON-FRI")
                .Build();

            IJobDetail jobTanda1Santander = JobBuilder.Create<AcreditarTanda1SantanderHenderson>()
                .WithIdentity("SantanderJobTAN1", "GrupoTrabajoSantander")
                .Build();

            IJobDetail jobTanda2Santander = JobBuilder.Create<AcreditarTanda2SantanderHenderson>()
                .WithIdentity("SantanderJobTAN2", "GrupoTrabajoSantander")
                .Build();

            ITrigger triggerTanda1Santander = TriggerBuilder.Create()
                    .WithIdentity("SantanderTriggerTAN1", "GrupoTrabajoSantander")
                    .WithCronSchedule("0 0 7 ? * MON-FRI") // 7:00 Lun-Vie
                    .Build();

            ITrigger triggerTanda2Santander = TriggerBuilder.Create()
                    .WithIdentity("SantanderTriggerTAN2", "GrupoTrabajoSantander")
                    .WithCronSchedule("0 30 14 ? * MON-FRI") // 14:30 Lun-Vie
                    .Build();


            IJobDetail jobExcelHendersonTanda1 = JobBuilder
                                                .Create<ExcelHendersonTanda1>()
                                                .WithIdentity("ExcelHendersonTanda1", "GrupoTrabajoSantander")
                                                .Build();

            ITrigger triggerExcelHendersonTanda1 = TriggerBuilder.Create()
                                                   .WithIdentity("ExcelHendersonTan1", "GrupoTrabajoSantander")
                                                   .WithCronSchedule("0 10 7 ? * MON-FRI")
                                                   .Build();


            IJobDetail jobExcelHendersonTanda2 = JobBuilder
                                    .Create<ExcelHendersonTanda1>()
                                    .WithIdentity("ExcelHendersonTanda2", "GrupoTrabajoSantander")
                                    .Build();

            ITrigger triggerExcelHendersonTanda2 = TriggerBuilder.Create()
                                                   .WithIdentity("ExcelHendersonTan2", "GrupoTrabajoSantander")
                                                   .WithCronSchedule("0 40 14 ? * MON-FRI")
                                                   .Build();

            try
            {

                await _scheduler.ScheduleJob(jobDiaADiaDeLasSierras, triggerDiaADiaDeLasSierras);

                await _scheduler.ScheduleJob(jobPuntoAPuntoSantander, triggerPuntoAPuntoSantander);

                await _scheduler.ScheduleJob(jobDiaADiaSantander, triggerDiaADiaSantander);

                await _scheduler.ScheduleJob(jobTanda2Santander, triggerTanda2Santander);

                await _scheduler.ScheduleJob(jobExcelHendersonTanda1, triggerExcelHendersonTanda1);

                await _scheduler.ScheduleJob(jobExcelHendersonTanda2, triggerExcelHendersonTanda2);

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
