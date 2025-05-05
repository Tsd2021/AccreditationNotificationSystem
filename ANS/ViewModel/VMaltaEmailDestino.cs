using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ANS.Model;               // Aquí tus clases Cliente, Banco, ConfiguracionAcreditacion, EmailDestino
using ANS.Model.Services;      // ServicioCliente, ServicioBanco, ServicioEmailDestino

namespace ANS.ViewModel
{
    public class VMaltaEmailDestino : ViewModelBase
    {
        // -- Clientes y filtrado (igual que antes) --
        private readonly List<Cliente> _todosClientes = new List<Cliente>();
        public ObservableCollection<Cliente> Clientes { get; } = new ObservableCollection<Cliente>();
        private string _filtroCliente;
        public string FiltroCliente
        {
            get => _filtroCliente;
            set
            {
                if (Set(ref _filtroCliente, value))
                    AplicarFiltro();
            }
        }
        private Cliente _clienteSeleccionado;
        public Cliente ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set
            {
                if (Set(ref _clienteSeleccionado, value))
                {
                    RaiseSelectionChanged();
                    ((RelayCommand)GuardarCommand).RaiseCanExecuteChanged();
                }
            }
        }

        // -- Bancos --
        public ObservableCollection<Banco> Bancos { get; } = new ObservableCollection<Banco>();
        private Banco _bancoSeleccionado;
        public Banco BancoSeleccionado
        {
            get => _bancoSeleccionado;
            set
            {
                if (Set(ref _bancoSeleccionado, value))
                {
                    RaiseSelectionChanged();
                    ((RelayCommand)GuardarCommand).RaiseCanExecuteChanged();
                }
            }
        }

        // -- Tipos de acreditación --
        public ObservableCollection<ConfiguracionAcreditacion> TiposAcreditacion { get; } = new ObservableCollection<ConfiguracionAcreditacion>();
        private ConfiguracionAcreditacion _tipoSeleccionado;
        public ConfiguracionAcreditacion TipoSeleccionado
        {
            get => _tipoSeleccionado;
            set
            {
                if (Set(ref _tipoSeleccionado, value))
                {
                    RaiseSelectionChanged();
                    ((RelayCommand)GuardarCommand).RaiseCanExecuteChanged();
                }
            }
        }

        // -- Emails relacionados --
        public ObservableCollection<Email> RelatedEmails { get; } = new ObservableCollection<Email>();

        // -- Nuevo email --
        private string _nuevoEmail;
        public string NuevoEmail
        {
            get => _nuevoEmail;
            set
            {
                if (Set(ref _nuevoEmail, value))
                    ((RelayCommand)GuardarCommand).RaiseCanExecuteChanged();
            }
        }

        private bool _esPrincipal;
        public bool EsPrincipal
        {
            get => _esPrincipal;
            set => Set(ref _esPrincipal, value);
        }

        // -- Comandos --
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public VMaltaEmailDestino()
        {
            GuardarCommand = new RelayCommand(Guardar, CanGuardar);
            CancelarCommand = new RelayCommand(Cancelar);

            CargarDatosIniciales();
            AplicarFiltro();
        }

        private void CargarDatosIniciales()
        {
            _todosClientes.AddRange(ServicioCliente.getInstancia().ListaClientes);
            foreach (var c in _todosClientes) Clientes.Add(c);

            foreach (var b in ServicioBanco.getInstancia().ListaBancos)
                Bancos.Add(b);

            TiposAcreditacion.Add(new ConfiguracionAcreditacion { TipoAcreditacion = "DiaADia" });
            TiposAcreditacion.Add(new ConfiguracionAcreditacion { TipoAcreditacion = "PuntoAPunto" });
            TiposAcreditacion.Add(new ConfiguracionAcreditacion { TipoAcreditacion = "Tanda" });
        }

        private void AplicarFiltro()
        {
            Clientes.Clear();
            var filtrada = string.IsNullOrWhiteSpace(FiltroCliente)
                ? _todosClientes
                : _todosClientes.Where(c => c.Nombre.ToLower().Contains(FiltroCliente.ToLower()));
            foreach (var c in filtrada) Clientes.Add(c);
        }

        private bool CanGuardar()
        {
            return ClienteSeleccionado != null
                && BancoSeleccionado != null
                && TipoSeleccionado != null
                && !string.IsNullOrWhiteSpace(NuevoEmail);
        }

        private void Guardar()
        {
            if (ClienteSeleccionado == null ||
                BancoSeleccionado == null ||
                TipoSeleccionado == null)
            {
                return;
            }

     
            if (ServicioEmail.getInstancia().AgregarEmailDestino(BancoSeleccionado.NombreBanco,
                ClienteSeleccionado.IdCliente,
                ClienteSeleccionado.Nombre,
                TipoSeleccionado.TipoAcreditacion,
                NuevoEmail,
                EsPrincipal) > 0)
            {
                CargarEmailsAsociados();

                NuevoEmail = string.Empty;

                EsPrincipal = false;

            }

        }

        private void Cancelar()
        {
            // tu lógica de cancelar…
        }

        // Este método centraliza la comprobación de las tres selecciones
        private void RaiseSelectionChanged()
        {
            if (ClienteSeleccionado != null
             && BancoSeleccionado != null
             && TipoSeleccionado != null)
            {
                CargarEmailsAsociados();
            }
            else
            {
                RelatedEmails.Clear();
            }
        }

        // Y aquí realmente obtienes los emails de tu servicio
        private void CargarEmailsAsociados()
        {
            List<Email> lista = ServicioEmail
                            .getInstancia()
                            .ListarPor(ClienteSeleccionado.IdCliente,
                                        BancoSeleccionado.NombreBanco,
                                        TipoSeleccionado.TipoAcreditacion);

            RelatedEmails.Clear();

            foreach (Email e in lista)
                RelatedEmails.Add(e);
        }
    }
}
