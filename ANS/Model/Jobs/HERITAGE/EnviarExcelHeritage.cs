using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.HERITAGE

{
    [DisallowConcurrentExecution]
    public class EnviarExcelHeritage : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public EnviarExcelHeritage(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            string jobName = context.JobDetail.Key.Name;
            string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
            DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;

            Exception e = null;

            string _tarea = context.JobDetail.JobDataMap.GetString("tarea") ?? string.Empty;

            // ✅ Logging: Inicio de ejecución del job
            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC | Tarea: {_tarea}",
                $"Job: {jobName} | Group: {jobGroup} | Banco: Heritage | Tipo: Envío Excel");

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Enviando Excel Heritage", Color.FromRgb(21, 67, 96));

                });

                Banco heritage = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.heritage);

                ConfiguracionAcreditacion config = new ConfiguracionAcreditacion("DiaADia");

                await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(heritage, config, _tarea);


            }
            catch (Exception ex)
            {
                e = ex;

                Console.WriteLine($"Error al enviar excel Heritage {ex.Message}");
                ServicioLog.instancia.WriteLog(ex, "Heritage", "Excel Día a Día");

            }
            finally
            {

                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    VMmainWindow vm = main.DataContext as VMmainWindow;
                    if (vm == null)
                    {
                        vm = new VMmainWindow();

                        main.DataContext = vm;
                    }

                    Mensaje mensaje = new Mensaje();

                    mensaje.Color = Color.FromRgb(21, 67, 96);


                    mensaje.Banco = "Heritage";

                    mensaje.Tipo = "Envío Excel Heritage";

                    mensaje.Icon = PackIconKind.Bank;

                    if (e != null)
                    {

                        main.MostrarAviso("Error Job Envío Excel Heritage", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Envío Excel Heritage", Colors.Green);

                        mensaje.Estado = "Success";

                        // ✅ Logging: Finalización exitosa del job
                        ServicioLog.instancia.WriteInfo(
                            $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos | Tarea: {_tarea}",
                            $"Job: {jobName} | Group: {jobGroup} | Banco: Heritage | Tipo: Envío Excel");

                    }
                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();

                });


            }

            await Task.CompletedTask;

        }

    }
}
