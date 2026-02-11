using ANS.Model.DTOs;
using ANS.Model.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
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
                    LimpiarTextoBusquedaCommand.RaiseCanExecuteChanged();
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
                        _todasLasEmpresas.Clear();
                        EmpresaSeleccionada = null;
                        Depositos.Clear();
                        ResultadosPorBanco.Clear();
                        NotifyResultadosPorBancoChanged();
                    }
                    else
                    {
                        // Cargar empresas del buzón (sin filtro por banco)
                        _ = CargarEmpresas();
                    }
                }
            }
        }

        public bool HasBuzonSeleccionado => BuzonSeleccionado != null;

        // Empresas del buzón (opcional: si no se elige, se cargan depósitos de todas las cuentas)
        public ObservableCollection<EmpresaDto> Empresas { get; } = new ObservableCollection<EmpresaDto>();
        
        private List<EmpresaDto> _todasLasEmpresas = new List<EmpresaDto>(); // Cache de todas las empresas

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

        // Flag de excepciones (solo para Scotiabank)
        private bool _esExcepcionScotiabank;
        public bool EsExcepcionScotiabank
        {
            get => _esExcepcionScotiabank;
            set => Set(ref _esExcepcionScotiabank, value);
        }

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
        public ICollectionView DepositosView { get; }

        // Filtro rápido sobre depósitos: Todos | Pendientes | Acreditados | Sin cuenta
        private string _depositosQuickFilter = "Todos";
        public string DepositosQuickFilter
        {
            get => _depositosQuickFilter;
            set
            {
                if (Set(ref _depositosQuickFilter, value))
                {
                    DepositosView?.Refresh();
                    RaisePropertyChanged(nameof(TotalDepositosVisible));
                }
            }
        }

        public System.Collections.Generic.List<string> DepositosQuickFilterOptions { get; } =
            new System.Collections.Generic.List<string> { "Todos", "Pendientes", "Acreditados" };

        public int TotalDepositosVisible => DepositosView != null
            ? DepositosView.Cast<object>().Count()
            : 0;

        public int TotalSeleccionados => Depositos.Count(d => d.IsSelected);

        // Checkbox: al marcar, selecciona todos los pendientes visibles; al "Seleccionar todos visibles" solo marca pendientes si está activo
        private bool _seleccionarSoloPendientes;
        public bool SeleccionarSoloPendientes
        {
            get => _seleccionarSoloPendientes;
            set
            {
                if (Set(ref _seleccionarSoloPendientes, value) && value)
                {
                    // Al activar: marcar todos los pendientes visibles que tengan cuenta
                    foreach (var item in DepositosView.Cast<DepositoAcreditacionDto>())
                    {
                        if (!item.HasCuentaAsignada || item.IsAcreditado) continue;
                        item.IsSelected = true;
                    }
                    RaisePropertyChanged(nameof(TotalSeleccionados));
                    AcreditarSeleccionadosCommand.RaiseCanExecuteChanged();
                    LimpiarSeleccionDepositosCommand.RaiseCanExecuteChanged();
                }
            }
        }

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
        public RelayCommand<string> SetQuickFilterCommand { get; }
        public RelayCommand SeleccionarTodosVisiblesCommand { get; }
        public RelayCommand LimpiarSeleccionDepositosCommand { get; }
        public RelayCommand LimpiarTextoBusquedaCommand { get; }

        public VMacreditacionManualOperations()
        {
            _servicio = ServicioAcreditacionManual.getInstancia();

            DepositosView = CollectionViewSource.GetDefaultView(Depositos);
            DepositosView.Filter = DepositosFilter;

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
                () => HasBuzonSeleccionado && !IsLoading);

            AcreditarSeleccionadosCommand = new RelayCommand(
                async () => await AcreditarSeleccionados(),
                () => HasBuzonSeleccionado && Depositos.Any(d => d.IsSelected) && !IsLoading);

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

            SetQuickFilterCommand = new RelayCommand<string>(f =>
            {
                if (!string.IsNullOrEmpty(f) && DepositosQuickFilterOptions.Contains(f))
                    DepositosQuickFilter = f;
            });

            SeleccionarTodosVisiblesCommand = new RelayCommand(() =>
            {
                foreach (var item in DepositosView.Cast<DepositoAcreditacionDto>())
                {
                    if (!item.HasCuentaAsignada) continue;
                    if (SeleccionarSoloPendientes && item.IsAcreditado) continue;
                    item.IsSelected = true;
                }
                RaisePropertyChanged(nameof(TotalSeleccionados));
                AcreditarSeleccionadosCommand.RaiseCanExecuteChanged();
            }, () => HasBuzonSeleccionado && Depositos.Any());

            LimpiarSeleccionDepositosCommand = new RelayCommand(() =>
            {
                foreach (var d in Depositos)
                    d.IsSelected = false;
                RaisePropertyChanged(nameof(TotalSeleccionados));
                AcreditarSeleccionadosCommand.RaiseCanExecuteChanged();
            }, () => HasBuzonSeleccionado && Depositos.Any());

            LimpiarTextoBusquedaCommand = new RelayCommand(() =>
            {
                TextoBusquedaNN = string.Empty;
            }, () => !string.IsNullOrWhiteSpace(TextoBusquedaNN));
        }

        private bool DepositosFilter(object item)
        {
            if (!(item is DepositoAcreditacionDto d)) return false;
            switch (_depositosQuickFilter)
            {
                case "Pendientes": return !d.IsAcreditado;
                case "Acreditados": return d.IsAcreditado;
                default: return true; // Todos
            }
        }

        /// <summary>
        /// Llamar desde la vista cuando el usuario marca/desmarca un depósito para refrescar el estado del botón Acreditar.
        /// </summary>
        public void NotifyDepositoSelectionChanged()
        {
            RaisePropertyChanged(nameof(TotalSeleccionados));
            AcreditarSeleccionadosCommand.RaiseCanExecuteChanged();
            LimpiarSeleccionDepositosCommand.RaiseCanExecuteChanged();
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
                var todasLasEmpresas = await _servicio.ObtenerEmpresasPorBuzon(BuzonSeleccionado.NC);
                _todasLasEmpresas = todasLasEmpresas ?? new List<EmpresaDto>();

                Empresas.Clear();
                // Opción "TODAS" para quitar el filtro de empresa (IdCuenta = 0 se trata como null al cargar depósitos)
                Empresas.Add(new EmpresaDto { Empresa = "TODAS", IdCuenta = 0, Cuenta = "", Moneda = "" });
                foreach (var e in _todasLasEmpresas)
                {
                    Empresas.Add(e);
                }

                if (Empresas.Count > 0)
                    EmpresaSeleccionada = Empresas[0]; // TODAS por defecto

                StatusMessage = _todasLasEmpresas.Count > 0 
                    ? $"Se encontraron {_todasLasEmpresas.Count} empresa(s) / cuenta(s)" 
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
            if (BuzonSeleccionado == null)
                return;

            IsLoading = true;
            StatusMessage = "Cargando depósitos...";
            try
            {
                Depositos.Clear();
                // IdCuenta == 0 es la opción "TODAS" => sin filtro
                int? idCuentaFiltro = (EmpresaSeleccionada == null || EmpresaSeleccionada.IdCuenta == 0)
                    ? null
                    : (int?)EmpresaSeleccionada.IdCuenta;
                var depositos = await _servicio.ObtenerDepositosPorBuzonEnRango(
                    BuzonSeleccionado.NC,
                    Desde,
                    Hasta,
                    idCuentaFiltro);

                // Mapear con estado de acreditación (batch)
                var depositosConEstado = await _servicio.MapearDepositosConEstadoAcreditado(depositos);

                foreach (var deposito in depositosConEstado)
                {
                    Depositos.Add(deposito);
                }

                DepositosView?.Refresh();
                RaisePropertyChanged(nameof(TotalDepositosVisible));
                RaisePropertyChanged(nameof(TotalSeleccionados));
                SeleccionarTodosVisiblesCommand.RaiseCanExecuteChanged();
                LimpiarSeleccionDepositosCommand.RaiseCanExecuteChanged();

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
                    Environment.UserName,
                    EsExcepcionScotiabank);

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
