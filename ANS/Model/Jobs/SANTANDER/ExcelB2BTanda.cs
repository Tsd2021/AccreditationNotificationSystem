using ANS;
using ANS.Model;
using ANS.Model.Interfaces;
using ANS.Model.Services;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using Quartz;
using System.Drawing;
using System.Windows;
using System.Windows.Media;

[DisallowConcurrentExecution]
public class ExcelB2BTanda : IJob
{

    private readonly IServicioCuentaBuzon _servicioCuentaBuzon;


    public ExcelB2BTanda(IServicioCuentaBuzon servicioCuentaBuzon)
    {
        _servicioCuentaBuzon = servicioCuentaBuzon;
    }


    public async Task Execute(IJobExecutionContext context)
    {
        string jobName = context.JobDetail.Key.Name;
        string jobGroup = context.JobDetail.Key.Group ?? "DEFAULT";
        DateTimeOffset scheduledTime = context.ScheduledFireTimeUtc ?? DateTimeOffset.UtcNow;

        string _city = context.JobDetail.JobDataMap.GetString("city") ?? string.Empty;
        string _tarea = context.JobDetail.JobDataMap.GetString("tarea") ?? string.Empty;
        int _numTanda = context.JobDetail.JobDataMap.GetInt("numTanda");

        Exception e = null;

        ServicioLog.instancia.WriteInfo(
            $"Iniciando ejecución del job | ScheduledTime: {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC | Tarea: {_tarea} | Ciudad: {_city}",
            $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");

        try
        {

            Application.Current.Dispatcher.Invoke(() =>
            {

                MainWindow main = (MainWindow)Application.Current.MainWindow;

                main.MostrarAviso($"Ejecutando Tarea: Envío Excel Tanda {_numTanda} B2B", System.Windows.Media.Color.FromRgb(255, 102, 102));

            });
            Banco santander = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);


            TimeSpan desde = new TimeSpan();

            TimeSpan hasta = new TimeSpan();

            if (_numTanda == 1)
            {
                desde = new TimeSpan(7, 0, 0);

                hasta = new TimeSpan(7, 2, 0);
            }

            if (_numTanda == 2)
            {
                desde = new TimeSpan(14, 30, 0);

                hasta = new TimeSpan(14, 32, 0);
            }

            Cliente henderson = ServicioCliente.getInstancia().getByNombre("hender");

            await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, henderson, santander, _city, _numTanda, _tarea);

        }
        catch (Exception ex)
        {
            e = ex;
            Console.WriteLine($"Error al ejecutar Envío Excel Tanda {_numTanda} B2B: {ex.Message}");
            //ACA GUARDAR EN UN LOG.
            ServicioLog.instancia.WriteLog(ex, "Santander", $"Excel B2B Tanda {_numTanda}");

        }
        finally
        {

            Mensaje mensaje = new Mensaje();

            mensaje.Color = System.Windows.Media.Color.FromRgb(255, 102, 102);

            mensaje.Banco = "Santander";

            mensaje.Tipo = $"Envío Excel Tanda {_numTanda} B2B" + _city;

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

                    main.MostrarAviso($"Error Job Envío Excel Tanda {_numTanda} B2B", Colors.Red);

                    mensaje.Estado = "Error";

                    //escribir log error

                }

                else
                {

                    main.MostrarAviso($"Success Job Excel Envío Excel Tanda {_numTanda} B2B", Colors.Green);

                    mensaje.Estado = "Success";

                }

                ServicioMensajeria.getInstancia().agregar(mensaje);

                vm.CargarMensajes();

                // escribir log success

            });

            if (e == null)
            {
                ServicioLog.instancia.WriteInfo(
                    $"Job completado exitosamente | Duración: {(DateTimeOffset.UtcNow - scheduledTime).TotalSeconds:F2} segundos | Tarea: {_tarea} | Ciudad: {_city} | Tanda: {_numTanda}",
                    $"Job: {jobName} | Group: {jobGroup} | Clase: {GetType().Name}");
            }

        }

        await Task.CompletedTask;
    }
}



