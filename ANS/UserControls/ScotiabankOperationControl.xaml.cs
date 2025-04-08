using ANS.Model.Services;
using ANS.Model;
using ANS.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ANS.UserControls
{
    /// <summary>
    /// Interaction logic for ScotiabankOperationControl.xaml
    /// </summary>
    public partial class ScotiabankOperationControl : UserControl
    {
        public ScotiabankOperationControl()
        {
            InitializeComponent();

            var viewmodel = new VMscotiabankOperations(ServicioBanco.getInstancia().getByNombre(VariablesGlobales.scotiabank));

            DataContext = viewmodel;
        }
    }
}
