using ANS.Model;
using ANS.Model.Services;
using ANS.Scheduling;                // JobRun, JobRunStatus, IJobHistoryStore, SchedulerSnapshotProvider
using Quartz;                        // IScheduler
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using static ANS.Scheduling.AgendaQuartz;

namespace ANS.ViewModel
{
    public class VMmainWindow : INotifyPropertyChanged
    {
        // ==== Campos privados ====
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _execTimer;     // chequea si hay jobs corriendo
        private readonly DispatcherTimer _refreshTimer;  // refresco periódico de listas
        private readonly Dispatcher _ui;

        private IScheduler? _scheduler;
        private IJobHistoryStore? _store;
        private int _refreshing; // guard reentradas

        private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        private bool _isAnyRunning;
        private double _progressValue;

        private ObservableCollection<Mensaje> _listaMensajes = new();

        // ==== Colecciones que usa el XAML ====
        public ObservableCollection<ScheduledItem> Programados { get; } = new();
        public ObservableCollection<JobRun> Historial { get; } = new();

        // ==== Propiedades de UI ====
        public string CurrentTime
        {
            get => _currentTime;
            set { _currentTime = value; OnPropertyChanged(); }
        }

      
        public bool IsAnyRunning
        {
            get => _isAnyRunning;
            set { if (_isAnyRunning != value) { _isAnyRunning = value; OnPropertyChanged(); } }
        }

   
        public double ProgressValue
        {
            get => _progressValue;
            set { if (_progressValue != value) { _progressValue = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<Mensaje> TuplaMensajes
        {
            get => _listaMensajes;
            set { _listaMensajes = value; OnPropertyChanged(); }
        }

        // ==== CTORs ====
        public VMmainWindow()
        {
            _ui = Application.Current.Dispatcher;

            // Reloj
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, __) => CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _clockTimer.Start();

            // Timer: ¿hay ejecuciones en curso?
            _execTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            _execTimer.Tick += async (_, __) =>
            {
                if (_scheduler == null) return;
                var running = await _scheduler.GetCurrentlyExecutingJobs();
                IsAnyRunning = running?.Count > 0;
                if (!IsAnyRunning) ProgressValue = 0;
            };
            _execTimer.Start();

            // Timer: refresco “pull” de listas (por si se pierde algún evento)
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
            _refreshTimer.Tick += async (_, __) => await RefreshListsAsync();
            _refreshTimer.Start();

            // Mensajes iniciales
            CargarMensajes();
        }

        public VMmainWindow(IScheduler scheduler, IJobHistoryStore store, JobTrackingListener tracking) : this()
        {
            AttachSchedulerAndStore(scheduler, store, tracking);
        }

        // ==== Wiring desde App ====
        public void AttachSchedulerAndStore(IScheduler scheduler, IJobHistoryStore store, JobTrackingListener tracking)
        {
            _scheduler = scheduler;
            _store = store;

            // refrescar cuando Quartz avisa (push)
            tracking.Changed += async () => await RefreshListsAsync();
        }

        public async Task InitializeAsync() => await RefreshListsAsync();

        // ==== Carga/Refresco de datos para la UI ====
        public async Task RefreshListsAsync()
        {
            if (_scheduler == null || _store == null) return;
            if (Interlocked.Exchange(ref _refreshing, 1) == 1) return;

            try
            {
                var snap = await AgendaQuartz.SchedulerSnapshotProvider.GetAsync(_scheduler, _store);
                var runs = await _store.GetRecentAsync(300);

                await _ui.InvokeAsync(() =>
                {
                    Programados.Clear();
                    foreach (var it in snap) Programados.Add(it);

                    Historial.Clear();
                    foreach (var r in runs.OrderByDescending(r => r.ScheduledFireTimeUtc))
                        Historial.Add(r);
                });
            }
            finally
            {
                Interlocked.Exchange(ref _refreshing, 0);
            }
        }

        // ==== Mensajes existentes ====
        public void CargarMensajes()
        {
            var aux = ServicioMensajeria.getInstancia().getMensajes()
                .OrderByDescending(m => m.Fecha)
                .ToList();

            _listaMensajes.Clear();
            foreach (var mensaje in aux)
                _listaMensajes.Add(mensaje);
        }

        // ==== INotifyPropertyChanged ====
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
