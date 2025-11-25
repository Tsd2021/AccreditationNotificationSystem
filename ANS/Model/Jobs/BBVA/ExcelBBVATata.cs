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

    [DisallowConcurrentExecution]
    public class ExcelBBVATata : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public ExcelBBVATata(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            string jobName = context.JobDetail.Key.Name;
            string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
            DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;

            var data = context.MergedJobDataMap;
            string _tarea = data.GetString("tarea") ?? string.Empty;

            // ✅ Logging: Inicio de ejecución del job
            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC | Tarea: {_tarea}",
                $"Job: {jobName} | Group: {jobGroup} | Banco: BBVA | Tipo: Excel TATA");

            Exception e = null;
            try
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var main = (MainWindow)Application.Current.MainWindow;
                    main.MostrarAviso("Ejecutando tarea EXCEL BBVA TATA", Color.FromRgb(0, 68, 129));
                }));

                var bbva = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva);
                var desde = new TimeSpan(6, 30, 0);
                var hasta = new TimeSpan(20, 30, 0);
                var tata = ServicioCliente.getInstancia().getById(242);
                int numTanda = 1;

                await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, tata, bbva, "MONTEVIDEO", numTanda, _tarea);
            }
            catch (Exception ex)
            {
                e = ex;
                ServicioLog.instancia.WriteLog(ex, "BBVA", "Envío excel TATA");
            }
            finally
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var main = (MainWindow)Application.Current.MainWindow;
                    var vm = main.DataContext as VMmainWindow ?? new VMmainWindow();
                    main.DataContext = vm;

                    var mensaje = new Mensaje
                    {
                        Color = Color.FromRgb(0, 68, 129),
                        Banco = "BBVA",
                        Tipo = "EXCEL BBVA TATA",
                        Icon = PackIconKind.Bank,
                        Estado = e != null ? "Error" : "Success"
                    };

                    main.MostrarAviso(
                        e != null ? "Error Job EXCEL BBVA TATA - BBVA" : "Success Job EXCEL BBVA TATA - BBVA",
                        e != null ? Colors.Red : Colors.Green);

                    if (e == null)
                    {
                        // ✅ Logging: Finalización exitosa del job
                        ServicioLog.instancia.WriteInfo(
                            $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos | Tarea: {_tarea}",
                            $"Job: {jobName} | Group: {jobGroup} | Banco: BBVA | Tipo: Excel TATA");
                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);
                    vm.CargarMensajes();
                }));
            }
        }

    }
}

