using ANS.UserControls;
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

            BancoTitle.Text = $"Operaciones - {banco}";

            switch (banco)
            {
                case "Santander":
                    BankOperationsContent.Content = new SantanderOperationControl();
                    break;
                case "BBVA":
                    BankOperationsContent.Content = new BBVAOperationControl();
                    break;
                case "Scotiabank":
                    BankOperationsContent.Content = new ScotiabankOperationControl();
                    break;
                case "EnvioMasivo":
                    BankOperationsContent.Content = new EnvioMasivoOperationControl();
                    break;
                case "EnvioNiveles":
                    BankOperationsContent.Content = new EnvioNivelesOperationControl();
                    break;
            }
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
