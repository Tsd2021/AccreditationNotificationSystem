﻿using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.SANTANDER
{
    [DisallowConcurrentExecution]
    public class ExcelSantanderDiaADia : IJob
    {

        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
   

        public ExcelSantanderDiaADia(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }


        public async Task Execute(IJobExecutionContext context)
        {

            Exception e = null;

            string _city = context.JobDetail.JobDataMap.GetString("city") ?? string.Empty;

            try
            {

                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea Excel Santander DiaADia", Color.FromRgb(255, 102, 102));

                });
                Banco santander = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);


                ConfiguracionAcreditacion config = new ConfiguracionAcreditacion("DiaADia");

                await _servicioCuentaBuzon.enviarExcelSantanderDiaADia(_city,santander,config);

            }

            catch (Exception ex)
            {

                e = ex;
                Console.WriteLine($"Error al ejecutar Excel Santander DiaADia " + _city +  ex.Message);

                //ACA GUARDAR EN UN LOG

            }

            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "SANTANDER";

                mensaje.Tipo = "Excel Día a Día " + _city;

                mensaje.Icon = PackIconKind.Bank;

                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    VMmainWindow vm = main.DataContext as VMmainWindow;

                    if (vm == null)
                    {
                        vm = new VMmainWindow();

                        main.DataContext = vm;
                    }

                    if (e != null)
                    {

                        main.MostrarAviso("Error Job Excel Excel Santander DiaADia " + _city, Colors.Red);

                        mensaje.Estado = "Error";

                        //escribir log error

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Excel Santander DiaADia  " + _city, Colors.Green);

                        mensaje.Estado = "Success";

                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();

                    // escribir log success

                });

            }

            await Task.CompletedTask;
        }
    }



}
