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

            string _tarea = context.JobDetail.JobDataMap.GetString("tarea") ?? string.Empty;

            try
            {

                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando Tarea: Envío Excel Tanda 2 Henderson", Color.FromRgb(255, 102, 102));

                });

                Banco santander = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

                TimeSpan desde = new TimeSpan(14, 30, 0);

                TimeSpan hasta = new TimeSpan(14, 32,0);

                Cliente henderson = ServicioCliente.getInstancia().getByNombre("hender");

                int numTanda2 = 2;

                await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde,hasta, henderson, santander, _city , numTanda2,_tarea);

            }

            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar Envío Excel Tanda 2 Henderson: {ex.Message}");
                //ACA GUARDAR EN UN LOG
                ServicioLog.instancia.WriteLog(ex, "Santander", "Excel Henderson Tanda 2");
            }

            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "Santander";

                mensaje.Tipo = "Envío Excel Tanda2";

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

                        main.MostrarAviso("Error Job Envío Excel Tanda 2 Henderson", Colors.Red);

                        mensaje.Estado = "Error";

                        //escribir log error

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Envío Excel Tanda 2 Henderson", Colors.Green);

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
