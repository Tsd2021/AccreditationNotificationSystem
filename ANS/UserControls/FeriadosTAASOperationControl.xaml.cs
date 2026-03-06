using ANS.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace ANS.UserControls
{
    public partial class FeriadosTAASOperationControl : UserControl
    {
        public FeriadosTAASOperationControl()
        {
            InitializeComponent();
            DataContext = new VMFeriadosTAAS();
            Loaded += async (s, e) =>
            {
                if (DataContext is VMFeriadosTAAS vm)
                    await vm.CargarTodoAsync();
            };
        }

        private void TipoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is VMFeriadosTAAS vm && vm.TipoSeleccionado != null)
                vm.AlSeleccionarTipo();
        }

        private void FeriadoGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is VMFeriadosTAAS vm && vm.FeriadoSeleccionado != null)
                vm.AlSeleccionarFeriado();
        }
    }
}
