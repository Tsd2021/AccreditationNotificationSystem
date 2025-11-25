using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.HSBC

{
    [DisallowConcurrentExecution]
    public class EnviarExcelHsbc : IJob
    {
        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;
        public EnviarExcelHsbc(IServicioCuentaBuzon servicioCuentaBuzon)
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

            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC | Tarea: {_tarea}",
                $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Enviando Excel HSBC", Color.FromRgb(123, 27, 56));

                });

                Banco hsbc = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.hsbc);

                ConfiguracionAcreditacion config = new ConfiguracionAcreditacion("DiaADia");

                await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(hsbc, config,_tarea);

            }
            catch (Exception ex)
            {
                e = ex;

                Console.WriteLine($"Error al enviar excel HSBC {ex.Message}");
                ServicioLog.instancia.WriteLog(ex, "HSBC", "Excel Día a Día");

            }
            finally
            {

                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    VMmainWindow vm = main.DataContext as VMmainWindow;
                    if (vm == null)
                    {
                        vm = new VMmainWindow();

                        main.DataContext = vm;
                    }

                    Mensaje mensaje = new Mensaje();

                    mensaje.Color = Color.FromRgb(123, 27, 56);

                    mensaje.Banco = "HSBC";

                    mensaje.Tipo = "Envío Excel HSBC";

                    mensaje.Icon = PackIconKind.Bank;

                    if (e != null)
                    {

                        main.MostrarAviso("Error Job Envío Excel HSBC", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Envío Excel HSBC", Colors.Green);

                        mensaje.Estado = "Success";

                    }
                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();

                });

                if (e == null)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos | Tarea: {_tarea}",
                        $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");
                }

            }

            await Task.CompletedTask;

        }

    }
}
