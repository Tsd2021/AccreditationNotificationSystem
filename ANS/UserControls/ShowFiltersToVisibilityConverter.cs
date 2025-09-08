using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ANS.UserControls
{
    public class ShowFiltersToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool show = true;

            if (value is DependencyObject d)
                show = OperationProps.GetShowHostFilters(d);

            return show ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
