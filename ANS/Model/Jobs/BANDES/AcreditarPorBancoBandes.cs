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
    public class AcreditarPorBancoBANDES : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public AcreditarPorBancoBANDES(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Acreditando por banco BANDES", Color.FromRgb(123, 27, 56));

                });


                Banco bandes = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bandes);

                await _servicioCuentaBuzon.acreditarDiaADiaPorBanco(bandes);

            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar la tarea de BANDES {ex.Message}");
                ServicioLog.instancia.WriteLog(ex, "Bandes", "Acreditar Día a Día");
              

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

                    // Bordó (Bordeaux)
                    mensaje.Color = Color.FromRgb(123, 27, 56);

                    mensaje.Banco = "Bandes";

                    mensaje.Tipo = "Acreditar cuentas día a día";

                    mensaje.Icon = PackIconKind.Bank;

                    if (e != null)
                    {

                        main.MostrarAviso("Error Job Acreditar Día a Día Bandes", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Acreditar Día a Día Bandes", Colors.Green);

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