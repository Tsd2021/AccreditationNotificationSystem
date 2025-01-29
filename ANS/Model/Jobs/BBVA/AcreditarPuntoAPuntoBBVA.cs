using ANS.Model.Interfaces;
using Quartz;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.BBVA
{

    [DisallowConcurrentExecution]
    public class AcreditarPuntoAPuntoBBVAJob : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public AcreditarPuntoAPuntoBBVAJob(IServicioCuentaBuzon servicioCuentaBuzon)
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
                    main.MostrarAviso("Ejecutando tarea P2P ~BBVA~", Color.FromRgb(0, 68, 129));
                });

                await _servicioCuentaBuzon.acreditarPuntoAPuntoPorBanco(VariablesGlobales.bbva);

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
                    if (e != null)
                    {
                        main.MostrarAviso("ERROR - JOB P2P ~SANTANDER~", Colors.Red);
                    }
                    else
                    {
                        main.MostrarAviso("SUCCESS - JOB P2P ~SANTANDER~", Colors.Green);
                    }
                });

            }

            await Task.CompletedTask;

        }
    }
}
