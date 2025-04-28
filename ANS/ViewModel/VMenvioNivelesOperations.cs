using ANS.Model.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ANS.ViewModel
{
    public class VMenvioNivelesOperations:ViewModelBase
    {

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }
        public ICommand EjecutarEnvioNiveles { get; }
        public ServicioNiveles _servicioNiveles { get; set; }
        public VMenvioNivelesOperations()
        {

            _servicioNiveles = ServicioNiveles.getInstancia();

            EjecutarEnvioNiveles = new RelayCommand(async () => await ejecutarEnvioNiveles());

        }
        private async Task ejecutarEnvioNiveles()
        {

            IsLoading = true;

            try
            {
                await _servicioNiveles.ProcesarNotificacionesPorDesconexion();
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
