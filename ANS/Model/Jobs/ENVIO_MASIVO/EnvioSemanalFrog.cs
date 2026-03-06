using ANS.Model.Services;
using MaterialDesignThemes.Wpf;
using Quartz;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.ENVIO_MASIVO
{
    [DisallowConcurrentExecution]
    public class EnvioSemanalFrog : IJob
    {
        private readonly ServicioEnvioSemanalFrog _servicioEnvioSemanalFrog;

        public EnvioSemanalFrog()
        {
            _servicioEnvioSemanalFrog = new ServicioEnvioSemanalFrog();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            string jobName = context.JobDetail.Key.Name;
            string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
            DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;

            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job semanal FROG | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC",
                $"Job: {jobName} | Group: {jobGroup} | Tipo: Envío Semanal FROG");

            Exception error = null;

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow main = (MainWindow)Application.Current.MainWindow;
                    main.MostrarAviso("Ejecutando tarea ENVIO SEMANAL FROG", Color.FromRgb(0, 0, 0));
                });

                // Usamos la fecha local actual como referencia (viernes)
                await _servicioEnvioSemanalFrog.ProcesarSemanaAsync(DateTime.Today);
            }
            catch (Exception ex)
            {
                error = ex;
                ServicioLog.instancia.WriteLog(ex, "Todos", "Envío Semanal FROG");
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow main = (MainWindow)Application.Current.MainWindow;
                    VMmainWindow vm = main.DataContext as VMmainWindow ?? new VMmainWindow();
                    main.DataContext = vm;

                    Mensaje mensaje = new Mensaje
                    {
                        Color = Color.FromRgb(0, 0, 0),
                        Banco = "FROG",
                        Tipo = "ENVÍO SEMANAL FROG",
                        Icon = PackIconKind.Bank
                    };

                    if (error != null)
                    {
                        main.MostrarAviso("Error Job ENVIO SEMANAL FROG", Colors.Red);
                        mensaje.Estado = "Error";
                    }
                    else
                    {
                        main.MostrarAviso("Success Job ENVIO SEMANAL FROG", Colors.Green);
                        mensaje.Estado = "Success";

                        ServicioLog.instancia.WriteInfo(
                            $"Job semanal FROG completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos",
                            $"Job: {jobName} | Group: {jobGroup} | Tipo: Envío Semanal FROG");
                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);
                    vm.CargarMensajes();
                });
            }

            await Task.CompletedTask;
        }
    }
}

