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
    public class AcreditarDiaADiaSantanderDeLasSierras : IJob
    {
        private IServicioCuentaBuzon _servicioCuentaBuzon { get; set; }
        public AcreditarDiaADiaSantanderDeLasSierras(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Ejecutando tarea DXD DE LAS SIERRAS", Color.FromRgb(255, 102, 102));

                });


                Banco bank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

                Cliente cli = ServicioCliente.getInstancia().getByNombre("DE LAS SIERRAS");

                await _servicioCuentaBuzon.acreditarDiaADiaPorCliente(cli, bank , VariablesGlobales.horaCierreSantanderDeLaSierras_TXT); // es ID 268

            }
            catch (Exception ex)
            {

                e = ex;

                Console.WriteLine($"Error al ejecutar DXD DE LAS SIERRAS: {ex.Message}");

                ServicioLog.instancia.WriteLog(ex, "Santander", "Acreditar Día a Día De las Sierras");

            }
            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "Santander";

                mensaje.Tipo = "Acreditar día a día (De las Sierras)";

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

                        main.MostrarAviso("Error Job DXD DE LAS SIERRAS", Colors.Red);

                        mensaje.Estado = "Error";

                        //escribir log error

                    }

                    else
                    {

                        main.MostrarAviso("Success Job DXD DE LAS SIERRAS", Colors.Green);

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
