using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.BBVA
{

    [DisallowConcurrentExecution]
    public class AcreditarPuntoAPuntoBBVAJob : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public AcreditarPuntoAPuntoBBVAJob(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }
        public async Task Execute(IJobExecutionContext context)
        {


            Exception e = null;
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea P2P ~BBVA~", Color.FromRgb(0, 68, 129));
                });

                await _servicioCuentaBuzon.acreditarPuntoAPuntoPorBanco(VariablesGlobales.bbva);

            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar la tarea de BBVA: {ex.Message}");
                //ACA GUARDAR EN UN LOG
            }
            finally
            {

                Application.Current.Dispatcher.Invoke(() =>
                {

                MainWindow main = (MainWindow)Application.Current.MainWindow;

                // ✅ Recuperar el ViewModel del DataContext, si es null, asignarlo manualmente

                VMmainWindow vm = main.DataContext as VMmainWindow;
                if (vm == null)
                {
                    vm = new VMmainWindow();

                    main.DataContext = vm; // 🚀 Asignar el ViewModel si no estaba
                }

                    Mensaje mensaje = new Mensaje();

                    mensaje.Color = Color.FromRgb(0, 68, 129);

                    mensaje.Banco = "BBVA";

                    mensaje.Tipo = "P2P";

                    mensaje.Icon = PackIconKind.Bank;

                    if (e != null)

                {

                    main.MostrarAviso("Error Job P2P - BBVA", Colors.Red);

                    mensaje.Estado = "Error";

                }

                else
                {

                    main.MostrarAviso("Success Job P2P - BBVA", Colors.Green);

                    mensaje.Estado = "Success";

                }
                ServicioMensajeria.getInstancia().agregar(mensaje);

                vm.CargarMensajes();

            } );


            }

            await Task.CompletedTask;

        }
    }
}
