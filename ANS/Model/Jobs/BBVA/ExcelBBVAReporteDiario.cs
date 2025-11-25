using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.SANTANDER
{
    [DisallowConcurrentExecution]
    public class ExcelBBVAReporteDiario : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public ExcelBBVAReporteDiario(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            string jobName = context.JobDetail.Key.Name;
            string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
            DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;

            Exception e = null;

            string _tarea = context.JobDetail.JobDataMap.GetString("tarea") ?? string.Empty;

            // ✅ Logging: Inicio de ejecución del job
            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC | Tarea: {_tarea}",
                $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");

            try
            {

                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea Excel Resumen Diario BBVA", Color.FromRgb(0, 68, 129));

                });
                Banco bbva = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva);

                ConfiguracionAcreditacion config = new ConfiguracionAcreditacion(VariablesGlobales.diaxdia);

                await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(bbva, config, _tarea);

            }

            catch (Exception ex)
            {

                e = ex;

                Console.WriteLine($"Error al ejecutar Resumen Diario BBVA " +  ex.Message);

                ServicioLog.instancia.WriteLog(ex, "BBVA", "Envío Excel Resumen Diario");

            }

            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(0, 68, 129);

                mensaje.Banco = "BBVA";

                mensaje.Tipo = "Excel Resumen Diario " ;

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

                        main.MostrarAviso("Error Job Excel Resumen Diario BBVA ", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Resumen Diario BBVA  ", Colors.Green);

                        mensaje.Estado = "Success";

                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();              

                });

                if (e == null)
                {
                    // ✅ Logging: Finalización exitosa del job
                    ServicioLog.instancia.WriteInfo(
                        $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos | Tarea: {_tarea}",
                        $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");
                }

            }

            await Task.CompletedTask;
        }
    }



}
