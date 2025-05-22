using ANS.Model.Services;
using ANS.Model;
using ANS.ViewModel;
using System.Windows.Controls;


namespace ANS.UserControls
{
    /// <summary>
    /// Interaction logic for HsbcOperationControl.xaml
    /// </summary>
    public partial class HsbcOperationControl : UserControl
    {
        public HsbcOperationControl()
        {

            InitializeComponent();

            var viewmodel = new VMhsbcOperations(ServicioBanco.getInstancia().getByNombre(VariablesGlobales.hsbc));

            DataContext = viewmodel;
        }
    }
}
