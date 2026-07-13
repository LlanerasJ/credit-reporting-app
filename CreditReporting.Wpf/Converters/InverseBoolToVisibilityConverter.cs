using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CreditReporting.Wpf.Converters;

/// <summary>False → Visible, true → Collapsed (the opposite of BooleanToVisibilityConverter).</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
