using System.Windows;


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
