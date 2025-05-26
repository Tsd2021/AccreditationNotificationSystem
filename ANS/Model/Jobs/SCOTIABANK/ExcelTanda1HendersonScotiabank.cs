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
    public class ExcelTanda1HendersonScotiabank : IJob
    {

        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;

        public ExcelTanda1HendersonScotiabank(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Ejecutando tarea EXCEL Tanda 1 Scotiabank", Color.FromRgb(255, 102, 102));

                });

                Banco scotiabank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.scotiabank);

                TimeSpan desde = new TimeSpan(7, 0, 0);

                TimeSpan hasta = new TimeSpan(7, 2, 0);



                Cliente henderson = ServicioCliente.getInstancia().getByNombre("hender");
               
                int numTanda = 1;

                await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, henderson, scotiabank, "noimporta", numTanda);
            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar EXCEL TANDA 1 de Scotiabank: {ex.Message}");

                ServicioLog.instancia.WriteLog(ex, "Scotiabank", "Excel Tanda 1 Henderson");
                //ACA GUARDAR EN UN LOG

            }
            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "Scotiabank";

                mensaje.Tipo = "Envío Excel Tanda 1";

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

                        main.MostrarAviso("Error Job EXCEL_HENDERSON_TANDA1 Scotiabank", Colors.Red);

                        mensaje.Estado = "Error";

                        //escribir log error

                    }

                    else
                    {

                        main.MostrarAviso("Success Job EXCEL_HENDERSON_TANDA1 Scotiabank", Colors.Green);

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
