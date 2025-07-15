
using ANS.Model;
using ANS.Model.Services;
using ANS.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Diagnostics;
using System.Windows.Input;

namespace ANS.ViewModel
{
    public class VMbbvaOperations : ViewModelBase
    {
        private Banco banco { get; set; }
        private bool _isLoading;
        private ServicioCuentaBuzon _servicioCuentaBuzon;
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }

        #region Commands
        public ICommand EjecutarPuntoAPuntoTXTCommand { get; }
        public ICommand EjecutarDiaADiaTXTCommand { get; }
        public ICommand EjecutarExcelTataCommand { get; }
        public ICommand EjecutarExcelDiaADia { get; }
        public ICommand EjecutarAltaEmailDestinoCommand { get; }


        #endregion
        public VMbbvaOperations(Banco b)
        {

            banco = b;

            _servicioCuentaBuzon = new ServicioCuentaBuzon();

            EjecutarPuntoAPuntoTXTCommand = new RelayCommand(async () => await ejecutarPuntoAPuntoTXT());

            EjecutarDiaADiaTXTCommand = new RelayCommand(async () => await ejecutarDiaADiaTXT());

            EjecutarExcelTataCommand = new RelayCommand(async () => await ejecutarExcelTata());

            EjecutarExcelDiaADia = new RelayCommand(async () => await ejecutarExcelDiaADia());

            EjecutarAltaEmailDestinoCommand = new RelayCommand(async () => await ejecutarAltaEmailDestino());

        }


        private async Task ejecutarAltaEmailDestino()
        {

            Banco b = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva);

            Cliente c = null;

            ConfiguracionAcreditacion t = new ConfiguracionAcreditacion(VariablesGlobales.diaxdia);

            var alta = new AltaEmailDestino(b, c, t);

            alta.ShowDialog();

        }
        private async Task ejecutarPuntoAPuntoTXT()
        {

            IsLoading = true;

            try
            {
                await Task.Run(async () =>
                {

                    await _servicioCuentaBuzon.acreditarPuntoAPuntoPorBanco(banco);

                });


            }
            catch (Exception e)
            {
                Debug.WriteLine("Hubo un error: " + e.Message);

                throw;
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarDiaADiaTXT()
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
                Debug.WriteLine("Hubo un error: " + e.Message);

                throw;
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarExcelTata()
        {
            IsLoading = true;

            try
            {
                Banco bbva = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva);

                TimeSpan desde = new TimeSpan(6, 30, 0);

                TimeSpan hasta = new TimeSpan(20, 30, 0);

                string tarea = "ExcelTata";

                // ID TATA : 242

                Cliente tata = ServicioCliente.getInstancia().getById(242);

                int numTanda = 1;

                await _servicioCuentaBuzon.enviarExcelFormatoTanda(desde, hasta, tata, bbva, "MONTEVIDEO", numTanda,tarea);


            }
            catch (Exception e)
            {
                Debug.WriteLine("Hubo un error: " + e.Message);

                throw;
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarExcelDiaADia()
        {
            IsLoading = true;

            ConfiguracionAcreditacion config = new ConfiguracionAcreditacion(VariablesGlobales.diaxdia);

            string tarea = "DiaADia";

            try
            {

                await Task.Run(async () =>
                {

                    await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(banco, config, tarea);

                });
            }

            catch (Exception e)
            {
                Debug.WriteLine("Hubo un error: " + e.Message);

                throw;
            }

            finally
            {
                IsLoading = false;
            }

        }
    }
}
