using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ANS.Model.Interfaces;
using Quartz;

namespace ANS.Model.Jobs.SANTANDER
{
    [DisallowConcurrentExecution]
    public class AcreditarPuntoAPuntoSantander : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;

        public AcreditarPuntoAPuntoSantander(IServicioCuentaBuzon servicioCuentaBuzon)
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
                    main.MostrarAviso("Ejecutando tarea P2P ~SANTANDER~", Color.FromRgb(255, 102, 102));  // rojo claro
                });

                await _servicioCuentaBuzon.acreditarPuntoAPuntoPorBanco(VariablesGlobales.santander);


            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar la tarea de SANTANDER: {ex.Message}");
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
                    
                    // O si fue error:
                    
                });

            }

            await Task.CompletedTask;
        }
    }
}
