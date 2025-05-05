using ANS.ViewModel;
using System.Windows.Controls;


namespace ANS.UserControls
{
    /// <summary>
    /// Interaction logic for PanelAsociarEmailABuzon.xaml
    /// </summary>
    public partial class PanelAsociarEmailABuzon : UserControl
    {
        public PanelAsociarEmailABuzon()
        {

            var viewmodel = new VMpanelEmailBuzon();

            DataContext = viewmodel;

            InitializeComponent();

        }

        private void CheckBox_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {

        }
    }
}
