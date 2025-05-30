using ANS.Model;
using ANS.Model.Services;
using ANS.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;

namespace ANS.ViewModel
{
    public class VMscotiabankOperations : ViewModelBase
    {

        public Banco banco { get; set; }

        private bool _isLoading;

        private ServicioCuentaBuzon _servicioCuentaBuzon;

        public VMscotiabankOperations(Banco bank)
        {
            banco = bank;

            _servicioCuentaBuzon = new ServicioCuentaBuzon();

            EjecutarTanda1HendersonTXTCommand = new RelayCommand(async () => await ejecutarTanda1HendersonTXT());

            EjecutarTanda1HendersonExcelCommand = new RelayCommand(async () => await ejecutarTanda1HendersonExcel());

            EjecutarTanda2HendersonTXTCommand = new RelayCommand(async () => await ejecutarTanda2HendersonTXT());

            EjecutarTanda2HendersonExcelCommand = new RelayCommand(async () => await ejecutarTanda2HendersonExcel());

            EjecutarDiaADiaTXTCommand = new RelayCommand(async () => await ejecutarAcreditacionDiaADia());

            EjecutarDiaADiaExcelCommand = new RelayCommand(async () => await ejecutarExcelDiAADia());

            EjecutarAltaEmailDestinoCommand = new RelayCommand(async () => await ejecutarAltaEmailDestino());
        }
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }


        private async Task ejecutarAltaEmailDestino()
        {

            Banco b = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.scotiabank);

            Cliente c = null;

            ConfiguracionAcreditacion t = new ConfiguracionAcreditacion(VariablesGlobales.diaxdia);

            var alta = new AltaEmailDestino(b, c, t);

            alta.ShowDialog();
        }
        private async Task ejecutarTanda1HendersonTXT()
        {
            IsLoading = true;

            try
            {
                await _servicioCuentaBuzon.acreditarTandaHendersonScotiabank(VariablesGlobales.horaCierreScotiabankHendersonTanda1_TXT, 1);
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

        private async Task ejecutarTanda1HendersonExcel()
        {
            IsLoading = true;

            TimeSpan desde = new TimeSpan(7, 0, 0);

            TimeSpan hasta = new TimeSpan(7, 2, 0);

            Cliente henderson = ServicioCliente.getInstancia().getByNombre("hender");

            int numTanda = 1;

            try
            {

                await Task.Run(async () =>
                {
                    await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, henderson, banco, "MONTEVIDEO", numTanda);

                    // await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, henderson, _banco, "MALDONADO", numTanda);
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
        private async Task ejecutarTanda2HendersonTXT()
        {
            IsLoading = true;

            try
            {

                await Task.Run(async () =>
                {
                    await _servicioCuentaBuzon.acreditarTandaHendersonScotiabank(VariablesGlobales.horaCierreScotiabankHendersonTanda2_TXT, 2);

                    // await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, henderson, _banco, "MALDONADO", numTanda);
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
        private async Task ejecutarTanda2HendersonExcel()
        {
            IsLoading = true;

            TimeSpan desde = new TimeSpan(14,30, 0);

            TimeSpan hasta = new TimeSpan(14, 31, 0);

            Cliente henderson = ServicioCliente.getInstancia().getByNombre("hender");

            int numTanda = 2;

            try
            {

                await Task.Run(async () =>
                {
                    await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, henderson, banco, "MONTEVIDEO", numTanda);

                    // await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, henderson, _banco, "MALDONADO", numTanda);
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


        private async Task ejecutarAcreditacionDiaADia()
        {
            IsLoading = true;

            try
            {

                await Task.Run(async () =>
                {
                    await _servicioCuentaBuzon.acreditarDiaADiaPorBanco(banco);

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

        private async Task ejecutarExcelDiAADia()
        {
            IsLoading = true;

            ConfiguracionAcreditacion configDiaADia = new ConfiguracionAcreditacion(VariablesGlobales.diaxdia);

            try
            {

                await Task.Run(async () =>
                {
                    await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(banco,configDiaADia);

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

        #region COMANDOS
        public ICommand EjecutarTanda1HendersonTXTCommand { get; }
        public ICommand EjecutarTanda1HendersonExcelCommand { get; }
        public ICommand EjecutarTanda2HendersonTXTCommand { get; }
        public ICommand EjecutarTanda2HendersonExcelCommand { get; }
        public ICommand EjecutarDiaADiaTXTCommand { get; set; }
        public ICommand EjecutarDiaADiaExcelCommand { get; set; }
        public ICommand EjecutarAltaEmailDestinoCommand { get; }


        #endregion
    }
}
