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
    /// Interaction logic for ItauOperationControl.xaml
    /// </summary>
    public partial class ItauOperationControl : UserControl
    {
        public ItauOperationControl()
        {
            InitializeComponent();

            var viewmodel = new VMitauOperations(ServicioBanco.getInstancia().getByNombre(VariablesGlobales.itau));

            DataContext = viewmodel;
        }
    }
}
