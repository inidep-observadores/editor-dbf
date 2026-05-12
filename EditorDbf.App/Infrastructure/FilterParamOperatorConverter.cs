using System.Globalization;
using System.Windows.Data;
using EditorDbf.App.Models;

namespace EditorDbf.App.Infrastructure;

public sealed class FilterParamOperatorConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FilterParams p && parameter is string newOperator)
        {
            return p with { Operator = newOperator };
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
