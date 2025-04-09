﻿using ANS.Model;
using ANS.Model.Services;
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
        }
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
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
                    await _servicioCuentaBuzon.enviarExcelHenderson(desde, hasta, henderson, banco, "MONTEVIDEO", numTanda);

                    // await _servicioCuentaBuzon.enviarExcelHenderson(desde, hasta, henderson, _banco, "MALDONADO", numTanda);
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

                    // await _servicioCuentaBuzon.enviarExcelHenderson(desde, hasta, henderson, _banco, "MALDONADO", numTanda);
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
                    await _servicioCuentaBuzon.enviarExcelHenderson(desde, hasta, henderson, banco, "MONTEVIDEO", numTanda);

                    // await _servicioCuentaBuzon.enviarExcelHenderson(desde, hasta, henderson, _banco, "MALDONADO", numTanda);
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
        #endregion
    }
}
