using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.SANTANDER
{
    [DisallowConcurrentExecution]
    public class AcreditarDiaADiaSantander : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public AcreditarDiaADiaSantander(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            string jobName = context.JobDetail.Key.Name;
            string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
            DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;
            
            // ✅ Logging: Inicio de ejecución del job
            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC",
                $"Job: {jobName} | Group: {jobGroup} | Banco: Santander | Tipo: Acreditar día a día");
            
            Exception e = null;
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea Día a día de SANTANDER", Color.FromRgb(255, 102, 102));

                });

                Banco bank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

                await _servicioCuentaBuzon.acreditarDiaADiaPorBanco(bank);


            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar la tarea Día a día de SANTANDER: {ex.Message}");
                ServicioLog.instancia.WriteLog(ex, "Santander", "Acreditar día a día");
                //ACA GUARDAR EN UN LOG

            }
            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "Santander";

                mensaje.Tipo = "Acreditar cuentas día a día";

                mensaje.Icon = PackIconKind.Bank;

                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    VMmainWindow vm = main.DataContext as VMmainWindow;

                    if (vm == null)
                    {
                        vm = new VMmainWindow();

                        main.DataContext = vm;
                    }

                    if (e != null)
                    {

                        main.MostrarAviso("Error Job Día a día SANTANDER", Colors.Red);

                        mensaje.Estado = "Error";

                        //escribir log error

                    }

                    else
                    {

                        main.MostrarAviso("Success Job DXD SANTANDER", Colors.Green);

                        mensaje.Estado = "Success";
                        
                        // ✅ Logging: Resumen final del job
                        double duracionSegundos = (DateTimeOffset.UtcNow - scheduledTime).TotalSeconds;
                        ServicioLog.instancia.WriteInfo(
                            $"═══════════════════════════════════════════════════════════════ | " +
                            $"JOB COMPLETADO EXITOSAMENTE | Banco: Santander | Tipo: Acreditar día a día | " +
                            $"Duración: {duracionSegundos:F2} segundos | " +
                            $"ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC | " +
                            $"Finalizado: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                            $"Job: {jobName} | Group: {jobGroup} | AcreditarDiaADiaSantander");

                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();

                });

            }

            await Task.CompletedTask;
        }
    }
}
