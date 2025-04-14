
using ANS.Model;
using ANS.Model.Services;
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
        #endregion
        public VMbbvaOperations(Banco b)
        {

            banco = b;

            _servicioCuentaBuzon = new ServicioCuentaBuzon();

            EjecutarPuntoAPuntoTXTCommand = new RelayCommand(async () => await ejecutarPuntoAPuntoTXT());

            EjecutarDiaADiaTXTCommand = new RelayCommand(async () => await ejecutarDiaADiaTXT());

            EjecutarExcelTataCommand = new RelayCommand(async () => await ejecutarExcelTata());

            EjecutarExcelDiaADia = new RelayCommand(async () => await ejecutarExcelDiaADia());

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

            try
            {

                await Task.Run(async () =>
                {

                    await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(banco, config);

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
