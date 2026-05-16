using System;
using System.Globalization;
using System.Windows.Data;

namespace EditorDbf.App.Infrastructure;

public sealed class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null && value.Equals(parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null && value.Equals(true) ? parameter : Binding.DoNothing;
    }
}
