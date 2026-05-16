using System;
using System.Globalization;
using System.Windows.Data;
using EditorDbf.App.Models;

namespace EditorDbf.App.Infrastructure;

public sealed class FilterDisplayConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not FilterParams p) return "Filtrar por valor";
        
        string paramStr = parameter as string ?? p.Operator;
        bool isCustom = paramStr.EndsWith(":CUSTOM");
        string op = isCustom ? paramStr.Replace(":CUSTOM", "") : paramStr;
        
        // Mapear operadores a representaciones textuales en español
        string opDisplay = op switch
        {
            "=" => "es igual a",
            "<>" => "es distinto que",
            ">" => "es mayor que",
            ">=" => "es mayor o igual que",
            "<" => "es menor que",
            "<=" => "es menor o igual que",
            "CONTIENE" => "contiene",
            "ENTRE" => "está entre",
            "VACIO" => "está vacío",
            "NO VACIO" => "no está vacío",
            _ => op
        };

        if (isCustom)
        {
            return $"{p.ColumnName} {opDisplay} ...";
        }

        string valDisplay = p.Value switch
        {
            null or DBNull => "NULL",
            string s => $"'{Truncate(s)}'",
            DateTime dt => $"'{dt:dd/MM/yyyy}'",
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            double db => db.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(),
            bool b => b ? ".T." : ".F.",
            _ => p.Value.ToString() ?? "NULL"
        };

        if (op == "CONTIENE")
        {
            // Para el texto del menú, no mostramos los % para que sea más natural
            valDisplay = $"'{Truncate(p.Value?.ToString())}'";
        }

        if (op is "VACIO" or "NO VACIO")
        {
            return $"{p.ColumnName} {opDisplay}";
        }

        return $"{p.ColumnName} {opDisplay} {valDisplay}";
    }

    private string Truncate(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim(); // Limpiar espacios de campos de ancho fijo
        if (s.Length > 20) return s.Substring(0, 17) + "...";
        return s;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
