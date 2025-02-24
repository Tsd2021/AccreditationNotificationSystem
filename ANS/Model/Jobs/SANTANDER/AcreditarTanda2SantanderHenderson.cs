﻿using ANS.Model.Interfaces;
using ANS.Model.Services;
using Quartz;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.SANTANDER
{
    [DisallowConcurrentExecution]
    public class AcreditarTanda2SantanderHenderson : IJob
    {

        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;

        public AcreditarTanda2SantanderHenderson(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Ejecutando tarea Tanda Santander", Color.FromRgb(255, 102, 102));

                });
                

                await _servicioCuentaBuzon.acreditarTanda2HendersonSantander(VariablesGlobales.horaCierreSantanderHENDERSON_TANDA2_TXT);

            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar la Tanda 2 de SANTANDER: {ex.Message}");
                //ACA GUARDAR EN UN LOG

            }
            finally
            {

                //string msgRetorno = "SUCCESS - JOB TANDA ~SANTANDER~";

                //Color colorRetorno = Color.FromRgb(76, 175, 80); // verde succcesss

                //if (e != null)
                //{

                //    msgRetorno = "ERROR - JOB TANDA ~SANTANDER~ ";

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
