using Quartz;
using ANS.Model.Interfaces;
using ANS.Model.Services;
using System.Windows.Media;
using System.Windows;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;

namespace ANS.Model.Jobs.BBVA
{
    [DisallowConcurrentExecution]
    public class AcreditarDiaADiaBBVAJob : IJob
    {

        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public AcreditarDiaADiaBBVAJob(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Ejecutando tarea DIAXDIA ~BBVA~", Color.FromRgb(0, 68, 129));

                });


                Banco bbva = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva);

                await _servicioCuentaBuzon.acreditarDiaADiaPorBanco(bbva);
            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar la tarea de BBVA: {ex.Message}");
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

                    mensaje.Color = Color.FromRgb(0, 68, 129);

                    mensaje.Banco = "BBVA";

                    mensaje.Tipo = "DXD";

                    mensaje.Icon = PackIconKind.Bank;

                    if (e != null)
                    {

                        main.MostrarAviso("Error Job DXD - BBVA", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job DXD - BBVA", Colors.Green);

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

