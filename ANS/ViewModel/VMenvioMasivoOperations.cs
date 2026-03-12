using ANS.Model.Services;
using GalaSoft.MvvmLight;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;

namespace ANS.ViewModel
{
    public class VMenvioMasivoOperations : ViewModelBase
    {

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }
        public ICommand EjecutarEnvioMasivo1 { get; }
        public ICommand EjecutarEnvioMasivo2 { get; }
        public ICommand EjecutarEnvioMasivo3 { get; }
        public ICommand EjecutarEnvioMasivo4 { get; }
        public ICommand EjecutarEnvioMasivoFrog { get; }
        public ServicioEnvioMasivo _servicioEnvioMasivo { get; set; }
        private readonly ServicioEnvioSemanalFrog _servicioEnvioSemanalFrog;

        public VMenvioMasivoOperations()
        {
            _servicioEnvioMasivo = ServicioEnvioMasivo.getInstancia();
            _servicioEnvioSemanalFrog = new ServicioEnvioSemanalFrog();

            EjecutarEnvioMasivo1 = new RelayCommand(async () => await ejecutarEnvioMasivo1());
            EjecutarEnvioMasivo2 = new RelayCommand(async () => await ejecutarEnvioMasivo2());
            EjecutarEnvioMasivo3 = new RelayCommand(async () => await ejecutarEnvioMasivo3());
            EjecutarEnvioMasivo4 = new RelayCommand(async () => await ejecutarEnvioMasivo4());
            EjecutarEnvioMasivoFrog = new RelayCommand(async () => await ejecutarEnvioMasivoFrog());
        }
        private async Task ejecutarEnvioMasivo1()
        {

            IsLoading = true;

            try
            {
                await _servicioEnvioMasivo.procesarEnvioMasivo(1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ServicioLog.instancia.WriteLog(e, "Todos", "Envío Masivo 1");
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarEnvioMasivo2()
        {

            IsLoading = true;

            try
            {
                await _servicioEnvioMasivo.procesarEnvioMasivo(2);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ServicioLog.instancia.WriteLog(e, "Todos", "Envío Masivo 2");
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task ejecutarEnvioMasivo3()
        {

            IsLoading = true;

            try
            {
                await _servicioEnvioMasivo.procesarEnvioMasivo(3);
                
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
                ServicioLog.instancia.WriteLog(e, "Todos", "Envío Masivo 3");
            }
            finally
            {
                IsLoading = false;
            }
        }
        private async Task ejecutarEnvioMasivo4()
        {
            IsLoading = true;
            try
            {
                await _servicioEnvioMasivo.procesarEnvioMasivo(4);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ServicioLog.instancia.WriteLog(e, "Todos", "Envío Masivo 4");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ejecutarEnvioMasivoFrog()
        {
            IsLoading = true;
            try
            {
                await _servicioEnvioSemanalFrog.ProcesarSemanaAsync(DateTime.Today);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ServicioLog.instancia.WriteLog(e, "Todos", "Envío Masivo FROG");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
