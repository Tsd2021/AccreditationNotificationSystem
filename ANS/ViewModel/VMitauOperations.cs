﻿
using ANS.Model;
using ANS.Model.Services;
using ANS.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Diagnostics;
using System.Windows.Input;

namespace ANS.ViewModel
{
    public class VMitauOperations : ViewModelBase
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
        public ICommand EjecutarEnvioExcelCommand { get; }
        public ICommand EjecutarDiaADiaTXTCommand { get; }
        public ICommand EjecutarAltaEmailDestinoCommand { get; }

        #endregion
        public VMitauOperations(Banco b)
        {

            banco = b;

            _servicioCuentaBuzon = new ServicioCuentaBuzon();

            EjecutarEnvioExcelCommand = new RelayCommand(async () => await ejecutarEnvioExcel());

            EjecutarDiaADiaTXTCommand = new RelayCommand(async () => await ejecutarDiaADiaTXT());

            EjecutarAltaEmailDestinoCommand = new RelayCommand(async () => await ejecutarAltaEmailDestino());
        }

        private async Task ejecutarAltaEmailDestino()
        {

            Banco b = ServicioBanco.getInstancia().getByNombre(VariablesGlobales.itau);

            Cliente c = null;

            ConfiguracionAcreditacion t = new ConfiguracionAcreditacion(VariablesGlobales.diaxdia);

            var alta = new AltaEmailDestino(b, c, t);

            alta.ShowDialog();
        }


        private async Task ejecutarEnvioExcel()
        {

            IsLoading = true;
            string tarea = "DiaADia";
            try
            {
                await Task.Run(async () =>
                {

                    await _servicioCuentaBuzon.enviarExcelDiaADiaPorBanco(banco, new ConfiguracionAcreditacion { TipoAcreditacion = VariablesGlobales.diaxdia },tarea);

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

    }
}