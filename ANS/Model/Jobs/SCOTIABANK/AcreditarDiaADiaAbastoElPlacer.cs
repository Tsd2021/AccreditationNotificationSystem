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
    public class AcreditarDiaADiaAbastoElPlacer : IJob
    {
        private IServicioCuentaBuzon _servicioCuentaBuzon { get; set; }
        public AcreditarDiaADiaAbastoElPlacer(IServicioCuentaBuzon servicioCuentaBuzon)
        {
            _servicioCuentaBuzon = servicioCuentaBuzon;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            string jobName = context.JobDetail.Key.Name;
            string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
            DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;

            Exception e = null;

            ServicioLog.instancia.WriteInfo(
                $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC",
                $"Job: {jobName} | Group: {jobGroup} | Banco: Scotiabank | Tipo: Acreditar día a día Abasto El Placer");

            try
            {

                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea DXD ABASTO EL PLACER", Color.FromRgb(255, 102, 102));

                });


                Banco bank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.scotiabank);

                Cliente cli = ServicioCliente.getInstancia().getById(1015); // ABASTO EL PLACER SAS

                if (cli == null)
                {
                    throw new Exception("No se encontró el cliente con ID 1015 (ABASTO EL PLACER SAS) en la lista de clientes precargados.");
                }

                // Excepción de la mañana: acredita 7 AM. De tarde acredita de nuevo con el DXD normal de Scotiabank (16:10).
                await _servicioCuentaBuzon.acreditarDiaADiaPorCliente(cli, bank, VariablesGlobales.horaCierreScotiabankAbastoElPlacer_TXT);

            }
            catch (Exception ex)
            {

                e = ex;

                Console.WriteLine($"Error al ejecutar DXD ABASTO EL PLACER: {ex.Message}");

                ServicioLog.instancia.WriteLog(ex, "Scotiabank", "Acreditar Día a Día Abasto El Placer");

            }
            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "Scotiabank";

                mensaje.Tipo = "Acreditar día a día (Abasto El Placer)";

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

                        main.MostrarAviso("Error Job DXD ABASTO EL PLACER", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job DXD ABASTO EL PLACER", Colors.Green);

                        mensaje.Estado = "Success";

                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();

                });

                if (e == null)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos",
                        $"Job: {jobName} | Group: {jobGroup} | Banco: Scotiabank | Tipo: Acreditar día a día Abasto El Placer");
                }

            }

            await Task.CompletedTask;
        }
    }
}
