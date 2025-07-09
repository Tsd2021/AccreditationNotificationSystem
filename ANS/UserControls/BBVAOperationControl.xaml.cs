using ANS.Model.Services;
using ANS.Model;
using ANS.ViewModel;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace ANS.UserControls
{
    /// <summary>
    /// Interaction logic for BBVAOperationControl.xaml
    /// </summary>
    public partial class BBVAOperationControl : UserControl
    {
        public BBVAOperationControl()
        {
            InitializeComponent();

            var viewmodel = new VMbbvaOperations(ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bbva));

            DataContext = viewmodel;
        }
    }
}
