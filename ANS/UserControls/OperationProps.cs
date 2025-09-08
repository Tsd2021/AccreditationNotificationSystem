// OperationProps.cs  (Build Action = Compile)
using System.Windows;

namespace ANS.UserControls
{
    public static class OperationProps
    {
        public static readonly DependencyProperty ShowHostFiltersProperty =
            DependencyProperty.RegisterAttached(
                "ShowHostFilters",
                typeof(bool),
                typeof(OperationProps),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetShowHostFilters(DependencyObject element, bool value) =>
            element.SetValue(ShowHostFiltersProperty, value);

        public static bool GetShowHostFilters(DependencyObject element) =>
            (bool)element.GetValue(ShowHostFiltersProperty);
    }
}
