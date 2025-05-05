using ANS.Model;
using ANS.Model.Services;
using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;


namespace ANS.ViewModel
{
    public class VMpanelEmailBuzon : ViewModelBase
    {
        private bool _isLoading;
        private ObservableCollection<Buzon> _buzones;
        private ObservableCollection<Buzon> _listaOriginalBuzones;
        private ObservableCollection<Email> _emails = new ObservableCollection<Email>();
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }
        public ObservableCollection<Buzon> Buzones
        {
            get => _buzones;
            set => Set(ref _buzones, value);
        }
        public ObservableCollection<Email> Emails
        {
            get => _emails;
            set => Set(ref _emails, value);
        }
        private Buzon _selectedBuzon;
        public Buzon SelectedBuzon
        {
            get => _selectedBuzon;
            set
            {
                if (Set(ref _selectedBuzon, value))
                {
                    CargarEmailsDeBuzon(value);
                }
            }
        }
        public VMpanelEmailBuzon()
        {
            _buzones = new ObservableCollection<Buzon>();
            _listaOriginalBuzones = new ObservableCollection<Buzon>();
        cargarListaBuzones();
        }
        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (Set(ref _filterText, value))
                    FilterBuzon(FilterText);      
            }
        }

        private void FilterBuzon(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
       
                Buzones = new ObservableCollection<Buzon>(_listaOriginalBuzones);
            }
            else
            {
            
                var filtrados = _listaOriginalBuzones
                    .Where(b =>
                        !string.IsNullOrEmpty(b.NN)
                        && b.NN.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                    );
                Buzones = new ObservableCollection<Buzon>(filtrados);
            }
        }

        private void cargarListaBuzones()
        {

            var listaB = ServicioCC.getInstancia().getBuzones();
          
            foreach (var b in listaB)
                _listaOriginalBuzones.Add(b);

            Buzones = new ObservableCollection<Buzon>(_listaOriginalBuzones);

        }

        private void CargarEmailsDeBuzon(Buzon b)
        {
            Emails.Clear();

            if (b?._listaEmails == null) return;

            foreach (var email in b._listaEmails)
                if (email != null)
                    Emails.Add(email);
        }
    }
}
