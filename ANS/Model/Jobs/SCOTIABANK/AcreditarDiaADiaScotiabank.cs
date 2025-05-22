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
    public class AcreditarDiaADiaScotiabank : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;

        public AcreditarDiaADiaScotiabank(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Ejecutando DXD Scotiabank", Color.FromRgb(255, 102, 102));  // rojo claro

                });


                Banco scotiabank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.scotiabank);

                await _servicioCuentaBuzon.acreditarDiaADiaPorBanco(scotiabank);


            }
            catch (Exception ex)
            {

                e = ex;

                Console.WriteLine($"Error al ejecutar la tarea de Scotiabank: {ex.Message}");

                ServicioLog.instancia.WriteLog(ex, "Scotiabank", "Acreditar Día a día");

            }

            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "Scotiabank";

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

                        main.MostrarAviso("Error Job DXD Scotiabank", Colors.Red);

                        mensaje.Estado = "Error";

                        //escribir log error

                    }

                    else
                    {

                        main.MostrarAviso("Success Job DXD Scotiabank", Colors.Green);

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
