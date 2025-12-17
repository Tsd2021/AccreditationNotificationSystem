using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.SCOTIABANK
{
    [DisallowConcurrentExecution]
    public class CombinarTxtScotiabankPorDivisaMontevideo : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            string jobName = context.JobDetail.Key.Name;
            string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
            DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;

            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC",
                $"SCOTIABANK | Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");

            Exception e = null;

            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MainWindow main = (MainWindow)Application.Current.MainWindow;
                    main.MostrarAviso("Combinando TXT Scotiabank Montevideo por divisa", Color.FromRgb(255, 102, 102));
                });

                // Procesar UYU y USD usando el servicio con lógica de reintentos
                await ServicioCombinarTxtScotiabank.CombinarArchivosPorCiudad("MONTEVIDEO");

                ServicioLog.instancia.WriteInfo(
                    "Job completado exitosamente | Archivos combinados para UYU y USD",
                    $"SCOTIABANK | Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");
            }
            catch (Exception ex)
            {
                e = ex;
                Console.WriteLine($"Error al combinar TXT de Scotiabank Montevideo: {ex.Message}");
                ServicioLog.instancia.WriteLog(ex, "Scotiabank", "Combinar TXT Montevideo por divisa");
            }
            finally
            {
                Mensaje mensaje = new Mensaje
                {
                    Color = Color.FromRgb(255, 102, 102),
                    Banco = "Scotiabank",
                    Tipo = "Combinar TXT Montevideo por divisa",
                    Icon = PackIconKind.Bank,
                    Estado = e != null ? "Error" : "Success"
                };

                Application.Current?.Dispatcher.Invoke(() =>
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
                        main.MostrarAviso("Error Job Combinar TXT Scotiabank Montevideo", Colors.Red);
                    }
                    else
                    {
                        main.MostrarAviso("Success Job Combinar TXT Scotiabank Montevideo", Colors.Green);
                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);
                    vm.CargarMensajes();
                });
            }

            await Task.CompletedTask;
        }
    }
}

