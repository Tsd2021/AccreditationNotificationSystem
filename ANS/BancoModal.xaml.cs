using ANS.Model;
using ANS.UserControls;
using ANS.Views;
using System.Windows;


namespace ANS
{
    /// <summary>
    /// Interaction logic for BancoModal.xaml
    /// </summary>
    public partial class BancoModal : Window
    {
        public BancoModal()
        {
            InitializeComponent();
        }


        public BancoModal(string banco)
        {
            InitializeComponent();

            // Título VISIBLE: mapea el alias legado al nombre nuevo (Hsbc -> BTG PACTUAL).
            // El 'banco' recibido sigue siendo la clave de ruteo interna (Tag del botón), no se toca.
            BancoTitle.Text = $"Operaciones - {IdentidadBanco.NombreVisible(banco)}";

            switch (banco)
            {
                case "Santander":
                    BankOperationsContent.Content = new SantanderOperationControl(); break;
                case "BBVA":
                    BankOperationsContent.Content = new BBVAOperationControl(); break;
                case "Scotiabank":
                    BankOperationsContent.Content = new ScotiabankOperationControl(); break;
                case "Itau":
                    BankOperationsContent.Content = new ItauOperationControl(); break;
                case "Hsbc":
                    BankOperationsContent.Content = new HsbcOperationControl(); break;
                case "Bandes":
                    BankOperationsContent.Content = new BandesOperationControl(); break;
                case "EnvioMasivo":
                    BankOperationsContent.Content = new EnvioMasivoOperationControl(); break;
                case "EnvioManual":
                    BankOperationsContent.Content = new EnvioManualOperationControl(); break;
                case "EnvioNiveles":
                    BankOperationsContent.Content = new EnvioNivelesOperationControl(); break;
                case "AcreditacionManual":
                    BankOperationsContent.Content = new AcreditacionManualOperationControl(); break;
                case "FeriadosTAAS":
                    BankOperationsContent.Content = new FeriadosTAASOperationControl(); break;
                case "AltaEmailDestino":
                    var altaWin = new AltaEmailDestino();
                    altaWin.ShowDialog();
                    this.Close();
                    break;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

    }
}
