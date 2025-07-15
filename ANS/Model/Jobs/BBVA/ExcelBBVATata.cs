using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using DocumentFormat.OpenXml.Bibliography;
using MaterialDesignThemes.Wpf;
using Quartz;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.BBVA
{

    [DisallowConcurrentExecution]
    public class ExcelBBVATata : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public ExcelBBVATata(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }
        public async Task Execute(IJobExecutionContext context)
        {

            string _tarea = context.JobDetail.JobDataMap.GetString("tarea") ?? string.Empty;
            Exception e = null;
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea EXCEL BBVA TATA", Color.FromRgb(0, 68, 129));
                });

                Banco bbva = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva);

                TimeSpan desde = new TimeSpan(6, 30, 0);

                TimeSpan hasta = new TimeSpan(20, 30, 0);


                // ID TATA : 242

                Cliente tata = ServicioCliente.getInstancia().getById(242);

                int numTanda = 1;

                await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, tata, bbva, "MONTEVIDEO", numTanda,_tarea);

            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar la tarea de BBVA: {ex.Message}");
                //ACA GUARDAR EN UN LOG
                ServicioLog.instancia.WriteLog(ex, "BBVA", "Envío excel TATA");
            }
            finally
            {

                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    VMmainWindow vm = main.DataContext as VMmainWindow;
                    if (vm == null)
                    {
                        vm = new VMmainWindow();

                        main.DataContext = vm;
                    }

                    Mensaje mensaje = new Mensaje();

                    mensaje.Color = Color.FromRgb(0, 68, 129);

                    mensaje.Banco = "BBVA";

                    mensaje.Tipo = "EXCEL BBVA TATA";

                    mensaje.Icon = PackIconKind.Bank;

                    if (e != null)
                    {

                        main.MostrarAviso("Error Job EXCEL BBVA TATA - BBVA", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job EXCEL BBVA TATA - BBVA", Colors.Green);

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

