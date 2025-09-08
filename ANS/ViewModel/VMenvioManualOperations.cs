using ANS.Model;
using ANS.Model.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ANS.ViewModel
{
    public class VMenvioManualOperations : ViewModelBase
    {
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }

        public ObservableCollection<BuzonDTO> Buzones { get; } = new ObservableCollection<BuzonDTO>();
        public ICollectionView BuzonesView { get; }

        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (Set(ref _filterText, value))
                    BuzonesView?.Refresh();
            }
        }

        private BuzonDTO _buzonSelected;
        public BuzonDTO buzonSelected
        {
            get => _buzonSelected;
            set
            {
                if (Set(ref _buzonSelected, value))
                {
                    RaisePropertyChanged(nameof(IsHendersonSelected));
                    RaisePropertyChanged(nameof(HasSelection));
                    EjecutarEnvioManualCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasSelection => buzonSelected != null;
        public bool IsHendersonSelected => buzonSelected?.esHenderson() ?? false;

        private DateTime? _fechaSelected = DateTime.Today;
        public DateTime? fechaSelected
        {
            get => _fechaSelected;
            set
            {
                if (Set(ref _fechaSelected, value))
                    EjecutarEnvioManualCommand.RaiseCanExecuteChanged();
            }
        }

        private int? _numTandaSelected;
        public int? numTandaSelected
        {
            get => _numTandaSelected;
            set
            {
                if (Set(ref _numTandaSelected, value))
                    EjecutarEnvioManualCommand.RaiseCanExecuteChanged();
            }
        }

        public int[] Tandas { get; } = new[] { 1, 2 };

        public ServicioEnvioAcreditacionManual _sam { get; }
        private readonly ServicioCC _servicioCC;

        public RelayCommand EjecutarEnvioManualCommand { get; }
        public RelayCommand LimpiarSeleccionCommand { get; }

        public VMenvioManualOperations()
        {
            _sam = ServicioEnvioAcreditacionManual.getInstancia();
            _servicioCC = ServicioCC.getInstancia();

            var lista = _servicioCC?.listaBuzonesDTO ?? new System.Collections.Generic.List<BuzonDTO>();
            foreach (var b in lista) Buzones.Add(b);

            BuzonesView = CollectionViewSource.GetDefaultView(Buzones);
            BuzonesView.SortDescriptions.Clear();
            BuzonesView.SortDescriptions.Add(new SortDescription(nameof(BuzonDTO.NN), ListSortDirection.Ascending));
            BuzonesView.SortDescriptions.Add(new SortDescription(nameof(BuzonDTO.NC), ListSortDirection.Ascending));
            BuzonesView.Filter = o =>
            {
                if (o is not BuzonDTO b) return false;
                if (string.IsNullOrWhiteSpace(FilterText)) return true;
                var term = FilterText.Trim().ToLowerInvariant();
                return (b.NN ?? "").ToLowerInvariant().Contains(term)
                    || (b.NC ?? "").ToLowerInvariant().Contains(term)
                    || (b.Empresa ?? "").ToLowerInvariant().Contains(term);
            };

            EjecutarEnvioManualCommand = new RelayCommand(
                async () => await ejecutarEnvioManual(),
                () => CanEnviar());

            LimpiarSeleccionCommand = new RelayCommand(() =>
            {
                buzonSelected = null;
                numTandaSelected = null;
                // Podés conservar el filtro o limpiarlo:
                // FilterText = string.Empty;
            });
        }

        private bool CanEnviar()
        {
            if (!HasSelection) return false;
            if (!fechaSelected.HasValue) return false;
            if (IsHendersonSelected && !numTandaSelected.HasValue) return false;
            return true;
        }

        private async Task ejecutarEnvioManual()
        {
            IsLoading = true;
            try
            {
                var tanda = IsHendersonSelected ? (numTandaSelected ?? 0) : 0;
                await _sam.EnviarAcreditacionManual(buzonSelected, tanda, fechaSelected!.Value);
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
