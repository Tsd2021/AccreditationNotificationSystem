using ANS.Model;
using ANS.Model.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
                    MostrarClientes = false;
                    RaiseSelectionChanged();
                    (DeseleccionarClienteCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
                    TareaSeleccionada = null;
                    RaiseSelectionChanged();
                }
            }
        }

        // -- TareasEmail del banco --
        public IEnumerable<string> TareasEmail => BancoSeleccionado?.TareasEmail ?? Enumerable.Empty<string>();

        private string _tareaSeleccionada;
        public string TareaSeleccionada
        {
            get => _tareaSeleccionada;
            set
            {
                if (Set(ref _tareaSeleccionada, value))
                    RaiseSelectionChanged();
            }
        }

        // -- Ciudades --
        public ObservableCollection<string> Ciudades { get; } = new ObservableCollection<string>();

        private string _ciudadSeleccionada;
        public string CiudadSeleccionada
        {
            get => _ciudadSeleccionada;
            set
            {
                if (Set(ref _ciudadSeleccionada, value))
                    RaiseSelectionChanged();
            }
        }

        // -- Emails relacionados --
        public ObservableCollection<Email> RelatedEmails { get; } = new ObservableCollection<Email>();
        
        // Diccionario para guardar los emails originales antes de la edición
        private readonly Dictionary<Email, string> _emailsOriginales = new Dictionary<Email, string>();

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

        private bool _activo;
        public bool Activo
        {
            get => _activo;
            set => Set(ref _activo, value);
        }

        // -- Comandos --
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand ToggleClientesCommand { get; }
        public ICommand DeseleccionarClienteCommand { get; }
        public ICommand EliminarEmailCommand { get; }
        public ICommand ModificarEmailCommand { get; }

        public VMaltaEmailDestino()
        {
            ToggleClientesCommand = new RelayCommand(() =>
            {
                MostrarClientes = !MostrarClientes;
                if (MostrarClientes) AplicarFiltro();
            });

            DeseleccionarClienteCommand = new RelayCommand(
                DeseleccionarCliente,
                () => ClienteSeleccionado != null
            );

            GuardarCommand = new RelayCommand(Guardar, CanGuardar);
            CancelarCommand = new RelayCommand(Cancelar);
            EliminarEmailCommand = new RelayCommand<Email>(EliminarEmail, CanEliminarEmail);
            ModificarEmailCommand = new RelayCommand<Email>(ModificarEmail, CanModificarEmail);

            CargarDatosIniciales();
            AplicarFiltro();
        }

        public VMaltaEmailDestino(Banco banco, Cliente cliente) : this()
        {
            Bancos.Clear();
            Bancos.Add(banco);
            BancoSeleccionado = banco;

            _todosClientes.Clear();
            Clientes.Clear();
            var clientesBanco = ServicioCliente.getInstancia().ListaClientes;
            _todosClientes.AddRange(clientesBanco);
            foreach (var c in clientesBanco)
                Clientes.Add(c);

            ClienteSeleccionado = _todosClientes.FirstOrDefault(c => c.IdCliente == cliente?.IdCliente);
            CiudadSeleccionada = Ciudades.FirstOrDefault() ?? string.Empty;
        }

        private void CargarDatosIniciales()
        {
            _todosClientes.AddRange(ServicioCliente.getInstancia().ListaClientes);
            foreach (var c in _todosClientes) Clientes.Add(c);
            foreach (var b in ServicioBanco.getInstancia().ListaBancos) Bancos.Add(b);
            Ciudades.Add("Maldonado");
            Ciudades.Add("Montevideo");
        }

        private void AplicarFiltro()
        {
            Clientes.Clear();
            var filtrada = string.IsNullOrWhiteSpace(FiltroCliente)
                ? _todosClientes
                : _todosClientes.Where(c => c.Nombre.IndexOf(FiltroCliente, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (var c in filtrada) Clientes.Add(c);
        }

        private void RaiseSelectionChanged()
        {
            // Ahora solo depende de banco y tarea
            if (BancoSeleccionado != null && !string.IsNullOrWhiteSpace(TareaSeleccionada))
                CargarEmailsAsociados();
            else
                RelatedEmails.Clear();

            (GuardarCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeseleccionarClienteCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void CargarEmailsAsociados()
        {
            try
            {
                var lista = ServicioEmail
                    .getInstancia()
                    .ListarPor(
                        ClienteSeleccionado?.IdCliente,
                        BancoSeleccionado.NombreBanco,
                        TareaSeleccionada,
                        CiudadSeleccionada
                    );
                RelatedEmails.Clear();
                _emailsOriginales.Clear();
                foreach (var e in lista)
                {
                    RelatedEmails.Add(e);
                    // Guardar el email original para poder modificar después
                    _emailsOriginales[e] = e.Correo;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error al cargar emails:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanGuardar()
        {
            // El cliente ya no es requerido
            return BancoSeleccionado != null
                && !string.IsNullOrWhiteSpace(TareaSeleccionada)
                && string.IsNullOrEmpty(this[nameof(NuevoEmail)]);
        }

        private void Guardar()
        {
            try
            {
                int filas = ServicioEmail
                    .getInstancia()
                    .AgregarEmailDestino(
                        banco: BancoSeleccionado.NombreBanco,
                        idCliente: ClienteSeleccionado?.IdCliente,
                        tarea: TareaSeleccionada,
                        correo: NuevoEmail,
                        activo: Activo,
                        ciudad: CiudadSeleccionada
                    );
                if (filas > 0)
                {
                    CargarEmailsAsociados();
                    NuevoEmail = string.Empty;
                    Activo = false;
                }
                else
                {
                    MessageBox.Show("No se guardó ningún registro.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error SQL al guardar:\n{ex.Message}", "Error SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeseleccionarCliente()
        {
            ClienteSeleccionado = null;
            MostrarClientes = false;
        }

        private void Cancelar()
        {
            // Lógica cancelar
        }

        private bool CanEliminarEmail(Email email)
        {
            return email != null 
                && BancoSeleccionado != null 
                && !string.IsNullOrWhiteSpace(TareaSeleccionada)
                && !string.IsNullOrWhiteSpace(CiudadSeleccionada);
        }

        private void EliminarEmail(Email email)
        {
            if (email == null) return;

            var resultado = MessageBox.Show(
                $"¿Está seguro que desea eliminar el email '{email.Correo}'?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado != MessageBoxResult.Yes) return;

            try
            {
                int filas = ServicioEmail
                    .getInstancia()
                    .EliminarEmailDestino(
                        banco: BancoSeleccionado.NombreBanco,
                        idCliente: ClienteSeleccionado?.IdCliente,
                        tarea: TareaSeleccionada,
                        correo: email.Correo,
                        ciudad: CiudadSeleccionada
                    );

                if (filas > 0)
                {
                    MessageBox.Show("Email eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    _emailsOriginales.Remove(email);
                    CargarEmailsAsociados();
                }
                else
                {
                    MessageBox.Show("No se pudo eliminar el email. Verifique que exista en la base de datos.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error SQL al eliminar:\n{ex.Message}", "Error SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanModificarEmail(Email email)
        {
            return email != null 
                && BancoSeleccionado != null 
                && !string.IsNullOrWhiteSpace(TareaSeleccionada)
                && !string.IsNullOrWhiteSpace(CiudadSeleccionada)
                && !string.IsNullOrWhiteSpace(email.Correo);
        }

        private void ModificarEmail(Email email)
        {
            if (email == null) return;

            // Validar el nuevo email
            if (string.IsNullOrWhiteSpace(email.Correo))
            {
                MessageBox.Show("El email no puede estar vacío.", "Error de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                // Restaurar el email original
                if (_emailsOriginales.ContainsKey(email))
                    email.Correo = _emailsOriginales[email];
                return;
            }

            // Validar caracteres especiales no permitidos
            char[] caracteresNoPermitidos = { ',', ';', ' ', '\t', '\n', '\r' };
            if (email.Correo.IndexOfAny(caracteresNoPermitidos) >= 0)
            {
                MessageBox.Show("El email no puede contener caracteres especiales como comas, punto y coma o espacios.", "Error de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                // Restaurar el email original
                if (_emailsOriginales.ContainsKey(email))
                    email.Correo = _emailsOriginales[email];
                return;
            }

            // Validar formato de email
            if (!Regex.IsMatch(email.Correo, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                MessageBox.Show("Formato de email inválido.", "Error de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                // Restaurar el email original
                if (_emailsOriginales.ContainsKey(email))
                    email.Correo = _emailsOriginales[email];
                return;
            }

            // Obtener el email original
            if (!_emailsOriginales.ContainsKey(email))
            {
                MessageBox.Show("No se pudo encontrar el email original para modificar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string correoViejo = _emailsOriginales[email];
            
            // Si no cambió, no hacer nada
            if (correoViejo == email.Correo)
            {
                return;
            }

            try
            {
                int filas = ServicioEmail
                    .getInstancia()
                    .ModificarEmailDestino(
                        banco: BancoSeleccionado.NombreBanco,
                        idCliente: ClienteSeleccionado?.IdCliente,
                        tarea: TareaSeleccionada,
                        correoViejo: correoViejo,
                        correoNuevo: email.Correo,
                        activo: email.Activo,
                        ciudad: CiudadSeleccionada
                    );

                if (filas > 0)
                {
                    MessageBox.Show("Email modificado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Actualizar el email original guardado
                    _emailsOriginales[email] = email.Correo;
                    CargarEmailsAsociados();
                }
                else
                {
                    MessageBox.Show("No se pudo modificar el email. Verifique que exista en la base de datos.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Restaurar el email original
                    email.Correo = correoViejo;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error SQL al modificar:\n{ex.Message}", "Error SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                // Restaurar el email original
                email.Correo = correoViejo;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Restaurar el email original
                email.Correo = correoViejo;
            }
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
                    
                    // Validar caracteres especiales no permitidos (comas, punto y coma, espacios, etc.)
                    char[] caracteresNoPermitidos = { ',', ';', ' ', '\t', '\n', '\r' };
                    if (NuevoEmail.IndexOfAny(caracteresNoPermitidos) >= 0)
                        return "El email no puede contener caracteres especiales como comas, punto y coma o espacios.";
                    
                    // Validar formato básico de email
                    if (!Regex.IsMatch(NuevoEmail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                        return "Formato de email inválido.";
                }
                return null;
            }
        }
        #endregion
    }
}
