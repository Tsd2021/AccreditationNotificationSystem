﻿using ANS.Model.Interfaces;
using ANS.Model.Services;
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

            try
            {

                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea EXCEL TANDA 2 HENDERSON", Color.FromRgb(255, 102, 102));

                });

                Banco santander = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

                TimeSpan desde = VariablesGlobales.horaCierreSantanderHENDERSON_TANDA1_TXT;

                TimeSpan hasta = VariablesGlobales.horaCierreSantanderHENDERSON_TANDA2_TXT;

                Cliente henderson = ServicioCliente.getInstancia().getByNombre("hender");

                int numTanda2 = 2;

                await _servicioCuentaBuzon.enviarExcelHenderson(desde, hasta, henderson, santander, _city , numTanda2);

            }

            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al ejecutar EXCEL HENDEROSN TANDA 2 de SANTANDER: {ex.Message}");
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
