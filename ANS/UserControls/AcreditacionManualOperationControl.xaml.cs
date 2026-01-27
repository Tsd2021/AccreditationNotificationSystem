using ANS.ViewModel;
using System.Diagnostics;
using System.Windows.Controls;

namespace ANS.UserControls
{
    /// <summary>
    /// Interaction logic for AcreditacionManualOperationControl.xaml
    /// </summary>
    public partial class AcreditacionManualOperationControl : UserControl
    {
        public AcreditacionManualOperationControl()
        {
            InitializeComponent();

            var viewmodel = new VMacreditacionManualOperations();

            DataContext = viewmodel;

#if DEBUG
            Debug.WriteLine($"[AcreditacionManual] DataContext set: {DataContext?.GetType().FullName ?? "null"}");
#endif
        }
    }
}
