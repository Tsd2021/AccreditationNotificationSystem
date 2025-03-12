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
    public class ExcelSantanderTesoreria1 : IJob
    {

        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
   

        public ExcelSantanderTesoreria1(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Ejecutando tarea EXCEL 1  SANTANDER para TESORERIA", Color.FromRgb(255, 102, 102));

                });
                Banco santander = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

                TimeSpan hasta = new TimeSpan(12, 40, 0);

              //  await _servicioCuentaBuzon.enviarExcelHenderson(hasta,henderson,santander, numTanda);

            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar EXCEL 1  SANTANDER para TESORERIA: {ex.Message}");
                //ACA GUARDAR EN UN LOG

            }

            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "SANTANDER";

                mensaje.Tipo = "Excel Santander para Tesorería 1 ";

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

                        main.MostrarAviso("Error Job Excel SANTANDER_TESORERIA 1", Colors.Red);

                        mensaje.Estado = "Error";

                        //escribir log error

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Excel SANTANDER_TESORERIA 1", Colors.Green);

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
