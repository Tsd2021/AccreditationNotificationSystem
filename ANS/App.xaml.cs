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
using ANS.Model.Jobs.ENVIO_MASIVO;
using ANS.Model.Jobs.BANDES;
using ANS.Model.Jobs.HSBC;
using ANS.Model.Jobs.ITAU;


namespace ANS
{
    /// <summary>
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private IScheduler _scheduler;
        protected override async void OnStartup(StartupEventArgs e)
        {

            base.OnStartup(e);

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            cargarClientes();

            preCargarBancos();

            preCargarListaNC();

            preCargarEmailsTarea();

            initServicios();

            var factory = new StdSchedulerFactory();

            _scheduler = await factory.GetScheduler();

            var servicioCuentaBuzon = new ServicioCuentaBuzon();

            _scheduler.JobFactory = new MyJobFactory(servicioCuentaBuzon);

            await crearJobsBBVA(_scheduler);

            await crearJobsSantander(_scheduler);

            await crearJobsScotiabank(_scheduler);

            await crearJobsHSBC(_scheduler);

            await crearJobsBandes(_scheduler);

            await crearJobsItau(_scheduler);

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
        private void preCargarEmailsTarea()
        {
            ServicioEmailTarea.Instancia.ObtenerTodosLosEmailTarea();
        }
        private async Task crearJobsHSBC(IScheduler scheduler)
        {
            //Tarea 1: Acreditar dia a dia HSBC  (16:31:10)
            #region TAREA_ACREDITAR_DXD
            IJobDetail jobAcreditarHsbc = JobBuilder.Create<AcreditarPorBancoHSBC>()
            .WithIdentity("HSBCJobAcreditar", "GrupoTrabajoHSBC")
           
            .Build();

            ITrigger triggerAcreditarHsbc = TriggerBuilder.Create()
            .WithIdentity("HSBCTriggerAcreditar", "GrupoTrabajoHSBC")
            .WithCronSchedule("10 31 16 ? * MON-FRI")
            .Build();
            #endregion

            //Tarea 2: Enviar excel dia a dia HSBC  (16:32:10)
            #region TAREA_ENVIAR_EXCEL_DXD
            IJobDetail jobEnviarExcelHsbc = JobBuilder.Create<EnviarExcelHsbc>()
            .WithIdentity("HSBCJobEnviarExcel", "GrupoTrabajoHSBC")
            .UsingJobData("tarea", "DiaADia")
            .Build();

            ITrigger triggerEnviarExcelHsbc = TriggerBuilder.Create()
            .WithIdentity("HSBCTriggerEnviarExcel", "GrupoTrabajoHSBC")
            .WithCronSchedule("10 32 16 ? * MON-FRI")
            .Build();
            #endregion

            await _scheduler.ScheduleJob(jobAcreditarHsbc, triggerAcreditarHsbc);

            await _scheduler.ScheduleJob(jobEnviarExcelHsbc, triggerEnviarExcelHsbc);
        }
        private async Task crearJobsItau(IScheduler scheduler)
        {
            //Tarea 1: Acreditar dia a dia HSBC  (16:31:30)
            #region TAREA_ACREDITAR_DXD
            IJobDetail jobAcreditarItau = JobBuilder.Create<AcreditarPorBancoITAU>()
            .WithIdentity("ItauJobAcreditar", "GrupoTrabajoITAU")
            .Build();

            ITrigger triggerAcreditarItau = TriggerBuilder.Create()
            .WithIdentity("ItauTriggerAcreditar", "GrupoTrabajoITAU")
            .WithCronSchedule("30 31 16 ? * MON-FRI")
            .Build();
            #endregion

            //Tarea 2: Enviar excel dia a dia HSBC  (16:32:30)
            #region TAREA_ENVIAR_EXCEL_DXD
            IJobDetail jobEnviarExcelItau = JobBuilder.Create<EnviarExcelItau>()
            .WithIdentity("ItauJobEnviarExcel", "GrupoTrabajoITAU")
              .UsingJobData("tarea", "DiaADia")
            .Build();

            ITrigger triggerEnviarExcelItau = TriggerBuilder.Create()
            .WithIdentity("ItauTriggerEnviarExcel", "GrupoTrabajoITAU")
            .WithCronSchedule("30 32 16 ? * MON-FRI")
            .Build();
            #endregion

            await _scheduler.ScheduleJob(jobAcreditarItau, triggerAcreditarItau);

            await _scheduler.ScheduleJob(jobEnviarExcelItau, triggerEnviarExcelItau);
        }
        private async Task crearJobsBandes(IScheduler scheduler)
        {
            //Tarea 1: Acreditar dia a dia BANDES  (16:31:20)
            #region TAREA_ACREDITAR_DXD
            IJobDetail jobAcreditarBandes = JobBuilder.Create<AcreditarPorBancoBANDES>()
            .WithIdentity("ItauJobAcreditar", "GrupoTrabajoBANDES")
            .Build();

            ITrigger triggerAcreditarBandes = TriggerBuilder.Create()
            .WithIdentity("ItauTriggerAcreditar", "GrupoTrabajoBANDES")
            .WithCronSchedule("20 31 16 ? * MON-FRI")
            .Build();
            #endregion

            //Tarea 2: Enviar excel dia a dia HSBC  (16:32:20)
            #region TAREA_ENVIAR_EXCEL_DXD
            IJobDetail jobEnviarExcelBandes = JobBuilder.Create<EnviarExcelBandes>()
            .WithIdentity("BandesJobEnviarExcel", "GrupoTrabajoBANDES")
            .UsingJobData("tarea", "DiaADia")
            .Build();

            ITrigger triggerEnviarExcelBandes = TriggerBuilder.Create()
            .WithIdentity("BandesTriggerEnviarExcel", "GrupoTrabajoBANDES")
            .WithCronSchedule("20 32 16 ? * MON-FRI")
            .Build();
            #endregion

            await _scheduler.ScheduleJob(jobAcreditarBandes, triggerAcreditarBandes);

            await _scheduler.ScheduleJob(jobEnviarExcelBandes, triggerEnviarExcelBandes);
        }
        private async Task crearJobEnviosNiveles(IScheduler scheduler)
        {
            #region Tarea 1: ENVIO NIVELES  ( STARTS 9:10:00 ENDS 22:00:00)

            IJobDetail jobEnvioNiveles = JobBuilder.Create<EnvioNiveles>()
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
            ServicioLog.getInstancia();
            Exception e = new Exception("test");
            ServicioLog.instancia.WriteLog(e, "bank", "type");
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
        private void cargarClientes()
        {
            ServicioCliente.getInstancia().getAllClientes();
        }
        private void preCargarBancos()
        {
            var santander = new Banco(1, VariablesGlobales.santander.ToUpper())
            {
                TareasEmail = new List<string>
            {
                "Tanda1",
                "Tanda2",
                "Tesoreria1",
                "Tesoreria2",
                "DiaADia",
                "ReporteDiario"
            }
            };
            Banco scotiabank = new Banco(2, VariablesGlobales.scotiabank.ToUpper())
            {
                TareasEmail = new List<string>
            {
                "Tanda1",
                "Tanda2",
                "DiaADia"
            }
            };
            Banco hsbc = new Banco(3, VariablesGlobales.hsbc.ToUpper())
            {
                TareasEmail = new List<string>
            {
                "DiaADia"
            }
            };
            Banco bbva = new Banco(4, VariablesGlobales.bbva.ToUpper())
            {
                TareasEmail = new List<string>
            {
                "ExcelTata",
                "ReporteDiario"
            }
            };
            Banco heritage = new Banco(5, VariablesGlobales.heritage.ToUpper())
            {
                TareasEmail = new List<string>
            {
                "DiaADia"
            }
            };
            Banco brou = new Banco(6, VariablesGlobales.brou.ToUpper())
            {
                TareasEmail = new List<string>
            {
                "DiaADia",
            }
            };
            Banco itau = new Banco(7, VariablesGlobales.itau.ToUpper())
            {
                TareasEmail = new List<string>
            {
                "DiaADia"
            }
            };
            Banco bandes = new Banco(8, VariablesGlobales.bandes.ToUpper())
            {
                TareasEmail = new List<string>
            {
                "DiaADia"
            }
            };

            ServicioBanco.getInstancia().agregar(santander);
            ServicioBanco.getInstancia().agregar(scotiabank);
            ServicioBanco.getInstancia().agregar(hsbc);
            ServicioBanco.getInstancia().agregar(bbva);
            ServicioBanco.getInstancia().agregar(heritage);
            ServicioBanco.getInstancia().agregar(brou);
            ServicioBanco.getInstancia().agregar(itau);
            ServicioBanco.getInstancia().agregar(bandes);
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

            #region Tarea 2: ENVIO MASIVO 2 (15:05:00)

            IJobDetail jobEnvioMasivo2 = JobBuilder.Create<EnvioMasivo>()
                .WithIdentity("EnvioMasivo2Job", "GrupoEnvioMasivo")
                .UsingJobData("numEnvioMasivo", 2)  
                .Build();

     
            ITrigger triggerEnvioMasivo2 = TriggerBuilder.Create()
                .WithIdentity("EnvioMasivo2Trigger", "GrupoEnvioMasivo")
                .WithCronSchedule("10 05 15 ? * MON-FRI")
                .Build();
            #endregion

            #region Tarea 3: ENVIO MASIVO 3 (16:10:00)
       
            IJobDetail jobEnvioMasivo3 = JobBuilder.Create<EnvioMasivo>()
                .WithIdentity("EnvioMasivo3Job", "GrupoEnvioMasivo")
                .UsingJobData("numEnvioMasivo", 3)
                .Build();

            ITrigger triggerEnvioMasivo3 = TriggerBuilder.Create()
                .WithIdentity("EnvioMasivo3Trigger", "GrupoEnvioMasivo")
                .WithCronSchedule("0 10 16 ? * MON-FRI")
                .Build();

            #endregion

            #region Tarea 4: ENVIO MASIVO 4 (19:40:0)

            IJobDetail jobEnvioMasivo4 = JobBuilder.Create<EnvioMasivo>()
                .WithIdentity("EnvioMasivo4Job", "GrupoEnvioMasivo")
                .UsingJobData("numEnvioMasivo", 4)
                .Build();

            ITrigger triggerEnvioMasivo4 = TriggerBuilder.Create()
                .WithIdentity("EnvioMasivo4Trigger", "GrupoEnvioMasivo")
                .WithCronSchedule("0 40 19 ? * MON-FRI")
                .Build();

            #endregion

            try
            {

                await scheduler.ScheduleJob(jobEnvioMasivo1, triggerEnvioMasivo1);

                await scheduler.ScheduleJob(jobEnvioMasivo2, triggerEnvioMasivo2);

                await scheduler.ScheduleJob(jobEnvioMasivo3, triggerEnvioMasivo3);

                await scheduler.ScheduleJob(jobEnvioMasivo4, triggerEnvioMasivo4);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }  
        private async Task crearJobsScotiabank(IScheduler scheduler)
        {
            if (scheduler != null)
            {
                #region Tarea 1: ACREDITAR TANDA 1 (7:02 AM)
                // Job para acreditar (método implementado en la clase AcreditarTanda1HendersonScotiabank)
                IJobDetail jobAcreditarTanda1Scotiabank = JobBuilder.Create<AcreditarTanda1HendersonScotiabank>()
                    .WithIdentity("ScotiabankJobAcreditarTAN1", "GrupoTrabajoScotiabank")
                    .Build();

                // Trigger que dispara la ejecución a las 7:02:30 AM de lunes a viernes.
                ITrigger triggerAcreditarTanda1Scotiabank = TriggerBuilder.Create()
                    .WithIdentity("ScotiabankTriggerAcreditarTAN1", "GrupoTrabajoScotiabank")
                    .WithCronSchedule("30 2 7 ? * MON-FRI")
                    .Build();
                #endregion
                #region Tarea 2: EXCEL TANDA 1 (7:03:35 AM)
                // Job para generar Excel a partir de los registros (implementado en ExcelHendersonTanda1)
                IJobDetail jobExcelTanda1Scotiabank = JobBuilder.Create<ExcelTanda1HendersonScotiabank>()
                    .WithIdentity("ScotiabankJobExcelTAN1", "GrupoTrabajoScotiabank")
                    .UsingJobData("tarea", "Tanda1")
                    .Build();

                // Trigger que dispara la ejecución a las 7:03:35 AM de lunes a viernes.
                ITrigger triggerExcelTanda1Scotiabank = TriggerBuilder.Create()
                    .WithIdentity("ScotiabankTriggerExcelTAN1", "GrupoTrabajoScotiabank")
                    .WithCronSchedule("35 3 7 ? * MON-FRI")
                    .Build();
                #endregion
                #region Tarea 3: ACREDITAR TANDA 2 (14:50:50)
                // Job para acreditar tanda 2 (implementado en AcreditarTanda2HendersonScotiabank)
                IJobDetail jobAcreditarTanda2Scotiabank = JobBuilder.Create<AcreditarTanda2HendersonScotiabank>()
                    .WithIdentity("ScotiabankJobAcreditarTAN2", "GrupoTrabajoScotiabank")
                    .Build();

                // Trigger que dispara la ejecución a las 14:31:00 de lunes a viernes.
                ITrigger triggerAcreditarTanda2Scotiabank = TriggerBuilder.Create()
                    .WithIdentity("ScotiabankTriggerAcreditarTAN2", "GrupoTrabajoScotiabank")
                    .WithCronSchedule("50 50 14 ? * MON-FRI")
                    .Build();
                #endregion
                #region Tarea 4: EXCEL TANDA 2 (14:51:50)
                // Job para generar Excel a partir de la segunda tanda (implementado en ExcelHendersonTanda2)
                IJobDetail jobExcelTanda2Scotiabank = JobBuilder.Create<ExcelTanda2HendersonScotiabank>()
                    .WithIdentity("ScotiabankJobExcelTAN2", "GrupoTrabajoScotiabank")
                    .UsingJobData("tarea", "Tanda2")
                    .Build();

                // Trigger que dispara la ejecución a las 14:35:36 de lunes a viernes.
                ITrigger triggerExcelTanda2Scotiabank = TriggerBuilder.Create()
                    .WithIdentity("ScotiabankTriggerExcelTAN2", "GrupoTrabajoScotiabank")
                    .WithCronSchedule("50 51 14 ? * MON-FRI")
                    .Build();
                #endregion
                #region Tarea 5: Acreditar DXD (16:05:50)
                IJobDetail jobAcreditarDiaADiaScotiabank =  JobBuilder.Create<AcreditarDiaADiaScotiabank>()
                                                            .WithIdentity("ScotiabankJobAcreditarDXD", "GrupoTrabajoScotiabank")
                                                            .Build();

                // Trigger que dispara la ejecución a las 14:35:36 de lunes a viernes.
                ITrigger triggerAcreditarDiaADiaScotiabank = TriggerBuilder.Create()
                    .WithIdentity("ScotiabankTriggerAcreditarDXD", "GrupoTrabajoScotiabank")
                    .WithCronSchedule("50 5 16 ? * MON-FRI")
                    .Build();
                #endregion
                #region Tarea 6: EXCEL DXD (16:10:20)
                IJobDetail jobExcelDiaADiaScotiabank = JobBuilder.Create<ExcelScotiabankDiaADia>()
                    .WithIdentity("ScotiabankJobExcelDXD", "GrupoTrabajoScotiabank")
                    .UsingJobData("tarea","DiaADia")
                    .Build();
                ITrigger triggerExcelDiaADiaScotiabank = TriggerBuilder.Create()
                    .WithIdentity("ScotiabankTriggerExcelDXD", "GrupoTrabajoScotiabank")
                    .WithCronSchedule("20 10 16 ? * MON-FRI")
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

                    await scheduler.ScheduleJob(jobAcreditarDiaADiaScotiabank,triggerAcreditarDiaADiaScotiabank);

                    await scheduler.ScheduleJob(jobExcelDiaADiaScotiabank, triggerExcelDiaADiaScotiabank);

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

            // Tarea 2: 07:01:30 ACREDITAR TANDA 1
            #region ACREDITAR_TANDA1_HENDERSON
            IJobDetail jobTanda1Santander = JobBuilder.Create<AcreditarTanda1SantanderHenderson>()
            .WithIdentity("SantanderJobTAN1", "GrupoTrabajoSantander")
            .Build();

            ITrigger triggerTanda1Santander = TriggerBuilder.Create()
            .WithIdentity("SantanderTriggerTAN1", "GrupoTrabajoSantander")
            .WithCronSchedule("30 1 7 ? * MON-FRI") // 7:00 Lun-Vie
            .Build();
            #endregion

            // Tarea 3: 07:02:55 EXCEL TANDA 1
            #region EXCEL_TANDA1_HENDERSON
            IJobDetail jobExcelHendersonTanda1 = JobBuilder.Create<ExcelHendersonTanda1>()
                                        .WithIdentity("JobExcelHendersonTanda1", "GrupoTrabajoSantander")
                                        .UsingJobData("city", "MONTEVIDEO")
                                        .UsingJobData("tarea", "Tanda1")
                                        .Build();


            ITrigger triggerExcelHendersonTanda1 = TriggerBuilder.Create()
                                                   .WithIdentity("TriggerExcelHendersonTan1", "GrupoTrabajoSantander")
                                                   .WithCronSchedule("55 2 7 ? * MON-FRI")
                                                   .Build();
            #endregion

            // Tarea 4: 07:03:30 EXCEL PARA TESORERIA TANDA 1
            #region EXCEL_TANDA1_TESORERIA
            IJobDetail jobTanda1ExcelTesoreria = JobBuilder.Create<ExcelSantanderTesoreria1>()
            .WithIdentity("SantanderJobTan1Tesoreria", "GrupoTrabajoSantander")
             .UsingJobData("tarea", "Tesoreria1")
            .Build();


            ITrigger triggerTanda1ExcelTesoreria = TriggerBuilder.Create()
                    .WithIdentity("SantanderTriggerTan1Tesoreria", "GrupoTrabajoSantander")
                    .WithCronSchedule("30 3 7 ? * MON-FRI") // 7:03 L-V
                    .Build();
            #endregion

            // Tarea 5: 14:50:10 ACREDITAR TANDA 2
            #region ACREDITAR_TANDA2_HENDERSON
            IJobDetail jobTanda2Santander = JobBuilder.Create<AcreditarTanda2SantanderHenderson>()
            .WithIdentity("SantanderJobTAN2", "GrupoTrabajoSantander")
            .Build();


            ITrigger triggerTanda2Santander = TriggerBuilder.Create()
            .WithIdentity("SantanderTriggerTAN2", "GrupoTrabajoSantander")
            .WithCronSchedule("10 50 14 ? * MON-FRI") // 14:42:00 Lun-Vie
            .Build();
            #endregion

            // Tarea 6: 14:51:00 EXCEL TANDA 2
            #region EXCEL_TANDA2_HENDERSON
            IJobDetail jobExcelHendersonTanda2 = JobBuilder.Create<ExcelHendersonTanda2>()
                            .WithIdentity("JobExcelHendersonTanda2", "GrupoTrabajoSantander")
                            .UsingJobData("city", "MONTEVIDEO")
                             .UsingJobData("tarea", "Tanda2")
                            .Build();

            ITrigger triggerExcelHendersonTanda2 = TriggerBuilder.Create()
                                                    .WithIdentity("TriggerExcelHendersonTan2", "GrupoTrabajoSantander")
                                                    .WithCronSchedule("0 51 14 ? * MON-FRI")
                                                    .Build();
            #endregion

            // Tarea 7: 14:51:35 EXCEL PARA TESORERIA TANDA 2
            #region EXCEL_TANDA2_TESORERIA
            IJobDetail jobTanda2ExcelTesoreria = JobBuilder.Create<ExcelSantanderTesoreria2>()
                        .WithIdentity("SantanderJobTan2Tesoreria", "GrupoTrabajoSantander")
                         .UsingJobData("tarea", "Tesoreria2")
                        .Build();


            ITrigger triggerTanda2ExcelTesoreria = TriggerBuilder.Create()
                    .WithIdentity("SantanderTriggerTan2Tesoreria", "GrupoTrabajoSantander")
                    .WithCronSchedule("35 51 14 ? * MON-FRI") // 14:36:30 L-V
                    .Build();
            #endregion

            // Tarea 8: 15:50:00 ACREDITAR DIA A DIA
            #region ACREDITAR DXD Santander

            IJobDetail jobDiaADiaSantander = JobBuilder.Create<AcreditarDiaADiaSantander>().WithIdentity("SantanderJobDAD", "GrupoTrabajoSantander")
                .Build();

            ITrigger triggerDiaADiaSantander = TriggerBuilder.Create()
                .WithIdentity("SantanderTriggerDAD", "GrupoTrabajoSantander")
                .WithCronSchedule("0 50 15 ? * MON-FRI")
                .Build();


            #endregion

            // Tarea 9: 15:51:30 EXCEL DIA A DIA
            #region EXCEL_DXD_SANTANDER
            IJobDetail jobDiaADiaSantanderExcel = JobBuilder.Create<ExcelSantanderDiaADia>()
                .WithIdentity("SantanderJobExcelDAD1", "GrupoTrabajoSantander")
                .UsingJobData("tarea", "DiaADia")
                .Build();

            ITrigger triggerDiaADiaSantanderExcel = TriggerBuilder.Create()
                .WithIdentity("SantanderTriggerExcelDAD1", "GrupoTrabajoSantander")
                .WithCronSchedule("30 51 15 ? * MON-FRI")
                .Build();
            #endregion

            // Tarea 10: 15:53:00 EXCEL REPORTE DIARIO
            #region REPORTE_DIARIO

            IJobDetail jobReporteDiarioSantander = JobBuilder.Create<ExcelReporteDiarioSantander>()
                        .WithIdentity("SantanderJobReporteDiario", "GrupoTrabajoSantander")
                        .UsingJobData("tarea", "ReporteDiario")
                        .Build();

            ITrigger triggerReporteDiarioSantander = TriggerBuilder.Create()
                    .WithIdentity("SantanderTriggerReporteDiario", "GrupoTrabajoSantander")
                    .WithCronSchedule("0 53 15 ? * MON-FRI") // 15:50 L-V
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
                                                    .EndingDailyAt(TimeOfDay.HourAndMinuteOfDay(15, 46))
                                                    .OnDaysOfTheWeek(new[]
                                                    {
                                                        DayOfWeek.Monday,
                                                        DayOfWeek.Tuesday,
                                                        DayOfWeek.Wednesday,
                                                        DayOfWeek.Thursday,
                                                        DayOfWeek.Friday
                                                    })
                                                    .WithIntervalInMinutes(15))
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
                .WithSchedule(CronScheduleBuilder
                    // Segundos | Minutos    | Horas     | Day-of-month | Meses | Días-semana
                    .CronSchedule("0 15,45 8-19 ? * MON-FRI"))
                .Build();
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
            .UsingJobData("tarea", "ReporteDiario")
            .Build();

            ITrigger triggerBBVAEnviarExcelResumen = TriggerBuilder.Create()
            .WithIdentity("BBVAJTriggerReporteDiario", "GrupoTrabajoBBVA")
            .WithCronSchedule("10 05 21 ? * MON-FRI")
            .Build();
            #endregion

            //Tarea 4: Enviar excel solo Tata formato Henderson ( por nn y empresa )
            #region TAREA_EXCEL_TATA 21:06
            IJobDetail jobBBVAEnviarExcelTata = JobBuilder.Create<ExcelBBVATata>()
            .WithIdentity("BBVAJobExcelTata", "GrupoTrabajoBBVA")
            .UsingJobData("tarea", "ExcelTata")
            .Build();

            ITrigger triggerBBVAEnviarExcelTata = TriggerBuilder.Create()
            .WithIdentity("BBVAJTriggerTata", "GrupoTrabajoBBVA")
            .WithCronSchedule("40 6 21 ? * MON-FRI")
            .Build();
            #endregion

            await _scheduler.ScheduleJob(jobPuntoAPuntoBBVA, triggerBBVAPuntoAPunto);

            await _scheduler.ScheduleJob(jobBBVAEnviarExcelResumen, triggerBBVAEnviarExcelResumen);

            await _scheduler.ScheduleJob(jobBBVADiaADia, triggerBBVADiaADia);

            await _scheduler.ScheduleJob(jobBBVAEnviarExcelTata, triggerBBVAEnviarExcelTata);

        }
        protected override async void OnExit(ExitEventArgs e)
        {
            if (_scheduler != null)
            {
                await _scheduler.Shutdown();
            }

            base.OnExit(e);
        }
    }
}
