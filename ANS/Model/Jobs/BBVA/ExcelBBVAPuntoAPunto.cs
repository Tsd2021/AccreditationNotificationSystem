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
    public class ExcelBBVAPuntoAPunto : IJob
    {

        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
   

        public ExcelBBVAPuntoAPunto(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Ejecutando tarea EXCEL_P2P BBVA", Color.FromRgb(0, 68, 129));

                });
                Banco bbva = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva);


                ConfiguracionAcreditacion config = new ConfiguracionAcreditacion(VariablesGlobales.p2p);

                await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(bbva, config);

            }

            catch (Exception ex)
            {

                e = ex;

                Console.WriteLine($"Error al ejecutar Excel BBVA PuntoAPunto " +  ex.Message);

            }

            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(0, 68, 129);

                mensaje.Banco = "BBVA";

                mensaje.Tipo = "Excel Resumen P2P " ;

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

                        main.MostrarAviso("Error Job Excel Excel BBVA P2P " , Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Excel BBVA P2P  " , Colors.Green);

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
