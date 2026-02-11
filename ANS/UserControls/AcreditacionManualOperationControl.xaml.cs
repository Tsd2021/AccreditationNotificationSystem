using ANS.ViewModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

        private void DepositoCheckBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var uc = sender as DependencyObject;
            while (uc != null)
            {
                if (uc is AcreditacionManualOperationControl control)
                {
                    (control.DataContext as VMacreditacionManualOperations)?.NotifyDepositoSelectionChanged();
                    return;
                }
                uc = VisualTreeHelper.GetParent(uc);
            }
        }
    }
}
