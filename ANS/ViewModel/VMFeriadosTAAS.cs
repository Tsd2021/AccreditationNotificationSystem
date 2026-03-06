using ANS.Model;
using ANS.Model.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ANS.ViewModel
{
    /// <summary>Item para mostrar en la grilla: fecha, nombre del tipo y emoji activo (✅/❌).</summary>
    public class FeriadoTAASDisplay
    {
        public FeriadoTAAS Model { get; set; }
        public DateTime Fecha => Model.Feriado;
        public string TipoFeriadoNombre { get; set; } = "";
        public string ActivoDisplay => Model.Activo ? "✅" : "❌";
    }

    public class VMFeriadosTAAS : ViewModelBase
    {
        private readonly ServicioFeriadosTAAS _servicio;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (Set(ref _isLoading, value))
                    RaiseCommandsCanExecuteChanged();
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        // --- Tipos de Feriado ---
        public ObservableCollection<TipoFeriadoTAAS> Tipos { get; } = new ObservableCollection<TipoFeriadoTAAS>();
        private TipoFeriadoTAAS _tipoSeleccionado;
        public TipoFeriadoTAAS TipoSeleccionado
        {
            get => _tipoSeleccionado;
            set
            {
                if (Set(ref _tipoSeleccionado, value))
                    RaiseCommandsCanExecuteChanged();
            }
        }
        private string _tipoNombre = "";
        public string TipoNombre
        {
            get => _tipoNombre;
            set
            {
                if (Set(ref _tipoNombre, value ?? ""))
                    RaiseCommandsCanExecuteChanged();
            }
        }
        private int _tipoDias = 1;
        public int TipoDias
        {
            get => _tipoDias;
            set => Set(ref _tipoDias, value < 1 ? 1 : value);
        }

        // --- Feriados ---
        public ObservableCollection<FeriadoTAAS> Feriados { get; } = new ObservableCollection<FeriadoTAAS>();
        /// <summary>Colección para la grilla: Fecha, TipoFeriadoNombre, ActivoDisplay (✅/❌).</summary>
        public ObservableCollection<FeriadoTAASDisplay> FeriadosDisplay { get; } = new ObservableCollection<FeriadoTAASDisplay>();
        private FeriadoTAASDisplay _feriadoDisplaySeleccionado;
        public FeriadoTAASDisplay FeriadoDisplaySeleccionado
        {
            get => _feriadoDisplaySeleccionado;
            set
            {
                if (Set(ref _feriadoDisplaySeleccionado, value))
                {
                    FeriadoSeleccionado = value?.Model;
                    RaiseCommandsCanExecuteChanged();
                }
            }
        }
        private FeriadoTAAS _feriadoSeleccionado;
        public FeriadoTAAS FeriadoSeleccionado
        {
            get => _feriadoSeleccionado;
            set
            {
                if (Set(ref _feriadoSeleccionado, value))
                    RaiseCommandsCanExecuteChanged();
            }
        }
        private int? _filtroAnio;
        public int? FiltroAnio
        {
            get => _filtroAnio;
            set
            {
                if (Set(ref _filtroAnio, value))
                    RaisePropertyChanged(nameof(FiltroAnioTexto));
            }
        }
        /// <summary>Para binding del TextBox del filtro año (vacío = todos).</summary>
        public string FiltroAnioTexto
        {
            get => _filtroAnio.HasValue ? _filtroAnio.Value.ToString() : "";
            set
            {
                if (string.IsNullOrWhiteSpace(value)) { FiltroAnio = null; return; }
                if (int.TryParse(value.Trim(), out var anio)) FiltroAnio = anio;
            }
        }
        private DateTime _feriadoFecha = DateTime.Today;
        public DateTime FeriadoFecha
        {
            get => _feriadoFecha;
            set => Set(ref _feriadoFecha, value);
        }
        private bool _feriadoActivo = true;
        public bool FeriadoActivo
        {
            get => _feriadoActivo;
            set => Set(ref _feriadoActivo, value);
        }
        private int _feriadoIdTipo = 0;
        public int FeriadoIdTipo
        {
            get => _feriadoIdTipo;
            set
            {
                if (Set(ref _feriadoIdTipo, value))
                    RaiseCommandsCanExecuteChanged();
            }
        }

        public ICommand CargarTodoCommand { get; }
        public ICommand CrearTipoCommand { get; }
        public ICommand ActualizarTipoCommand { get; }
        public ICommand BorrarTipoCommand { get; }
        public ICommand CrearFeriadoCommand { get; }
        public ICommand ActualizarFeriadoCommand { get; }
        public ICommand BorrarFeriadoCommand { get; }
        public ICommand ToggleActivoFeriadoCommand { get; }

        public VMFeriadosTAAS()
        {
            _servicio = ServicioFeriadosTAAS.GetInstancia();

            CargarTodoCommand = new RelayCommand(async () => await CargarTodoAsync(), () => !IsLoading);
            CrearTipoCommand = new RelayCommand(async () => await CrearTipoAsync(), () => !IsLoading && !string.IsNullOrWhiteSpace(TipoNombre));
            ActualizarTipoCommand = new RelayCommand(async () => await ActualizarTipoAsync(), () => !IsLoading && TipoSeleccionado != null);
            BorrarTipoCommand = new RelayCommand(async () => await BorrarTipoAsync(), () => !IsLoading && TipoSeleccionado != null);
            CrearFeriadoCommand = new RelayCommand(async () => await CrearFeriadoAsync(), () => !IsLoading && FeriadoIdTipo > 0);
            ActualizarFeriadoCommand = new RelayCommand(async () => await ActualizarFeriadoAsync(), () => !IsLoading && FeriadoSeleccionado != null);
            BorrarFeriadoCommand = new RelayCommand(async () => await BorrarFeriadoAsync(), () => !IsLoading && FeriadoSeleccionado != null);
            ToggleActivoFeriadoCommand = new RelayCommand(async () => await ToggleActivoFeriadoAsync(), () => !IsLoading && FeriadoSeleccionado != null);
        }

        private void RaiseCommandsCanExecuteChanged()
        {
            (CargarTodoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CrearTipoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ActualizarTipoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (BorrarTipoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CrearFeriadoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ActualizarFeriadoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (BorrarFeriadoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ToggleActivoFeriadoCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        public async Task CargarTodoAsync()
        {
            IsLoading = true;
            StatusMessage = "";
            try
            {
                var tipos = await _servicio.ListTiposAsync();
                Tipos.Clear();
                foreach (var t in tipos) Tipos.Add(t);
                await CargarFeriadosAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al cargar: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
                RaiseCommandsCanExecuteChanged();
            }
        }

        private async Task CargarFeriadosAsync()
        {
            var lista = await _servicio.ListFeriadosAsync(FiltroAnio);
            FeriadoDisplaySeleccionado = null;
            Feriados.Clear();
            FeriadosDisplay.Clear();
            foreach (var f in lista)
            {
                Feriados.Add(f);
                var tipo = Tipos.FirstOrDefault(t => t.Id == f.IdTipoFeriado);
                FeriadosDisplay.Add(new FeriadoTAASDisplay
                {
                    Model = f,
                    TipoFeriadoNombre = tipo?.TipoFeriado ?? ""
                });
            }
        }

        public async Task CrearTipoAsync()
        {
            if (string.IsNullOrWhiteSpace(TipoNombre)) return;
            IsLoading = true;
            StatusMessage = "";
            try
            {
                var (id, err) = await _servicio.CreateTipoAsync(TipoNombre.Trim(), TipoDias);
                if (err != null) { StatusMessage = err; return; }
                var list = await _servicio.ListTiposAsync();
                Tipos.Clear();
                foreach (var t in list) Tipos.Add(t);
                TipoNombre = "";
                TipoDias = 1;
                StatusMessage = "Tipo creado correctamente.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsLoading = false; RaiseCommandsCanExecuteChanged(); }
        }

        public async Task ActualizarTipoAsync()
        {
            if (TipoSeleccionado == null) return;
            IsLoading = true;
            StatusMessage = "";
            try
            {
                var err = await _servicio.UpdateTipoAsync(TipoSeleccionado.Id, TipoNombre.Trim(), TipoDias);
                if (err != null) { StatusMessage = err; return; }
                var list = await _servicio.ListTiposAsync();
                Tipos.Clear();
                foreach (var t in list) Tipos.Add(t);
                TipoSeleccionado = null;
                StatusMessage = "Tipo actualizado.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsLoading = false; RaiseCommandsCanExecuteChanged(); }
        }

        public async Task BorrarTipoAsync()
        {
            if (TipoSeleccionado == null) return;
            IsLoading = true;
            StatusMessage = "";
            try
            {
                var err = await _servicio.DeleteTipoAsync(TipoSeleccionado.Id);
                if (err != null) { StatusMessage = err; return; }
                Tipos.Remove(TipoSeleccionado);
                TipoSeleccionado = null;
                await CargarFeriadosAsync();
                StatusMessage = "Tipo eliminado.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsLoading = false; RaiseCommandsCanExecuteChanged(); }
        }

        public async Task CrearFeriadoAsync()
        {
            if (FeriadoIdTipo <= 0) return;
            IsLoading = true;
            StatusMessage = "";
            try
            {
                var (id, err) = await _servicio.CreateFeriadoAsync(FeriadoFecha, FeriadoActivo, FeriadoIdTipo);
                if (err != null) { StatusMessage = err; return; }
                await CargarFeriadosAsync();
                StatusMessage = "Feriado creado.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsLoading = false; RaiseCommandsCanExecuteChanged(); }
        }

        public async Task ActualizarFeriadoAsync()
        {
            if (FeriadoSeleccionado == null) return;
            IsLoading = true;
            StatusMessage = "";
            try
            {
                var err = await _servicio.UpdateFeriadoAsync(FeriadoSeleccionado.Id, FeriadoFecha, FeriadoActivo, FeriadoIdTipo);
                if (err != null) { StatusMessage = err; return; }
                await CargarFeriadosAsync();
                FeriadoSeleccionado = null;
                StatusMessage = "Feriado actualizado.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsLoading = false; RaiseCommandsCanExecuteChanged(); }
        }

        public async Task BorrarFeriadoAsync()
        {
            if (FeriadoSeleccionado == null) return;
            IsLoading = true;
            StatusMessage = "";
            try
            {
                var err = await _servicio.DeleteFeriadoAsync(FeriadoSeleccionado.Id);
                if (err != null) { StatusMessage = err; return; }
                Feriados.Remove(FeriadoSeleccionado);
                FeriadoSeleccionado = null;
                StatusMessage = "Feriado eliminado.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsLoading = false; RaiseCommandsCanExecuteChanged(); }
        }

        public async Task ToggleActivoFeriadoAsync()
        {
            if (FeriadoSeleccionado == null) return;
            IsLoading = true;
            StatusMessage = "";
            try
            {
                var err = await _servicio.ToggleActivoAsync(FeriadoSeleccionado.Id);
                if (err != null) { StatusMessage = err; return; }
                await CargarFeriadosAsync();
                StatusMessage = "Estado activo actualizado.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsLoading = false; RaiseCommandsCanExecuteChanged(); }
        }

        public void AlSeleccionarTipo()
        {
            if (TipoSeleccionado == null) return;
            TipoNombre = TipoSeleccionado.TipoFeriado;
            TipoDias = TipoSeleccionado.Dias;
        }

        public void AlSeleccionarFeriado()
        {
            if (FeriadoSeleccionado == null) return;
            FeriadoFecha = FeriadoSeleccionado.Feriado;
            FeriadoActivo = FeriadoSeleccionado.Activo;
            FeriadoIdTipo = FeriadoSeleccionado.IdTipoFeriado;
        }
    }
}
