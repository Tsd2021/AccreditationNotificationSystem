using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignColors.ColorManipulation;
using MaterialDesignThemes.Wpf;
using Quartz;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.ITAU

{
    [DisallowConcurrentExecution]
    public class EnviarExcelItau : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public EnviarExcelItau(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Enviando Excel Itau", Color.FromRgb(123, 27, 56));

                });

                Banco itau = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.itau);

                ConfiguracionAcreditacion config = new ConfiguracionAcreditacion("DiaADia");

                await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(itau, config);
            }
            catch (Exception ex)
            {
                e = ex;

                Console.WriteLine($"Error al enviar excel Itau {ex.Message}");
                ServicioLog.instancia.WriteLog(ex, "Itau", "Excel Día a Día");
                //ACA GUARDAR EN UN LOG

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


                    mensaje.Banco = "Itau";

                    mensaje.Tipo = "Envío Excel Itau";

                    mensaje.Icon = PackIconKind.Bank;

                    if (e != null)
                    {

                        main.MostrarAviso("Error Job Envío Excel Itau", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Envío Excel Itau", Colors.Green);

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
