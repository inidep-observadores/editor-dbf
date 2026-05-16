using System;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace EditorDbf.App.Infrastructure
{
    public class CellModifiedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return false;

            var rowView = values[0] as DataRowView;
            // El valor de values[1] es el contenido de la celda, usado para disparar el refresco del binding
            var columnName = values[2] as string;

            if (rowView == null || string.IsNullOrEmpty(columnName)) return false;

            var row = rowView.Row;
            
            // Si la fila no está en estado Modified, ninguna celda está resaltada como cambiada
            if (row.RowState != DataRowState.Modified) return false;

            try
            {
                // Comparar valor actual con el original
                var current = row[columnName];
                var original = row[columnName, DataRowVersion.Original];

                if (current == null && original == null) return false;
                if (current == null || original == null) return true;

                return !current.Equals(original);
            }
            catch
            {
                // Puede fallar si la columna no existe o no tiene versión original (aunque RowState == Modified debería garantizarla)
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
