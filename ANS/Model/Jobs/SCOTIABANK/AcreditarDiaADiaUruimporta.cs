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
    public class AcreditarDiaADiaUruimporta : IJob
    {
        private IServicioCuentaBuzon _servicioCuentaBuzon { get; set; }
        public AcreditarDiaADiaUruimporta(IServicioCuentaBuzon servicioCuentaBuzon)
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
                $"Job: {jobName} | Group: {jobGroup} | Banco: Scotiabank | Tipo: Acreditar día a día Uruimporta");

            try
            {

                Application.Current.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea DXD URUIMPORTA", Color.FromRgb(255, 102, 102));

                });


                Banco bank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.scotiabank);

                Cliente cli = ServicioCliente.getInstancia().getById(1014); // URUIMPORTA

                if (cli == null)
                {
                    throw new Exception("No se encontró el cliente con ID 1014 (URUIMPORTA) en la lista de clientes precargados.");
                }

                // URUIMPORTA acredita SOLO a la mañana (07:03). Se excluye del DXD genérico de la tarde
                // (ServicioCuentaBuzon: NOT IN (..., 1014)) para no duplicar la acreditación.
                // Se pasa TimeSpan.Zero para que el corte sea la hora de cierre de CADA buzón (cc.CIERRE),
                // no una hora fija. La resolución por buzón está en acreditarDiaADiaPorCliente (rama 998/1014).
                await _servicioCuentaBuzon.acreditarDiaADiaPorCliente(cli, bank, TimeSpan.Zero);

            }
            catch (Exception ex)
            {

                e = ex;

                Console.WriteLine($"Error al ejecutar DXD URUIMPORTA: {ex.Message}");

                ServicioLog.instancia.WriteLog(ex, "Scotiabank", "Acreditar Día a Día Uruimporta");

            }
            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(255, 102, 102);

                mensaje.Banco = "Scotiabank";

                mensaje.Tipo = "Acreditar día a día (Uruimporta)";

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

                        main.MostrarAviso("Error Job DXD URUIMPORTA", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job DXD URUIMPORTA", Colors.Green);

                        mensaje.Estado = "Success";

                    }

                    ServicioMensajeria.getInstancia().agregar(mensaje);

                    vm.CargarMensajes();

                });

                if (e == null)
                {
                    ServicioLog.instancia.WriteInfo(
                        $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos",
                        $"Job: {jobName} | Group: {jobGroup} | Banco: Scotiabank | Tipo: Acreditar día a día Uruimporta");
                }

            }

            await Task.CompletedTask;
        }
    }
}
