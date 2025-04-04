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
using System.Windows.Shapes;

namespace ANS
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SeleccionDeBanco : Window
    {
        public SeleccionDeBanco()
        {
            InitializeComponent();
        }



        private void BankButton_Click(object sender, RoutedEventArgs e)
        {
   
            var button = sender as System.Windows.Controls.Button;

            string bancoSeleccionado = button.Tag.ToString();

            var modal = new BancoModal(bancoSeleccionado);

            modal.ShowDialog();
        }
    }
}
