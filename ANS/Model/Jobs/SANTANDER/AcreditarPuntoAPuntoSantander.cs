using System.Windows;
using System.Windows.Media;
using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;

namespace ANS.Model.Jobs.SANTANDER
{
    [DisallowConcurrentExecution]
    public class AcreditarPuntoAPuntoSantander : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;

        public AcreditarPuntoAPuntoSantander(IServicioCuentaBuzon servicioCuentaBuzon)
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
                    main.MostrarAviso("Ejecutando P2P Santander", Color.FromRgb(255, 102, 102));  // rojo claro
                });


                Banco bank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

                await _servicioCuentaBuzon.acreditarPuntoAPuntoPorBanco(bank);


            }
            catch (Exception ex)
            {

                e = ex;

                Console.WriteLine($"Error al ejecutar la tarea de SANTANDER: {ex.Message}");

            }

            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "SANTANDER";

                mensaje.Tipo = "P2P";

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

                        main.MostrarAviso("Error Job P2P SANTANDER", Colors.Red);

                        mensaje.Estado = "Error";

                        //escribir log error
                   
                    }

                    else
                    {

                        main.MostrarAviso("Success Job P2P SANTANDER", Colors.Green);

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
