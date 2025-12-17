using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.BBVA
{
    [DisallowConcurrentExecution]
    public class AcreditarDiaADiaBBVANike : IJob
    {
        private IServicioCuentaBuzon _servicioCuentaBuzon { get; set; }
        public AcreditarDiaADiaBBVANike(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            string jobName = context.JobDetail.Key.Name;
            string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
            DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;

            Exception e = null;

            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC",
                $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");

            try
            {

                Application.Current?.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea DXD BBVA NIKE", Color.FromRgb(0, 68, 129));

                });


                Banco bank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva);

                Cliente cli = ServicioCliente.getInstancia().getById(998); // Nike

                // Usar TimeSpan.Zero para que use la hora de cierre del buzón desde la BD
                await _servicioCuentaBuzon.acreditarDiaADiaPorCliente(cli, bank, TimeSpan.Zero);

            }
            catch (Exception ex)
            {

                e = ex;

                Console.WriteLine($"Error al ejecutar DXD BBVA NIKE: {ex.Message}");

                ServicioLog.instancia.WriteLog(ex, "BBVA", "Acreditar Día a Día Nike");

            }
            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(0, 68, 129);

                mensaje.Banco = "BBVA";

                mensaje.Tipo = "Acreditar día a día (Nike)";

                mensaje.Icon = PackIconKind.Bank;

                Application.Current?.Dispatcher.Invoke(() =>
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

                        main.MostrarAviso("Error Job DXD BBVA NIKE", Colors.Red);

                        mensaje.Estado = "Error";

                        //escribir log error
                   
                    }

                    else
                    {

                        main.MostrarAviso("Success Job DXD BBVA NIKE", Colors.Green);

                        mensaje.Estado = "Success";
                        
                        // ✅ Logging: Finalización exitosa del job
                        ServicioLog.instancia.WriteInfo(
                            $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos",
                            $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");

                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();

                    // escribir log success

                });

                if (e == null)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos",
                        $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");
                }

            }

            await Task.CompletedTask;
        }
    }
}
