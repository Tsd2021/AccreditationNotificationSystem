using ANS.Model;
using ANS.Model.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ANS.ViewModel
{
    public class VMaltaEmailDestino : ViewModelBase, IDataErrorInfo
    {
        // -- Clientes y filtrado --
        private readonly List<Cliente> _todosClientes = new List<Cliente>();
        public ObservableCollection<Cliente> Clientes { get; } = new ObservableCollection<Cliente>();

        private bool _mostrarClientes;
        public bool MostrarClientes
        {
            get => _mostrarClientes;
            set => Set(ref _mostrarClientes, value);
        }

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

        private Cliente? _clienteSeleccionado;
        public Cliente? ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set
            {
                if (Set(ref _clienteSeleccionado, value))
                {
                    // Actualiza la lista de emails y los estados de los comandos
                    RaiseSelectionChanged();
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
                }
            }
        }

        public ObservableCollection<string> Ciudades { get; } = new ObservableCollection<string>();

        private string _ciudadSeleccionada;
        public string CiudadSeleccionada
        {
            get => _ciudadSeleccionada;
            set
            {
                if (Set(ref _ciudadSeleccionada, value))
                {
                    RaiseSelectionChanged();
                }
            }
        }

        // -- Tipos de acreditación --
        public ObservableCollection<ConfiguracionAcreditacion> TiposAcreditacion { get; }
            = new ObservableCollection<ConfiguracionAcreditacion>();

        private ConfiguracionAcreditacion _tipoSeleccionado;
        public ConfiguracionAcreditacion TipoSeleccionado
        {
            get => _tipoSeleccionado;
            set
            {
                if (Set(ref _tipoSeleccionado, value))
                {
                    RaiseSelectionChanged();
                }
            }
        }

        // -- Emails relacionados --
        public ObservableCollection<Email> RelatedEmails { get; }
            = new ObservableCollection<Email>();

        // -- Nuevo email --
        private string _nuevoEmail;
        public string NuevoEmail
        {
            get => _nuevoEmail;
            set
            {
                if (Set(ref _nuevoEmail, value))
                    (GuardarCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
        public ICommand ToggleClientesCommand { get; }
        public ICommand DeseleccionarClienteCommand { get; }

        public VMaltaEmailDestino()
        {
            // Alternar visibilidad del panel de clientes
            ToggleClientesCommand = new RelayCommand(() =>
            {
                MostrarClientes = !MostrarClientes;
                if (MostrarClientes) AplicarFiltro();
            });

            // Deseleccionar solo si hay uno elegido
            DeseleccionarClienteCommand = new RelayCommand(
                execute: DeseleccionarCliente,
                canExecute: () => ClienteSeleccionado != null
            );

            GuardarCommand = new RelayCommand(Guardar, CanGuardar);
            CancelarCommand = new RelayCommand(Cancelar);

            CargarDatosIniciales();
            AplicarFiltro();
        }

        // Constructor auxiliar (si lo usas desde otra parte)
        public VMaltaEmailDestino(Banco banco,
                                 Cliente cliente,
                                 ConfiguracionAcreditacion tipoAcreditacion)
            : this()
        {
            CargarDatosIniciales(banco, tipoAcreditacion);
            AplicarFiltro();

            BancoSeleccionado = Bancos
                .FirstOrDefault(b => b.NombreBanco == banco.NombreBanco);
            ClienteSeleccionado = _todosClientes
                .FirstOrDefault(c => c.IdCliente == cliente?.IdCliente);
            FiltroCliente = cliente?.Nombre ?? "";

            CiudadSeleccionada = "Montevideo"; // Asignar ciudad por defecto

            TipoSeleccionado = TiposAcreditacion
                .FirstOrDefault(t => t.TipoAcreditacion == tipoAcreditacion.TipoAcreditacion);
        }

        private void CargarDatosIniciales()
        {
            _todosClientes.AddRange(ServicioCliente.getInstancia().ListaClientes);
            foreach (var c in _todosClientes) Clientes.Add(c);

            foreach (var b in ServicioBanco.getInstancia().ListaBancos)
                Bancos.Add(b);

            Ciudades.Add("Maldonado");
            Ciudades.Add("Montevideo");

            //TiposAcreditacion.Add(new ConfiguracionAcreditacion { TipoAcreditacion = "DiaADia" });
            //TiposAcreditacion.Add(new ConfiguracionAcreditacion { TipoAcreditacion = "PuntoAPunto" });
            //TiposAcreditacion.Add(new ConfiguracionAcreditacion { TipoAcreditacion = "Tanda" });
        }

        private void CargarDatosIniciales(Banco b, ConfiguracionAcreditacion config)
        {
            _todosClientes.AddRange(
                ServicioCliente.getInstancia()
                    .ObtenerClientesPorBancoYTipoAcreditacion(b, config)
            );
            foreach (var c in _todosClientes) Clientes.Add(c);

            foreach (var bank in ServicioBanco.getInstancia().ListaBancos)
                Bancos.Add(bank);

            TiposAcreditacion.Add(new ConfiguracionAcreditacion { TipoAcreditacion = "DiaADia" });
            TiposAcreditacion.Add(new ConfiguracionAcreditacion { TipoAcreditacion = "PuntoAPunto" });
            TiposAcreditacion.Add(new ConfiguracionAcreditacion { TipoAcreditacion = "Tanda" });
        }

        private void AplicarFiltro()
        {
            Clientes.Clear();
            var filtrada = string.IsNullOrWhiteSpace(FiltroCliente)
                ? _todosClientes
                : _todosClientes.Where(c =>
                    c.Nombre.IndexOf(FiltroCliente,
                                      StringComparison.OrdinalIgnoreCase) >= 0
                  );
            foreach (var c in filtrada) Clientes.Add(c);
        }

        private bool CanGuardar()
        {
            return BancoSeleccionado != null
                && TipoSeleccionado != null
                && string.IsNullOrEmpty(this[nameof(NuevoEmail)]);
        }

        private void Guardar()
        {
            try
            {
                int? idCli = ClienteSeleccionado?.IdCliente;
                string nombreCli = ClienteSeleccionado?.Nombre;

                int filas = ServicioEmail
                    .getInstancia()
                    .AgregarEmailDestino(
                        banco: BancoSeleccionado.NombreBanco,
                        idCliente: idCli,
                        cliente: nombreCli,
                        tipoAcreditacion: TipoSeleccionado.TipoAcreditacion,
                        correo: NuevoEmail,
                        esPrincipal: EsPrincipal,
                        ciudad: CiudadSeleccionada
                    );

                if (filas > 0)
                {
                    CargarEmailsAsociados();
                    NuevoEmail = string.Empty;
                    EsPrincipal = false;
                    MostrarClientes = false;
                }
                else
                {
                    MessageBox.Show(
                        "No se guardó ningún registro.",
                        "Atención",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show(
                    $"Error al guardar en la base de datos:\n{sqlEx.Message}",
                    "Error SQL",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ocurrió un error inesperado:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Cancelar()
        {
            // tu lógica de cancelar...
        }

        private void RaiseSelectionChanged()
        {
            if (BancoSeleccionado != null && TipoSeleccionado != null)
                CargarEmailsAsociados();
            else
                RelatedEmails.Clear();

            // Actualiza habilitación de comandos
            (GuardarCommand as RelayCommand)
                ?.RaiseCanExecuteChanged();
            (DeseleccionarClienteCommand as RelayCommand)
                ?.RaiseCanExecuteChanged();
        }

        private void CargarEmailsAsociados()
        {
            var idCli = ClienteSeleccionado?.IdCliente;
            var lista = ServicioEmail
                .getInstancia()
                .ListarPor(
                    idCli,
                    BancoSeleccionado.NombreBanco,
                    TipoSeleccionado.TipoAcreditacion
                );

            RelatedEmails.Clear();
            foreach (var e in lista) RelatedEmails.Add(e);
        }

        private void DeseleccionarCliente()
        {
            ClienteSeleccionado = null;
            MostrarClientes = false;
        }

        #region IDataErrorInfo
        public string Error => null;
        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(NuevoEmail))
                {
                    if (string.IsNullOrWhiteSpace(NuevoEmail))
                        return "El email es obligatorio.";
                    if (!Regex.IsMatch(NuevoEmail,
                        @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        return "Formato de email inválido.";
                }
                return null;
            }
        }
        #endregion
    }
}
