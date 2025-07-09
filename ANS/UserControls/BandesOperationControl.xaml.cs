using ANS.Model.Services;
using ANS.Model;
using ANS.ViewModel;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;


namespace ANS.UserControls
{
    /// <summary>
    /// Interaction logic for BandesOperationControl.xaml
    /// </summary>
    public partial class BandesOperationControl : UserControl
    {
        public BandesOperationControl()
        {
            InitializeComponent();

            var viewmodel = new VMbandesOperations(ServicioBanco.getInstancia().getByNombre(VariablesGlobales.bandes));

            DataContext = viewmodel;
        }
    }
}
