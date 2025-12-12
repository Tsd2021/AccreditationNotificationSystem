using ANS.Model;
using ANS.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace ANS.Views
{
    /// <summary>
    /// Interaction logic for AltaEmailDestino.xaml
    /// </summary>
    public partial class AltaEmailDestino : Window
    {
        public AltaEmailDestino()
        {
            InitializeComponent();

            var viewmodel = new VMaltaEmailDestino();

            DataContext = viewmodel;
        }

        public AltaEmailDestino(Banco banco,Cliente cliente,ConfiguracionAcreditacion tanda)
        {
            InitializeComponent();

            var viewmodel = new VMaltaEmailDestino(banco,cliente);

            DataContext = viewmodel;
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Si se editó la columna "Activo", guardar automáticamente
            if (e.Column.Header?.ToString() == "Activo" && e.Row.Item is Email email)
            {
                var viewModel = DataContext as VMaltaEmailDestino;
                if (viewModel != null && viewModel.ModificarEmailCommand.CanExecute(email))
                {
                    // Pequeño delay para asegurar que el binding se complete
                    Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        viewModel.ModificarEmailCommand.Execute(email);
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
        }
    }
}
