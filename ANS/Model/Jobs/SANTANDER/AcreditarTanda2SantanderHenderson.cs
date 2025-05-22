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
    public class AcreditarTanda2SantanderHenderson : IJob
    {

        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;

        public AcreditarTanda2SantanderHenderson(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Ejecutando tarea Tanda Santander", Color.FromRgb(255, 102, 102));

                });

                int numTanda = 2;

                await _servicioCuentaBuzon.acreditarTandaHendersonSantander(VariablesGlobales.horaCierreSantanderHENDERSON_TANDA2_TXT,numTanda);

            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar la Tanda 2 de SANTANDER: {ex.Message}");
                //ACA GUARDAR EN UN LOG
                ServicioLog.instancia.WriteLog(ex, "Santander", "Acreditar Tanda 2");

            }
            finally
            {


                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "Santander";

                mensaje.Tipo = "Acreditar Tanda 2 (Henderson)";

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

                        main.MostrarAviso("Error Job HENDERSON_TANDA2 SANTANDER", Colors.Red);

                        mensaje.Estado = "Error";

                        //escribir log error

                    }

                    else
                    {

                        main.MostrarAviso("Success Job HENDERSON_TANDA2 SANTANDER", Colors.Green);

                        mensaje.Estado = "Success";

                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();

                    // escribir log success

                });

            }

            await Task.CompletedTask;
        }
    }
}
