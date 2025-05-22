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
                    (GuardarCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
                    (GuardarCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
                    (GuardarCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
                    (GuardarCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        private bool _esPrincipal;
        public bool EsPrincipal
        {
            get => _esPrincipal;
            set => Set(ref _esPrincipal, value);
        }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public VMaltaEmailDestino()
        {
            GuardarCommand = new RelayCommand(Guardar, CanGuardar);
            CancelarCommand = new RelayCommand(Cancelar);

            CargarDatosIniciales();
            AplicarFiltro();
        }
        public VMaltaEmailDestino(Banco banco, Cliente cliente, ConfiguracionAcreditacion tipoAcreditacion)
        {

            CargarDatosIniciales(banco, tipoAcreditacion);
            AplicarFiltro();
            BancoSeleccionado = Bancos
                .FirstOrDefault(b => b.NombreBanco == banco.NombreBanco);

            if (cliente != null)
            {
                ClienteSeleccionado = Clientes
       .FirstOrDefault(c => c.IdCliente == cliente.IdCliente);

                FiltroCliente = cliente.Nombre;
            }

            TipoSeleccionado = TiposAcreditacion
                .FirstOrDefault(t => t.TipoAcreditacion == tipoAcreditacion.TipoAcreditacion);

            GuardarCommand = new RelayCommand(Guardar, CanGuardar);
            CancelarCommand = new RelayCommand(Cancelar);


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

        private void CargarDatosIniciales(Banco b,ConfiguracionAcreditacion config)
        {
            _todosClientes.AddRange(ServicioCliente.getInstancia().ObtenerClientesPorBancoYTipoAcreditacion(b,config));
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
                : _todosClientes.Where(c => c.Nombre.IndexOf(FiltroCliente ?? "", StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (var c in filtrada) Clientes.Add(c);
        }
        private bool CanGuardar()
        {
            // sólo permitir guardar si no hay errores en NuevoEmail y todo está seleccionado
            return ClienteSeleccionado != null
                && BancoSeleccionado != null
                && TipoSeleccionado != null
                && string.IsNullOrEmpty(this[nameof(NuevoEmail)]);
        }
        private void Guardar()
        {
            try
            {
                int filas =
                    ServicioEmail
                        .getInstancia()
                        .AgregarEmailDestino(
                            BancoSeleccionado.NombreBanco,
                            ClienteSeleccionado.IdCliente,
                            ClienteSeleccionado.Nombre,
                            TipoSeleccionado.TipoAcreditacion,
                            NuevoEmail,
                            EsPrincipal);

                if (filas > 0)
                {
                    CargarEmailsAsociados();
                    NuevoEmail = string.Empty;
                    EsPrincipal = false;
                }
                else
                {
                    MessageBox.Show("No se guardó ningún registro.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (SqlException sqlEx)
            {

                MessageBox.Show(
                    $"Error al guardar en la base de datos:\n{sqlEx.Message}",
                    "Error SQL",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    $"Ocurrió un error inesperado:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void Cancelar()
        {
            // tu lógica de cancelar…
        }
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
        private void CargarEmailsAsociados()
        {
            var lista = ServicioEmail
                            .getInstancia()
                            .ListarPor(
                                ClienteSeleccionado.IdCliente,
                                BancoSeleccionado.NombreBanco,
                                TipoSeleccionado.TipoAcreditacion);

            RelatedEmails.Clear();
            foreach (var e in lista)
                RelatedEmails.Add(e);
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

                    if (!Regex.IsMatch(NuevoEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        return "Formato de email inválido.";
                }
                return null;
            }
        }

        #endregion
    }
}
