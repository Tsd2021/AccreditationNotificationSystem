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
    /// Día a día dedicado de AROCENA SRL (IdCliente 732), buzón EA22L0315N12000051
    /// "ANCAP AROCENA (spectro6)". Dispara 14:21 MON-FRI.
    ///
    /// Acredita a la cuenta 27012913, la misma que el resto de los dedicados de BBVA.
    ///
    /// El cliente estaba en PuntoAPunto y se pasó a DiaADia: antes lo tomaba el job P2P
    /// cada 30 minutos y quedaba repartido en varios REME del día; ahora sale en un solo
    /// archivo diario. Requiere que sus 2 configs (CuentasBuzonesId 557 y 558) estén en
    /// TipoAcreditacion = 'DiaADia' y que 732 esté en
    /// VariablesGlobales.clientesDxDDedicadosBBVA, si no lo re-acredita el genérico.
    /// </summary>
    [DisallowConcurrentExecution]
    public class AcreditarDiaADiaBBVAArocena : IJob
    {
        private IServicioCuentaBuzon _servicioCuentaBuzon { get; set; }
        public AcreditarDiaADiaBBVAArocena(IServicioCuentaBuzon servicioCuentaBuzon)
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
                $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");

            try
            {

                Application.Current?.Dispatcher.Invoke(() =>
                {

                    MainWindow main = (MainWindow)Application.Current.MainWindow;

                    main.MostrarAviso("Ejecutando tarea DXD BBVA AROCENA", Color.FromRgb(0, 68, 129));

                });


                Banco bank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva);

                Cliente cli = ServicioCliente.getInstancia().getById(732); // AROCENA SRL

                // TimeSpan.Zero => acreditarDiaADiaPorCliente usa la hora de cierre de CADA buzón
                // (cu.Cierre) como límite de acreditación. Para AROCENA el cierre es 14:00.
                await _servicioCuentaBuzon.acreditarDiaADiaPorCliente(cli, bank, TimeSpan.Zero);

            }
            catch (Exception ex)
            {

                e = ex;

                Console.WriteLine($"Error al ejecutar DXD BBVA AROCENA: {ex.Message}");

                ServicioLog.instancia.WriteLog(ex, "BBVA", "Acreditar Día a Día Arocena");

            }
            finally
            {

                Mensaje mensaje = new Mensaje();

                mensaje.Color = Color.FromRgb(0, 68, 129);

                mensaje.Banco = "BBVA";

                mensaje.Tipo = "Acreditar día a día (Arocena)";

                mensaje.Icon = PackIconKind.Bank;

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

                        main.MostrarAviso("Error Job DXD BBVA AROCENA", Colors.Red);

                        mensaje.Estado = "Error";

                    }

                    else
                    {

                        main.MostrarAviso("Success Job DXD BBVA AROCENA", Colors.Green);

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
