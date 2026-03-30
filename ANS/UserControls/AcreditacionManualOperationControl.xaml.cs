using ANS.Model.DTOs;
using ANS.ViewModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ANS.UserControls
{
    /// <summary>
    /// Interaction logic for AcreditacionManualOperationControl.xaml
    /// </summary>
    public partial class AcreditacionManualOperationControl : UserControl
    {
        public AcreditacionManualOperationControl()
        {
            InitializeComponent();

            var viewmodel = new VMacreditacionManualOperations();

            DataContext = viewmodel;

#if DEBUG
            Debug.WriteLine($"[AcreditacionManual] DataContext set: {DataContext?.GetType().FullName ?? "null"}");
#endif
        }

        private void DepositoCheckBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is not VMacreditacionManualOperations vm)
                return;

            // El binding TwoWay a IsSelected puede aplicarse después del evento Routed;
            // diferir para que AcreditarSeleccionadosCommand reevalúe con el valor ya persistido.
            Dispatcher.BeginInvoke(new Action(() => vm.NotifyDepositoSelectionChanged()),
                DispatcherPriority.Background);
        }

        private void DepositosDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not VMacreditacionManualOperations vm)
                return;

            // Solo marcar IsSelected al incluir filas en la selección del grid.
            // NO poner IsSelected = false en RemovedItems: al marcar varios checkboxes o cambiar el foco,
            // el DataGrid suele sacar filas de SelectedItems y eso desmarcaba depósitos que el usuario sigue queriendo acreditar.
            // Para desmarcar: checkbox o "Limpiar selección".
            foreach (var o in e.AddedItems)
            {
                if (o is not DepositoAcreditacionDto item)
                    continue;
                if (item.IsAcreditado || !item.HasCuentaAsignada)
                    continue;
                item.IsSelected = true;
            }

            vm.NotifyDepositoSelectionChanged();
        }
    }
}
