using ANS.Model;
using ANS.Model.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace ANS.ViewModel
{
    public class VMmainWindow : INotifyPropertyChanged
    {
        private string _currentTime;

        private DispatcherTimer _timer;

        private ObservableCollection<Mensaje> _listaMensajes;

        public VMmainWindow()
        {

            _currentTime = DateTime.Now.ToString("HH:mm:ss");

            _timer = new DispatcherTimer();

            _timer.Interval = TimeSpan.FromSeconds(1);

            _timer.Tick += (s, e) =>
            {
                CurrentTime = DateTime.Now.ToString("HH:mm:ss"); 
            };

            _timer.Start();

            _listaMensajes = new ObservableCollection<Mensaje>();

            CargarMensajes();

        }

        public string CurrentTime
        {
            get => _currentTime;

            set
            {

                _currentTime = DateTime.Now.ToString("HH:mm:ss");

                OnPropertyChanged();

            }
        }


        public void CargarMensajes()
        {

            List<Mensaje> aux = ServicioMensajeria.getInstancia().getMensajes()
                .OrderByDescending(m => m.Fecha) 
                .ToList();

            _listaMensajes.Clear();

            foreach (var mensaje in aux)
            {
                _listaMensajes.Add(mensaje);
            }

        }

        public ObservableCollection<Mensaje> TuplaMensajes
        {
            get => _listaMensajes;
            set
            {
                _listaMensajes = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
