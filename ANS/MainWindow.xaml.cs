using ANS.Model;
using ANS.ViewModel;
using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ANS
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _currentTime;
        private DispatcherTimer _timer;
        public MainWindow()
        {
            InitializeComponent();

            JobSnackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));

            this.DataContext = this;

            // Arrancamos la hora inicial
            CurrentTime = DateTime.Now.ToString("HH:mm:ss");

            // Configuramos un timer que se dispare cada 1 segundo
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (sender, args) =>
            {
                // Actualiza la propiedad CurrentTime con la hora actual
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            };
            _timer.Start();
        }


        public void MostrarAviso(string msg, System.Windows.Media.Color color)
        {
            var model = new SnackbarMsg

            {
                Texto = msg,

                Fondo = new SolidColorBrush(color)
            };

            Console.WriteLine("aqui se encoló el msg: " + msg);

            JobSnackbar.MessageQueue?.Enqueue(model);
        }
        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged();
            }
        }

        // Implementación de INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
