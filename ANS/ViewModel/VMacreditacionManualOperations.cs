using ANS.Model.DTOs;
using ANS.Model.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ANS.ViewModel
{
    public class VMacreditacionManualOperations : ViewModelBase
    {
        private readonly ServicioAcreditacionManual _servicio;
        private readonly DispatcherTimer _searchDebounceTimer;
        private const int SearchDebounceMs = 400;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (Set(ref _isLoading, value))
                {
                    BuscarBuzonCommand.RaiseCanExecuteChanged();
                    CargarEmpresasCommand.RaiseCanExecuteChanged();
                    CargarDepositosCommand.RaiseCanExecuteChanged();
                    AcreditarSeleccionadosCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        /// <summary>Mensaje visible en la card "1. Buscar Buzón" (siempre visible ahí).</summary>
        private string _searchStatusMessage;
        public string SearchStatusMessage
        {
            get => _searchStatusMessage;
            set => Set(ref _searchStatusMessage, value);
        }

        // Búsqueda de buzones
        private string _textoBusquedaNN;
        public string TextoBusquedaNN
        {
            get => _textoBusquedaNN;
            set
            {
                if (Set(ref _textoBusquedaNN, value))
                {
#if DEBUG
                    Debug.WriteLine($"[AcreditacionManual] TextoBusquedaNN setter: '{value ?? "(null)"}' (len={value?.Length ?? 0})");
#endif
                    _searchDebounceTimer.Stop();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        Buzones.Clear();
                        StatusMessage = string.Empty;
                        SearchStatusMessage = string.Empty;
                    }
                    else
                    {
                        _searchDebounceTimer.Start();
                    }
                    BuscarBuzonCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<BuzonBusquedaDto> Buzones { get; } = new ObservableCollection<BuzonBusquedaDto>();

        private BuzonBusquedaDto _buzonSeleccionado;
        public BuzonBusquedaDto BuzonSeleccionado
        {
            get => _buzonSeleccionado;
            set
            {
                if (Set(ref _buzonSeleccionado, value))
                {
                    RaisePropertyChanged(nameof(HasBuzonSeleccionado));
                    CargarEmpresasCommand.RaiseCanExecuteChanged();
                    CargarDepositosCommand.RaiseCanExecuteChanged();
                    AcreditarSeleccionadosCommand.RaiseCanExecuteChanged();
                    
                    // Limpiar empresas y depósitos al cambiar buzón
                    if (value == null)
                    {
                        Empresas.Clear();
                        EmpresaSeleccionada = null;
                        Depositos.Clear();
                        ResultadosPorBanco.Clear();
                        NotifyResultadosPorBancoChanged();
                    }
                    else
                    {
                        // Cargar empresas automáticamente al seleccionar buzón
                        _ = CargarEmpresas();
                    }
                }
            }
        }

        public bool HasBuzonSeleccionado => BuzonSeleccionado != null;

        // Empresas
        public ObservableCollection<EmpresaDto> Empresas { get; } = new ObservableCollection<EmpresaDto>();

        private EmpresaDto _empresaSeleccionada;
        public EmpresaDto EmpresaSeleccionada
        {
            get => _empresaSeleccionada;
            set
            {
                if (Set(ref _empresaSeleccionada, value))
                {
                    RaisePropertyChanged(nameof(HasEmpresaSeleccionada));
                    CargarDepositosCommand.RaiseCanExecuteChanged();
                    AcreditarSeleccionadosCommand.RaiseCanExecuteChanged();
                    
                    // Limpiar depósitos al cambiar empresa
                    if (value == null)
                    {
                        Depositos.Clear();
                        ResultadosPorBanco.Clear();
                        NotifyResultadosPorBancoChanged();
                    }
                }
            }
        }

        public bool HasEmpresaSeleccionada => EmpresaSeleccionada != null;

        // Fechas
        private DateTime _desde = DateTime.Today.AddDays(-7);
        public DateTime Desde
        {
            get => _desde;
            set
            {
                if (Set(ref _desde, value))
                {
                    CargarDepositosCommand.RaiseCanExecuteChanged();
                    AcreditarSeleccionadosCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private DateTime _hasta = DateTime.Today;
        public DateTime Hasta
        {
            get => _hasta;
            set
            {
                if (Set(ref _hasta, value))
                {
                    CargarDepositosCommand.RaiseCanExecuteChanged();
                    AcreditarSeleccionadosCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // Depósitos
        public ObservableCollection<DepositoAcreditacionDto> Depositos { get; } = new ObservableCollection<DepositoAcreditacionDto>();

        // Resultados por banco
        public ObservableCollection<ResultadoBatchDto> ResultadosPorBanco { get; } = new ObservableCollection<ResultadoBatchDto>();

        public bool HasResultadosPorBanco => ResultadosPorBanco.Count > 0;

        private void NotifyResultadosPorBancoChanged()
        {
            RaisePropertyChanged(nameof(HasResultadosPorBanco));
        }

        // Comandos
        public RelayCommand BuscarBuzonCommand { get; }
        public RelayCommand CargarEmpresasCommand { get; }
        public RelayCommand CargarDepositosCommand { get; }
        public RelayCommand AcreditarSeleccionadosCommand { get; }
        public RelayCommand LimpiarSeleccionCommand { get; }

        public VMacreditacionManualOperations()
        {
            _servicio = ServicioAcreditacionManual.getInstancia();

            _searchDebounceTimer = new DispatcherTimer(DispatcherPriority.Background, System.Windows.Application.Current.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(SearchDebounceMs)
            };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                if (!string.IsNullOrWhiteSpace(TextoBusquedaNN) && !IsLoading)
                    _ = BuscarBuzon();
            };

            BuscarBuzonCommand = new RelayCommand(
                async () => await BuscarBuzon(),
                () => !string.IsNullOrWhiteSpace(TextoBusquedaNN) && !IsLoading);

            CargarEmpresasCommand = new RelayCommand(
                async () => await CargarEmpresas(),
                () => HasBuzonSeleccionado && !IsLoading);

            CargarDepositosCommand = new RelayCommand(
                async () => await CargarDepositos(),
                () => HasBuzonSeleccionado && HasEmpresaSeleccionada && !IsLoading);

            AcreditarSeleccionadosCommand = new RelayCommand(
                async () => await AcreditarSeleccionados(),
                () => HasBuzonSeleccionado && HasEmpresaSeleccionada && Depositos.Any(d => d.IsSelected) && !IsLoading);

            LimpiarSeleccionCommand = new RelayCommand(() =>
            {
                BuzonSeleccionado = null;
                EmpresaSeleccionada = null;
                TextoBusquedaNN = string.Empty;
                Depositos.Clear();
                ResultadosPorBanco.Clear();
                NotifyResultadosPorBancoChanged();
                StatusMessage = string.Empty;
                SearchStatusMessage = string.Empty;
            });
        }

        private async Task BuscarBuzon()
        {
            _searchDebounceTimer.Stop();
            if (string.IsNullOrWhiteSpace(TextoBusquedaNN))
                return;

#if DEBUG
            Debug.WriteLine($"[AcreditacionManual] BuscarBuzon ENTRANDO: TextoBusquedaNN='{TextoBusquedaNN}'");
#endif

            IsLoading = true;
            SearchStatusMessage = "Buscando buzones...";
            StatusMessage = "Buscando buzones...";
            try
            {
                Buzones.Clear();
                var resultados = await _servicio.BuscarBuzonesPorNN(TextoBusquedaNN);
                foreach (var buzon in resultados)
                {
                    Buzones.Add(buzon);
                }
                var msg = resultados.Count > 0
                    ? $"Se encontraron {resultados.Count} buzón(es)"
                    : "No se encontraron buzones";
                SearchStatusMessage = msg;
                StatusMessage = msg;
#if DEBUG
                Debug.WriteLine($"[AcreditacionManual] BuscarBuzon OK: {resultados.Count} resultados. Buzones.Count={Buzones.Count}");
#endif
            }
            catch (Exception ex)
            {
                var err = $"Error al buscar: {ex.Message}";
                SearchStatusMessage = err;
                StatusMessage = err;
                ServicioLog.instancia.WriteLog(ex, "Todos", "Acreditación Manual - Buscar Buzón");
#if DEBUG
                Debug.WriteLine($"[AcreditacionManual] BuscarBuzon ERROR: {ex.Message}");
#endif
            }
            finally
            {
                IsLoading = false;
                BuscarBuzonCommand.RaiseCanExecuteChanged();
            }
        }

        private async Task CargarEmpresas()
        {
            if (BuzonSeleccionado == null)
                return;

            IsLoading = true;
            StatusMessage = "Cargando empresas...";
            try
            {
                Empresas.Clear();
                var empresas = await _servicio.ObtenerEmpresasPorBuzon(BuzonSeleccionado.NC);
                foreach (var empresa in empresas)
                {
                    Empresas.Add(empresa);
                }
                StatusMessage = empresas.Count > 0 
                    ? $"Se encontraron {empresas.Count} empresa(s)" 
                    : "No se encontraron empresas para este buzón";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar empresas: {ex.Message}";
                ServicioLog.instancia.WriteLog(ex, "Todos", "Acreditación Manual - Cargar Empresas");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CargarDepositos()
        {
            if (BuzonSeleccionado == null || EmpresaSeleccionada == null)
                return;

            IsLoading = true;
            StatusMessage = "Cargando depósitos...";
            try
            {
                Depositos.Clear();
                var depositos = await _servicio.ObtenerDepositosUltimos7Dias(
                    BuzonSeleccionado.NC,
                    EmpresaSeleccionada.Empresa,
                    Desde,
                    Hasta,
                    EmpresaSeleccionada.Moneda,
                    EmpresaSeleccionada.IdCuenta);

                // Mapear con estado de acreditación (batch)
                var depositosConEstado = await _servicio.MapearDepositosConEstadoAcreditado(depositos);

                foreach (var deposito in depositosConEstado)
                {
                    Depositos.Add(deposito);
                }

                StatusMessage = depositosConEstado.Count > 0 
                    ? $"Se encontraron {depositosConEstado.Count} depósito(s)" 
                    : "No se encontraron depósitos para los criterios seleccionados";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar depósitos: {ex.Message}";
                ServicioLog.instancia.WriteLog(ex, "Todos", "Acreditación Manual - Cargar Depósitos");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AcreditarSeleccionados()
        {
            var depositosSeleccionados = Depositos.Where(d => d.IsSelected).ToList();
            if (!depositosSeleccionados.Any())
            {
                StatusMessage = "No hay depósitos seleccionados";
                return;
            }

            IsLoading = true;
            StatusMessage = "Acreditando depósitos...";
            ResultadosPorBanco.Clear();

            try
            {
                var resultados = await _servicio.AcreditarDepositos(
                    depositosSeleccionados,
                    Environment.UserName);

                foreach (var resultado in resultados)
                {
                    ResultadosPorBanco.Add(resultado);
                }
                NotifyResultadosPorBancoChanged();

                // Refrescar lista de depósitos para actualizar estado
                await CargarDepositos();

                var exitosos = resultados.Count(r => r.Exitoso);
                var totales = resultados.Count;
                var totalInsertados = resultados.Sum(r => r.TotalInsertados);
                var totalOmitidos = resultados.Sum(r => r.TotalOmitidos);

                StatusMessage = exitosos == totales
                    ? $"Acreditación completada: {totalInsertados} insertados, {totalOmitidos} omitidos"
                    : $"Acreditación parcial: {exitosos}/{totales} bancos exitosos. {totalInsertados} insertados, {totalOmitidos} omitidos";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al acreditar: {ex.Message}";
                ServicioLog.instancia.WriteLog(ex, "Todos", "Acreditación Manual - Acreditar Depósitos");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
