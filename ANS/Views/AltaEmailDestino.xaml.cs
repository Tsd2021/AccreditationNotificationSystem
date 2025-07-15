using ANS.Model;
using ANS.ViewModel;
using System.Windows;

namespace ANS.Views
{
    /// <summary>
    /// Interaction logic for AltaEmailDestino.xaml
    /// </summary>
    public partial class AltaEmailDestino : Window
    {
        public AltaEmailDestino()
        {
            InitializeComponent();

            var viewmodel = new VMaltaEmailDestino();

            DataContext = viewmodel;
        }




        public AltaEmailDestino(Banco banco,Cliente cliente,ConfiguracionAcreditacion tanda)
        {
            InitializeComponent();

            var viewmodel = new VMaltaEmailDestino(banco,cliente);

            DataContext = viewmodel;
        }
    }
}
