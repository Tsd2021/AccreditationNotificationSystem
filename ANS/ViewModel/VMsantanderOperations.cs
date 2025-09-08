using ANS.Model;
using ANS.Model.Services;
using ANS.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;
using TensStdr;

namespace ANS.ViewModel
{
    public class VMsantanderOperations : ViewModelBase
    {
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }
        private DateTime? _fechaSeleccionada = DateTime.Today;
        public DateTime? FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set => Set(ref _fechaSeleccionada, value);
        }
        public ICommand EjecutarPuntoAPuntoTXTCommand { get; }
        public ICommand EjecutarDiaADiaTXTCommand { get; }
        public ICommand EjecutarTanda1HendersonTXTCommand { get; }
        public ICommand EjecutarTanda2HendersonTXTCommand { get; }
        public ICommand EjecutarTanda1HendersonExcelCommand { get; }
        public ICommand EjecutarTanda2HendersonExcelCommand { get; }
        public ICommand EjecutarTanda1ExcelTesoreriaCommand { get; }
        public ICommand EjecutarTanda2ExcelTesoreriaCommand { get; }
        public ICommand EjecutarDeLasSierrasTXTCommand { get; }
        public ICommand EjecutarDiaADiaExcelCommand { get; }
        public ICommand EjecutarReporteDiarioCommand { get; }
        public ICommand EjecutarAltaEmailDestinoCommand { get; }
        public ServicioCuentaBuzon _servicioCuentaBuzon { get; set; }
        public Banco _banco { get; set; }
        public VMsantanderOperations(Banco b)
        {

            _banco = b;

            _servicioCuentaBuzon = new ServicioCuentaBuzon();

            EjecutarDeLasSierrasTXTCommand = new RelayCommand(async () => await ejecutarDeLasSierrasTXT());

            EjecutarPuntoAPuntoTXTCommand = new RelayCommand(async () => await ejecutarPuntoAPuntoTXT());

            EjecutarTanda1HendersonExcelCommand = new RelayCommand(async () => await ejecutarTanda1HendersonExcel());

            EjecutarTanda2HendersonExcelCommand = new RelayCommand(async () => await ejecutarTanda2HendersonExcel());

            EjecutarDiaADiaTXTCommand = new RelayCommand(async () => await ejecutarDiaADiaTXT());

            EjecutarTanda1HendersonTXTCommand = new RelayCommand(async () => await ejecutarTanda1HendersonTXT());

            EjecutarTanda2HendersonTXTCommand = new RelayCommand(async () => await ejecutarTanda2HendersonTXT());

            EjecutarTanda1ExcelTesoreriaCommand = new RelayCommand(async () => await ejecutarTanda1ExcelTesoreria());

            EjecutarTanda2ExcelTesoreriaCommand = new RelayCommand(async () => await ejecutarTanda2ExcelTesoreria());

            EjecutarDiaADiaExcelCommand = new RelayCommand(async () => await ejecutarDiaADiaExcel());

            EjecutarReporteDiarioCommand = new RelayCommand(async () => await ejecutarReporteDiarioExcel());

            EjecutarAltaEmailDestinoCommand = new RelayCommand(async () => await ejecutarAltaEmailDestino());

        }
        private async Task ejecutarAltaEmailDestino()
        {

            Banco b = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

            Cliente c = null;

            ConfiguracionAcreditacion t = new ConfiguracionAcreditacion(VariablesGlobales.diaxdia);

            var alta = new AltaEmailDestino(b, c, t);

            alta.ShowDialog();
        }
        private async Task ejecutarDiaADiaTXT()
        {

            IsLoading = true;

            try
            {
                await _servicioCuentaBuzon.acreditarDiaADiaPorBanco(_banco);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarPuntoAPuntoTXT()
        {

            IsLoading = true;

            try
            {
                await _servicioCuentaBuzon.acreditarPuntoAPuntoPorBanco(_banco);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ServicioLog.instancia.WriteLog(e, "Santander", "[MANUAL] Acreditar Punto a Punto");
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarTanda1HendersonTXT()
        {

            IsLoading = true;

            try
            {
                await _servicioCuentaBuzon.acreditarTandaHendersonSantander(VariablesGlobales.horaCierreSantanderHENDERSON_TANDA1_TXT,1);
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
                ServicioLog.instancia.WriteLog(e, "Santander", "[MANUAL] Acreditar Tanda 1 Henderson");
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarTanda2HendersonTXT()
        {
            IsLoading = true;
            try
            {
                await _servicioCuentaBuzon.acreditarTandaHendersonSantander(VariablesGlobales.horaCierreSantanderHENDERSON_TANDA2_TXT,2);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ServicioLog.instancia.WriteLog(e, "Santander", "[MANUAL] Acreditar Tanda 2 Henderson");
            }
            finally
            {
                IsLoading = false;
            }
        }
        private async Task ejecutarTanda1HendersonExcel()
        {

            IsLoading = true;

            TimeSpan desde = new TimeSpan(7, 0, 0);

            TimeSpan hasta = new TimeSpan(7, 2, 0);

            Cliente henderson = ServicioCliente.getInstancia().getByNombre("hender");

            int numTanda = 1;

            string tarea = "Tanda1";

            try
            {

                await Task.Run(async () =>
                {

                    await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, henderson, _banco, "MONTEVIDEO", numTanda,tarea);

                });

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ServicioLog.instancia.WriteLog(e, "Santander", "[MANUAL] Enviar Excel Tanda 1 Henderson");
            }
            finally
            {
                IsLoading = false;
            }
        }
        private async Task ejecutarTanda2HendersonExcel()
        {

            IsLoading = true;

            TimeSpan desde = new TimeSpan(14, 30, 0);

            TimeSpan hasta = new TimeSpan(14, 31, 0);

            Cliente henderson = ServicioCliente.getInstancia().getByNombre("hender");

            int numTanda = 2;

            string tarea = "Tanda2";
            try
            {
                await Task.Run(async () =>
                {
                    await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, henderson, _banco, "MONTEVIDEO", numTanda,tarea);

                   // await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, henderson, _banco, "MALDONADO", numTanda);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ServicioLog.instancia.WriteLog(e, "Santander", "[MANUAL] Enviar Excel Tanda 2 Henderson");
            }
            finally
            {
                IsLoading = false;
            }
        }
        private async Task ejecutarDeLasSierrasTXT()
        {
            IsLoading = true;

            Cliente cli = ServicioCliente.getInstancia().getByNombre("DE LAS SIERRAS");

            try
            {
                await _servicioCuentaBuzon.acreditarDiaADiaPorCliente(cli, _banco, VariablesGlobales.horaCierreSantanderDeLaSierras_TXT);
            }

            catch (Exception e)
            {

                Console.WriteLine(e);

                ServicioLog.instancia.WriteLog(e, "Santander", "[MANUAL] Acreditar De las Sierras");

            }

            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarTanda1ExcelTesoreria()
        {

            IsLoading = true;

            TimeSpan desde = new TimeSpan(6, 59, 0);

            TimeSpan hasta = new TimeSpan(7, 1, 0);

            Banco bank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

            string tarea = "Tesoreria1";

            try
            {
                await Task.Run(() => { _servicioCuentaBuzon.enviarExcelTesoreria(bank, "MONTEVIDEO", 1, desde, hasta, tarea, FechaSeleccionada?.Date).Wait(); });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                ServicioLog.instancia.WriteLog(e, "Santander", "[MANUAL] Enviar Tanda 1 Excel TESORERÍA");
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarTanda2ExcelTesoreria()
        {

            IsLoading = true;

            await Task.Yield(); // Permite que el UI actualice el progress bar

            TimeSpan desde = new TimeSpan(14, 30, 0);

            TimeSpan hasta = new TimeSpan(14, 32, 0);

            Banco bank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

            string tarea = "Tesoreria2";

            try
            {

                await Task.Run(() => {  _servicioCuentaBuzon.enviarExcelTesoreria(bank, "MONTEVIDEO", 2, desde, hasta,tarea, FechaSeleccionada?.Date).Wait(); });

            }

            catch (Exception e)
            {
                Console.WriteLine(e);

                ServicioLog.instancia.WriteLog(e, "Santander", "[MANUAL] Enviar Tanda 2 Excel TESORERÍA");
            }

            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarDiaADiaExcel()
        {
            Banco bank = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander);

            ConfiguracionAcreditacion config = new ConfiguracionAcreditacion(VariablesGlobales.diaxdia);

            IsLoading = true; await Task.Yield();

            string tarea = "DiaADia";


            try
            {
                await Task.Run(() =>
                {
                    _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(bank, config,tarea).Wait();
                });

            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                ServicioLog.instancia.WriteLog(e, "Santander", "[MANUAL] Enviar Excel Día a Día");
            }
            finally
            {
                IsLoading = false;

            }


        }
        private async Task ejecutarReporteDiarioExcel()
        {
            IsLoading = true; await Task.Yield();

            string tarea = "ReporteDiario";

            try
            {
                await Task.Run(() =>
                {
                     _servicioCuentaBuzon.generarExcelDelResumenDelDiaSantander(tarea).Wait();
                });
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                IsLoading = false;

            }

        }
    }
}
