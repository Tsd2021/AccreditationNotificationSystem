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
    public class ExcelHendersonTanda2 : IJob
    {

        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
     

        public ExcelHendersonTanda2(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }



        public async Task Execute(IJobExecutionContext context)
        {

            string _city = context.JobDetail.JobDataMap.GetString("city") ?? string.Empty;

            Exception e = null;

            try
            {

                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea EXCEL TANDA 2 HENDERSON", Color.FromRgb(255, 102, 102));

                });

                Banco santander = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

                TimeSpan hasta = new TimeSpan(14, 30, 0);

                Cliente henderson = ServicioCliente.getInstancia().getByNombre("hender");

                int numTanda2 = 2;

                await _servicioCuentaBuzon.enviarExcelHenderson(hasta, henderson, santander, _city , numTanda2);

            }

            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar EXCEL HENDEROSN TANDA 2 de SANTANDER: {ex.Message}");
                //ACA GUARDAR EN UN LOG

            }

            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "SANTANDER";

                mensaje.Tipo = "EXCEL TANDA2";

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

                        main.MostrarAviso("Error Job Excel HENDERSON_TANDA2 SANTANDER", Colors.Red);

                        mensaje.Estado = "Error";

                        //escribir log error

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Excel HENDERSON_TANDA2 SANTANDER", Colors.Green);

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
