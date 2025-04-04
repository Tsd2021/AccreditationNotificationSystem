using ANS.Model;
using ANS.Model.Services;
using ANS.ViewModel;
using System.Windows.Controls;

namespace ANS.UserControls
{
    /// <summary>
    /// Interaction logic for SantanderOperationControl.xaml
    /// </summary>
    public partial class SantanderOperationControl : UserControl
    {

        public SantanderOperationControl()
        {
            InitializeComponent();

            var viewmodel = new VMsantanderOperations(ServicioBanco.getInstancia().getByNombre(VariablesGlobales.santander));

            DataContext = viewmodel;
        }

    }
}
