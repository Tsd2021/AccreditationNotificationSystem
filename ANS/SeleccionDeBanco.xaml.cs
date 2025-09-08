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
            var button = (System.Windows.Controls.Button)sender;
            string tag = button.Tag?.ToString();

            try
            {
                if (tag == "AltaEmailDestino")
                {
                    var altaWin = new AltaEmailDestino
                    {
                        Owner = this
                    };
                    altaWin.ShowDialog();
                }
                else
                {
                    var modal = new BancoModal(tag)
                    {
                        Owner = this
                    };
                    modal.ShowDialog();
                }
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                MessageBox.Show(
                    $"Error al inicializar la ventana:\n{tie.InnerException?.Message ?? tie.Message}",
                    "Error de Invocación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error inesperado:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
