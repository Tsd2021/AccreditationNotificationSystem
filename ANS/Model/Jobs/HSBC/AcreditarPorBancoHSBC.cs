using ANS.Model.Interfaces;
using ANS.Model.Services;
using Quartz;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.HSBC
{
    [DisallowConcurrentExecution]
    public class AcreditarPorBancoHSBC : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public AcreditarPorBancoHSBC(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Acreditando por banco HSBC", Color.FromRgb(255, 102, 102));

                });


                Banco hsbc = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.hsbc);

                await _servicioCuentaBuzon.acreditarDiaADiaPorBanco(hsbc);


            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar la tarea de HSBC {ex.Message}");
                //ACA GUARDAR EN UN LOG

            }
            finally
            {

                //string msgRetorno = "SUCCESS - JOB DIAXDIA ~SANTANDER~";

                //Color colorRetorno = Color.FromRgb(76, 175, 80); // verde succcesss

                //if (e != null)
                //{

                //    msgRetorno = "ERROR - JOB DIAXDIA ~SANTANDER~ ";

                //    colorRetorno = Color.FromRgb(255, 0, 0); //ROJO 
                //}

                //Application.Current.Dispatcher.Invoke(() =>
                //{
                //    MainWindow main = (MainWindow)Application.Current.MainWindow;

                //    main.OcultarAviso(msgRetorno, colorRetorno);
                //});

            }

            await Task.CompletedTask;
        }
    }
}