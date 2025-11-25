using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System;
using System.Windows;
using System.Windows.Media;

namespace ANS.Model.Jobs.SCOTIABANK
{
    [DisallowConcurrentExecution]
    public class ExcelScotiabankDiaADia : IJob
    {

        private readonly IServicioCuentaBuzon _servicioCuentaBuzon;

        public ExcelScotiabankDiaADia(IServicioCuentaBuzon servicioCuentaBuzon)
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

                    main.MostrarAviso("Ejecutando tarea Excel Scotiabank DiaADia", Color.FromRgb(255, 102, 102));

                });
                Banco scotiabank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.scotiabank);


                ConfiguracionAcreditacion config = new ConfiguracionAcreditacion("DiaADia");

                await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(scotiabank, config,_tarea);

            }

            catch (Exception ex)
            {

                e = ex;
                Console.WriteLine($"Error al ejecutar Excel Scotiabank DiaADia " + ex.Message);

                //ACA GUARDAR EN UN LOG
                ServicioLog.instancia.WriteLog(ex, "Scotiabank", "Excel Día a Día");
            }

            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "Scotiabank";

                mensaje.Tipo = "Excel Día a Día ";

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

                        main.MostrarAviso("Error Job Excel Excel Scotiabank DiaADia ", Colors.Red);

                        mensaje.Estado = "Error";

                        //escribir log error

                    }

                    else
                    {

                        main.MostrarAviso("Success Job Excel Scotiabank DiaADia  ", Colors.Green);

                        mensaje.Estado = "Success";

                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();

                    // escribir log success

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
