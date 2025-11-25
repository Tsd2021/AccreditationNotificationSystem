
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.ENVIO_MASIVO
{
    [DisallowConcurrentExecution]
    public class EnvioMasivo : IJob
    {
        private readonly ServicioEnvioMasivo _servicioEnvioMasivo;
        public EnvioMasivo(ServicioEnvioMasivo servicioEnvioMasivo)
        {
            _servicioEnvioMasivo = servicioEnvioMasivo;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            string jobName = context.JobDetail.Key.Name;
            string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
            DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;
            
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            int numEnvioMasivo = dataMap.GetInt("numEnvioMasivo");
            
            // ✅ Logging: Inicio de ejecución del job
            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC | NumEnvioMasivo: {numEnvioMasivo}",
                $"Job: {jobName} | Group: {jobGroup} | Tipo: Envío Masivo");

            Exception e = null;

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea ENVIO MASIVO", Color.FromRgb(0, 0,0));
                });


                await _servicioEnvioMasivo.procesarEnvioMasivo(numEnvioMasivo);

            }
            catch (Exception ex)
            {

                e = ex;

                Console.WriteLine($"Error al ejecutar la tarea de ENVIO MASIVO: {ex.Message}");

                ServicioLog.instancia.WriteLog(ex, "Todos", $"Envío masívo {numEnvioMasivo}");
                //ACA GUARDAR EN UN LOG
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

                    mensaje.Color = Color.FromRgb(0, 0, 0);

                    mensaje.Banco = "TODOS LOS BANCOS";

                    mensaje.Tipo = "ENVÍO MASIVO";

                    mensaje.Icon = PackIconKind.Bank;

                    if (e != null)
                    {

                        main.MostrarAviso("Error Job ENVIO MASIVO", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job ENVIO MASIVO", Colors.Green);

                        mensaje.Estado = "Success";
                        
                        // ✅ Logging: Finalización exitosa del job
                        ServicioLog.instancia.WriteInfo(
                            $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos | NumEnvioMasivo: {numEnvioMasivo}",
                            $"Job: {jobName} | Group: {jobGroup} | Tipo: Envío Masivo");

                    }
                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();

                });


            }

            await Task.CompletedTask;

        }
    }
}
