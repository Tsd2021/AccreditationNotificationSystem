using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.SCOTIABANK
{
    [DisallowConcurrentExecution]
    public class AcreditarDiaADiaFarmashop : IJob
    {
        private IServicioCuentaBuzon _servicioCuentaBuzon { get; set; }
        public AcreditarDiaADiaFarmashop(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Ejecutando tarea DXD COBOE", Color.FromRgb(255, 102, 102));

                });


                Banco bank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.scotiabank);

                Cliente cli = ServicioCliente.getInstancia().getById(179); //FARMASHOP

                await _servicioCuentaBuzon.acreditarDiaADiaPorCliente(cli, bank, VariablesGlobales.horaCierreScotiabankCoboe_TXT); // es ID 179

            }
            catch (Exception ex)
            {

                e = ex;

                Console.WriteLine($"Error al ejecutar DXD COBOE: {ex.Message}");

                ServicioLog.instancia.WriteLog(ex, "Scotiabank", "Acreditar Día a Día COBOE");

            }
            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "Scotiabank";

                mensaje.Tipo = "Acreditar día a día (COBOE)";

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

                        main.MostrarAviso("Error Job DXD COBOE", Colors.Red);

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
