using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.BANDES

{
    [DisallowConcurrentExecution]
    public class EnviarExcelBandes : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public EnviarExcelBandes(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            Exception e = null;

            string _tarea = context.JobDetail.JobDataMap.GetString("tarea") ?? string.Empty;

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Enviando Excel Bandes", Color.FromRgb(123, 27, 56));

                });

                Banco bandes = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bandes);

                ConfiguracionAcreditacion config = new ConfiguracionAcreditacion("DiaADia");

                await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(bandes, config, _tarea);
              

            }
            catch (Exception ex)
            {
                e = ex;

                Console.WriteLine($"Error al enviar excel Bandes {ex.Message}");
                ServicioLog.instancia.WriteLog(ex, "Bandes", "Excel Día a Día");

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

                    mensaje.Color = Color.FromRgb(123, 27, 56);


                    mensaje.Banco = "Bandes";

                    mensaje.Tipo = "Envío Excel Bandes";

                    mensaje.Icon = PackIconKind.Bank;

                    if (e != null)
                    {

                        main.MostrarAviso("Error Job Envío Excel Bandes", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Envío Excel Bandes", Colors.Green);

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
