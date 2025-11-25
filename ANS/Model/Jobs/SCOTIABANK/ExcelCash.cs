using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using DocumentFormat.OpenXml.Bibliography;
using MaterialDesignThemes.Wpf;
using Quartz;
using System;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.SCOTIABANK

{

    [DisallowConcurrentExecution]
    public class ExcelCash : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public ExcelCash(IServicioCuentaBuzon servicioCuentaBuzon)
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
                $"Job: {jobName} | Group: {jobGroup} | Banco: Scotiabank | Tipo: Excel Cash");

            Exception e = null;
            try
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var main = (MainWindow)Application.Current.MainWindow;
                    main.MostrarAviso("Ejecutando tarea EXCEL SCBK CASH", Color.FromRgb(255, 102, 102));
                }));

                var banco = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.scotiabank);
                var desde = new TimeSpan(16, 0, 0);
                var hasta = new TimeSpan(16, 6, 0);
                var cliente = ServicioCliente.getInstancia().getById(40); // CASH
                int numTanda = 1;

                await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, cliente, banco, "MONTEVIDEO", numTanda, _tarea);
            }
            catch (Exception ex)
            {
                e = ex;
                ServicioLog.instancia.WriteLog(ex, "SCOTIABANK", "Envío excel CASH");
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
                        Color = Color.FromRgb(255, 102, 102),
                        Banco = "SCOTIABANK",
                        Tipo = "EXCEL SCOTIABANK CASH",
                        Icon = PackIconKind.Bank,
                        Estado = e != null ? "Error" : "Success"
                    };

                    main.MostrarAviso(
                        e != null ? "Error Job EXCEL CASH - SCOTIABANK" : "Success Job EXCEL CASH - SCOTIABANK",
                        e != null ? Colors.Red : Colors.Green);

                    if (e == null)
                    {
                        // ✅ Logging: Finalización exitosa del job
                        ServicioLog.instancia.WriteInfo(
                            $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos | Tarea: {_tarea}",
                            $"Job: {jobName} | Group: {jobGroup} | Banco: Scotiabank | Tipo: Excel Cash");
                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);
                    vm.CargarMensajes();
                }));
            }
        }

    }
}

