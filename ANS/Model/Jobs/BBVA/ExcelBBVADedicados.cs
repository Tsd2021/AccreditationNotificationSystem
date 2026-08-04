using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.BBVA
{
    /// <summary>
    /// Excel consolidado de los clientes BBVA con job día a día dedicado (Nike, Mans,
    /// ROBLEFUERTE, RUTADOCE, AROCENA): una fila por cliente con el monto acreditado hoy.
    ///
    /// Dispara 14:25, después del último job dedicado (AROCENA 14:21), con margen para que
    /// termine. La lista de clientes sale de VariablesGlobales.clientesDxDDedicadosBBVA, así
    /// que al agregar uno nuevo aparece acá solo.
    ///
    /// Los destinatarios se resuelven por Email_Tarea con (Banco='BBVA', Tarea, Ciudad).
    /// </summary>
    [DisallowConcurrentExecution]
    public class ExcelBBVADedicados : IJob
    {
        private IServicioCuentaBuzon _servicioCuentaBuzon { get; set; }

        public ExcelBBVADedicados(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            string jobName = context.JobDetail.Key.Name;
            string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
            DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;

            string _tarea = context.JobDetail.JobDataMap.GetString("tarea") ?? string.Empty;
            string _ciudad = context.JobDetail.JobDataMap.GetString("ciudad") ?? "MONTEVIDEO";

            Exception e = null;

            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC | Tarea: {_tarea}",
                $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");

            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando Excel consolidado dedicados BBVA", Color.FromRgb(0, 68, 129));
                });

                await _servicioCuentaBuzon.enviarExcelConsolidadoDedicadosBBVA(_tarea, null, _ciudad);
            }
            catch (Exception ex)
            {
                e = ex;

                Console.WriteLine($"Error al ejecutar Excel consolidado dedicados BBVA: {ex.Message}");

                ServicioLog.instancia.WriteLog(ex, "BBVA", "Excel consolidado clientes dedicados");
            }
            finally
            {
                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(0, 68, 129);

                mensaje.Banco = "BBVA";

                mensaje.Tipo = "Excel consolidado dedicados";

                mensaje.Icon = PackIconKind.FileExcel;

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
                        main.MostrarAviso("Error Excel consolidado dedicados BBVA", Colors.Red);

                        mensaje.Estado = "Error";
                    }
                    else
                    {
                        main.MostrarAviso("Success Excel consolidado dedicados BBVA", Colors.Green);

                        mensaje.Estado = "Success";
                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();
                });

                if (e == null)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos",
                        $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");
                }
            }

            await Task.CompletedTask;
        }
    }
}
