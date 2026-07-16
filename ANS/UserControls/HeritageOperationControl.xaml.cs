using ANS.Model.Services;
using ANS.Model;
using ANS.ViewModel;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;


namespace ANS.UserControls
{
    /// <summary>
    /// Interaction logic for HeritageOperationControl.xaml
    /// </summary>
    public partial class HeritageOperationControl : UserControl
    {
        public HeritageOperationControl()
        {
            InitializeComponent();

            var viewmodel = new VMheritageOperations(ServicioBanco.getInstancia().getByNombre(VariablesGlobales.heritage));

            DataContext = viewmodel;
        }
    }
}
