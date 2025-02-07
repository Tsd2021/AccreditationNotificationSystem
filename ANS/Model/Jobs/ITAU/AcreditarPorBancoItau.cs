using ANS.Model.Interfaces;
using ANS.Model.Services;
using MaterialDesignColors.ColorManipulation;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.ITAU
{

    [DisallowConcurrentExecution]
    public class AcreditarPorBancoITAU : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public AcreditarPorBancoITAU(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Acreditando por banco ITAU", Color.FromRgb(255, 102, 102));
                });


                Banco itau =  ServicioBanco.getInstancia().getByNombre(VariablesGlobales.itau);

                await _servicioCuentaBuzon.acreditarDiaADiaPorBanco(itau);


            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar la tarea de ITAU {ex.Message}");
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
