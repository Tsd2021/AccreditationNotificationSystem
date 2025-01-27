using ANS.Model.Jobs;
using Quartz.Impl;
using Quartz;
using System.Configuration;
using System.Data;
using System.Windows;
using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.Model.Jobs.BBVA;
using ANS.Model.Jobs.SANTANDER;
using static Quartz.Logging.OperationName;

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

            var factory = new StdSchedulerFactory();

            _scheduler = await factory.GetScheduler();

            var servicioCuentaBuzon = new ServicioCuentaBuzon();

            _scheduler.JobFactory = new MyJobFactory(servicioCuentaBuzon);

            await crearJobsBBVA(_scheduler);

            await crearJobsSantander(_scheduler);

            await _scheduler.Start();

        }
        private async Task crearJobsSantander(IScheduler scheduler)
        {

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
                                                    .WithIntervalInMinutes(5))
                                                    .Build();

            IJobDetail jobDiaADiaSantander = JobBuilder.Create<AcreditarDiaADiaSantander>().WithIdentity("SantanderJobDAD", "GrupoTrabajoSantander")
                .Build();

            ITrigger triggerDiaADiaSantander = TriggerBuilder.Create()
                .WithIdentity("SantanderTriggerDAD", "GrupoTrabajoSantander")
                .WithCronSchedule("0 32 15 ? * MON-FRI")
                .Build();

            IJobDetail jobTandaSantander = JobBuilder.Create<AcreditarTandaSantander>()
                .WithIdentity("SantanderJobTAN", "GrupoTrabajoSantander")
                .Build();

            ITrigger triggerTanda1Santander = TriggerBuilder.Create()
                .WithIdentity("SantanderTriggerTAN1", "GrupoTrabajoSantander")
                .WithCronSchedule("0 0 7 ? * MON-FRI") // 7:00 de lunes a viernes
                .Build();

            ITrigger triggerTanda2Santander = TriggerBuilder.Create()
                .WithIdentity("SantanderTriggerTAN2", "GrupoTrabajoSantander")
                .WithCronSchedule("0 45 15 ? * MON-FRI") // 15:45 de lunes a viernes
                .Build();

            try
            {

                await _scheduler.ScheduleJob(jobPuntoAPuntoSantander, triggerPuntoAPuntoSantander);

                await _scheduler.ScheduleJob(jobDiaADiaSantander, triggerDiaADiaSantander);

                await _scheduler.ScheduleJob(jobTandaSantander, new HashSet<ITrigger> { triggerTanda1Santander, triggerTanda2Santander }, true);

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

                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(11, 15))

                    .EndingDailyAt(TimeOfDay.HourAndMinuteOfDay(20, 30))

                    .OnDaysOfTheWeek(new[]
                    {
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday
                    })
                    .WithIntervalInMinutes(30)
                )
                .Build();

            IJobDetail jobBBVADiaADia = JobBuilder.Create<AcreditarDiaADiaBBVAJob>()
                .WithIdentity("BBVAJobDAD", "GrupoTrabajoBBVA")
                .Build();

            ITrigger triggerBBVADiaADia = TriggerBuilder.Create()
                .WithIdentity("BBVATriggerDAD", "GrupoTrabajoBBVA")
                .WithCronSchedule("0 35 16 ? * MON-FRI")
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
