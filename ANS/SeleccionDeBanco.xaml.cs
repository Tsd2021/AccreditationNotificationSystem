using ANS.Views;
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

            if (bancoSeleccionado == "AltaEmailDestino")
            {
                // Abro directamente la ventana de alta de email destino
                var altaWin = new AltaEmailDestino();
                altaWin.Owner = this;      // opcional si quieres que esté sobre MainWindow
                altaWin.ShowDialog();      // modal
                                           // aquí no hago nada más, nunca llamo a BancoModal.ShowDialog()
            }
            else
            {
                // Para cualquier otro banco, uso BancoModal
                var modal = new BancoModal(bancoSeleccionado);
                modal.Owner = this;        // opcional
                modal.ShowDialog();
            }
        }
    }
}
