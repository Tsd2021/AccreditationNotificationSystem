using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.ENVIO_MASIVO
{
    [DisallowConcurrentExecution]
    public class EnvioNiveles : IJob
    {
        private readonly ServicioNiveles _servicioNiveles;
        public EnvioNiveles(ServicioNiveles servicioNiveles)
        {
            _servicioNiveles = servicioNiveles;
        }
        public async Task Execute(IJobExecutionContext context)
        {

            JobDataMap dataMap = context.JobDetail.JobDataMap;

            int numEnvioMasivo = dataMap.GetInt("numEnvioMasivo");

            Exception e = null;

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea ENVIO NIVELES", Color.FromRgb(0, 0, 0));
                });


                await _servicioNiveles.ProcesarNotificacionesPorDesconexion();

            }
            catch (Exception ex)
            {

                e = ex;

                Console.WriteLine($"Error al ejecutar la tarea de ENVIO NIVELES: {ex.Message}");
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

                    mensaje.Banco = "TODOS LOS BUZONES";

                    mensaje.Tipo = "ENVIO NIVELES";

                    mensaje.Icon = PackIconKind.Bank;

                    if (e != null)
                    {

                        main.MostrarAviso("Error Job ENVIO NIVELES", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job ENVIO NIVELES", Colors.Green);

                        mensaje.Estado = "Success";

                    }
                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();

                });


            }

            await Task.CompletedTask;

        }
    }
}
