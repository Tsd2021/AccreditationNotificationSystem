using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.BANDES

{
    [DisallowConcurrentExecution]
    public class EnviarExcelBandes : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public EnviarExcelBandes(IServicioCuentaBuzon servicioCuentaBuzon)
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
                $"Job: {jobName} | Group: {jobGroup} | Banco: Bandes | Tipo: Envío Excel");

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Enviando Excel Bandes", Color.FromRgb(123, 27, 56));

                });

                Banco bandes = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bandes);

                ConfiguracionAcreditacion config = new ConfiguracionAcreditacion("DiaADia");

                await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(bandes, config, _tarea);
              

            }
            catch (Exception ex)
            {
                e = ex;

                Console.WriteLine($"Error al enviar excel Bandes {ex.Message}");
                ServicioLog.instancia.WriteLog(ex, "Bandes", "Excel Día a Día");

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

                    mensaje.Color = Color.FromRgb(123, 27, 56);


                    mensaje.Banco = "Bandes";

                    mensaje.Tipo = "Envío Excel Bandes";

                    mensaje.Icon = PackIconKind.Bank;

                    if (e != null)
                    {

                        main.MostrarAviso("Error Job Envío Excel Bandes", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Envío Excel Bandes", Colors.Green);

                        mensaje.Estado = "Success";

                        // ✅ Logging: Finalización exitosa del job
                        ServicioLog.instancia.WriteInfo(
                            $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos | Tarea: {_tarea}",
                            $"Job: {jobName} | Group: {jobGroup} | Banco: Bandes | Tipo: Envío Excel");

                    }
                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();

                });


            }

            await Task.CompletedTask;

        }

    }
}
