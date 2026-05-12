using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using EditorDbf.App.Infrastructure;
using EditorDbf.App.Models;
using EditorDbf.App.Services;
using EditorDbf.App.Views;

namespace EditorDbf.App.ViewModels;

public sealed class TableTabViewModel : ObservableObject
{
    private static readonly string[] FilterOperatorsCatalog = ["=", "<>", ">", "<", "CONTIENE", "VACIO", "NO VACIO"];

    private DbfTableDocument _document;
    private DataRowView? _selectedRecord;
    private bool _hasPendingChanges;
    private string? _selectedFilterColumn;
    private string _selectedFilterOperator = "=";
    private string _filterValue = string.Empty;
    private string _currentFilterText = string.Empty;
    private string _sqlFilter = string.Empty;
    private readonly List<DataRowView> _selectedRecords = [];

    public TableTabViewModel(
        DbfTableDocument document,
        IReadOnlyList<DbfFieldDescriptor> structure,
        Action<TableTabViewModel> closeAction)
    {
        _document = document;

        TableStructure = new ObservableCollection<DbfFieldDescriptor>(structure);
        FilterColumns = new ObservableCollection<string>(document.DataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        FilterOperators = new ObservableCollection<string>(FilterOperatorsCatalog);
        SelectedFilterColumn = FilterColumns.FirstOrDefault();

        CloseCommand = new RelayCommand(() => closeAction(this));
        ApplyFilterCommand = new RelayCommand(ApplyFilter, CanApplyFilter);
        ClearFilterCommand = new RelayCommand(ClearFilter, () => IsFilterActive);
        ExportCommand = new RelayCommand(ExportTable);
        
        // Nuevos comandos para filtrado avanzado
        ApplySqlFilterCommand = new RelayCommand(ApplySqlFilter);
        FilterByValueCommand = new RelayCommand<object>(FilterByValue);
        FilterCustomCommand = new RelayCommand(() => FilterCustom(LastRightClickedCellInfo));
        ChangeValueCommand = new RelayCommand(ChangeValue);

        SubscribeToDataTable();
        UpdateRecordCounts();
    }

    public string FilePath => _document.FilePath;

    public string FileName => Path.GetFileName(_document.FilePath);

    public string Header => HasPendingChanges ? $"{FileName} *" : FileName;

    public DbfTableDocument Document => _document;

    public DbfHeaderInfo HeaderInfo => _document.HeaderInfo;

    public ObservableCollection<DbfFieldDescriptor> TableStructure { get; }

    public ObservableCollection<string> FilterColumns { get; }

    public ObservableCollection<string> FilterOperators { get; }

    public ICommand CloseCommand { get; }

    public ICommand ApplyFilterCommand { get; }

    public ICommand ClearFilterCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ApplySqlFilterCommand { get; }
    public ICommand FilterByValueCommand { get; }
    public ICommand FilterCustomCommand { get; }
    public ICommand ChangeValueCommand { get; }

    public DataView CurrentTableView => _document.DataTable.DefaultView;

    public DataRowView? SelectedRecord
    {
        get => _selectedRecord;
        set
        {
            if (SetProperty(ref _selectedRecord, value))
            {
                OnPropertyChanged(nameof(HasSelectedRecords));
            }
        }
    }

    public int SelectedRecordsCount => _selectedRecords.Count;

    public bool HasSelectedRecords => SelectedRecordsCount > 0 || SelectedRecord is not null;

    public string? SelectedFilterColumn
    {
        get => _selectedFilterColumn;
        set
        {
            if (SetProperty(ref _selectedFilterColumn, value))
            {
                NotifyCommands();
            }
        }
    }

    public string SelectedFilterOperator
    {
        get => _selectedFilterOperator;
        set
        {
            if (SetProperty(ref _selectedFilterOperator, value))
            {
                NotifyCommands();
            }
        }
    }

    public string FilterValue
    {
        get => _filterValue;
        set
        {
            if (SetProperty(ref _filterValue, value))
            {
                NotifyCommands();
            }
        }
    }

    public string CurrentFilterText
    {
        get => _currentFilterText;
        private set
        {
            if (SetProperty(ref _currentFilterText, value))
                OnPropertyChanged(nameof(IsFilterActive));
        }
    }

    public bool IsFilterActive => !string.IsNullOrWhiteSpace(_document.DataTable.DefaultView.RowFilter);

    public string SqlFilter
    {
        get => _sqlFilter;
        set => SetProperty(ref _sqlFilter, value);
    }

    public FilterParams? LastRightClickedCellInfo
    {
        get => _lastRightClickedCellInfo;
        set => SetProperty(ref _lastRightClickedCellInfo, value);
    }
    private FilterParams? _lastRightClickedCellInfo;

    public int TotalRecords => _document.DataTable.Rows.Count;
    
    public int FilteredRecords => CurrentTableView.Count;

    public bool HasPendingChanges
    {
        get => _hasPendingChanges;
        private set
        {
            if (SetProperty(ref _hasPendingChanges, value))
            {
                OnPropertyChanged(nameof(Header));
            }
        }
    }

    public void AddRecord()
    {
        var row = _document.DataTable.NewRow();
        _document.DataTable.Rows.Add(row);
        HasPendingChanges = true;
    }

    public int DeleteSelectedRecords()
    {
        if (_selectedRecords.Count > 0)
        {
            var deleted = 0;
            var distinctRows = _selectedRecords
                .DistinctBy(record => record.Row)
                .ToList();

            foreach (var record in distinctRows)
            {
                if (record.Row.RowState != DataRowState.Deleted)
                {
                    record.Delete();
                    deleted++;
                }
            }

            _selectedRecords.Clear();
            OnPropertyChanged(nameof(SelectedRecordsCount));
            OnPropertyChanged(nameof(HasSelectedRecords));
            HasPendingChanges = deleted > 0 || HasPendingChanges;
            return deleted;
        }

        if (SelectedRecord is null)
        {
            return 0;
        }

        SelectedRecord.Delete();
        SelectedRecord = null;
        HasPendingChanges = true;
        return 1;
    }

    public void AppendRowsFrom(DataTable sourceTable)
    {
        foreach (DataRow sourceRow in sourceTable.Rows)
        {
            if (sourceRow.RowState == DataRowState.Deleted)
            {
                continue;
            }

            var targetRow = _document.DataTable.NewRow();
            for (var columnIndex = 0; columnIndex < _document.DataTable.Columns.Count; columnIndex++)
            {
                targetRow[columnIndex] = sourceRow[columnIndex];
            }

            _document.DataTable.Rows.Add(targetRow);
        }
    }

    public void ReplaceDocument(DbfTableDocument document, IReadOnlyList<DbfFieldDescriptor> structure)
    {
        UnsubscribeFromDataTable();
        _document = document;

        TableStructure.Clear();
        foreach (var field in structure)
        {
            TableStructure.Add(field);
        }

        FilterColumns.Clear();
        foreach (DataColumn column in _document.DataTable.Columns)
        {
            FilterColumns.Add(column.ColumnName);
        }

        SelectedFilterColumn = FilterColumns.FirstOrDefault();
        SelectedFilterOperator = "=";
        FilterValue = string.Empty;
        CurrentFilterText = "(sin filtro)";
        _document.DataTable.DefaultView.RowFilter = string.Empty;
        SelectedRecord = null;
        _selectedRecords.Clear();
        OnPropertyChanged(nameof(SelectedRecordsCount));
        OnPropertyChanged(nameof(HasSelectedRecords));

        HasPendingChanges = false;
        SubscribeToDataTable();
        OnPropertyChanged(nameof(CurrentTableView));
        OnPropertyChanged(nameof(HeaderInfo));
        NotifyCommands();
    }

    public void MarkSaved()
    {
        _document.DataTable.AcceptChanges();
        HasPendingChanges = false;
    }

    public void ExportTable()
    {
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(FileName);
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv|Excel file (*.xlsx)|*.xlsx|DBF file (*.dbf)|*.dbf",
            FileName = $"{fileNameWithoutExt}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            var path = dialog.FileName;
            var ext = Path.GetExtension(path).ToLower();
            
            var service = new EditorDbf.App.Services.DbfTableService();
            service.ExportTable(_document, CurrentTableView, path, ext);
        }
    }

    public void Dispose()
    {
        UnsubscribeFromDataTable();
    }

    public void UpdateSelectedRecords(IEnumerable<DataRowView> selectedRecords)
    {
        _selectedRecords.Clear();
        _selectedRecords.AddRange(selectedRecords);
        OnPropertyChanged(nameof(SelectedRecordsCount));
        OnPropertyChanged(nameof(HasSelectedRecords));
    }

    private bool CanApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilterColumn))
        {
            return false;
        }

        return !OperatorNeedsValue(SelectedFilterOperator) || !string.IsNullOrWhiteSpace(FilterValue);
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilterColumn)) return;

        var column = _document.DataTable.Columns[SelectedFilterColumn];
        if (column is null) return;

        var expression = BuildFilterExpression(column, SelectedFilterOperator, FilterValue);
        CombineAndApplyFilter(expression);
    }

    private void ApplyFilter(FilterParams p)
    {
        var column = _document.DataTable.Columns[p.ColumnName];
        if (column == null) return;

        var expression = BuildFilterExpression(column, p.Operator, p.Value?.ToString() ?? string.Empty);
        CombineAndApplyFilter(expression);
    }

    private void CombineAndApplyFilter(string newCondition)
    {
        var currentFilter = _document.DataTable.DefaultView.RowFilter;
        if (string.IsNullOrWhiteSpace(currentFilter))
        {
            ApplyFilterExpression(newCondition);
        }
        else
        {
            ApplyFilterExpression($"({currentFilter}) AND ({newCondition})");
        }
    }

    private void ApplySqlFilter()
    {
        ApplyFilterExpression(SqlFilter);
    }

    private void ChangeValue()
    {
        if (LastRightClickedCellInfo == null) return;

        var columnName = LastRightClickedCellInfo.ColumnName;
        var column = _document.DataTable.Columns[columnName];
        if (column == null) return;

        var currentValue = LastRightClickedCellInfo.Value?.ToString() ?? string.Empty;

        var newValue = InputDialog.Show(
            $"Ingrese el nuevo valor para la columna [{columnName}]:",
            "Cambiar Valor",
            currentValue);

        if (newValue == null) return;

        // Determinar qué registros actualizar
        var recordsToUpdate = new List<DataRowView>();
        if (_selectedRecords.Count > 1)
        {
            recordsToUpdate.AddRange(_selectedRecords);
        }
        else if (SelectedRecord != null)
        {
            recordsToUpdate.Add(SelectedRecord);
        }

        if (recordsToUpdate.Count == 0) return;

        // Pedir confirmación
        var count = recordsToUpdate.Count;
        var confirmMsg = count > 1 
            ? $"¿Está seguro de que desea cambiar el valor de [{columnName}] a '{newValue}' en los {count} registros seleccionados?"
            : $"¿Está seguro de que desea cambiar el valor de [{columnName}] a '{newValue}'?";

        var result = System.Windows.MessageBox.Show(confirmMsg, "Confirmar Cambio", 
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var typedValue = ConvertValue(newValue, column.DataType);
            
            foreach (var rowView in recordsToUpdate)
            {
                rowView.Row[columnName] = typedValue ?? DBNull.Value;
            }
            
            HasPendingChanges = true;
            // Forzar refresco de la vista si es necesario (aunque el binding debería detectarlo)
            NotifyCommands();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al cambiar valor: {ex.Message}", "Error de Edición", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private object? ConvertValue(string value, Type targetType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (targetType == typeof(string)) return string.Empty;
            return null;
        }

        if (targetType == typeof(bool))
        {
            var s = value.Trim().ToUpper();
            return s is "T" or "Y" or "1" or "TRUE" or "S";
        }

        if (targetType == typeof(DateTime))
        {
            if (DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
            return DateTime.Parse(value);
        }

        if (targetType == typeof(decimal) || targetType == typeof(double))
        {
            return Convert.ChangeType(value.Replace(',', '.'), targetType, CultureInfo.InvariantCulture);
        }

        return Convert.ChangeType(value, targetType);
    }

    private void ApplyFilterExpression(string expression)
    {
        try
        {
            _document.DataTable.DefaultView.RowFilter = expression;
            SqlFilter = expression;
            CurrentFilterText = string.IsNullOrWhiteSpace(expression) ? string.Empty : expression;
            UpdateRecordCounts();
            NotifyCommands();
        }
        catch (Exception ex)
        {
            // Podríamos mostrar un mensaje de error si la expresión SQL es inválida
            System.Windows.MessageBox.Show($"Filtro inválido: {ex.Message}", "Error de Filtro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void FilterCustom(FilterParams? p)
    {
        if (p == null) return;

        var newValue = InputDialog.Show(
            $"Ingrese valor para el filtro [{p.ColumnName}] {p.Operator}:",
            "Filtro Personalizado",
            p.Value?.ToString() ?? string.Empty);

        if (newValue != null)
        {
            ApplyFilter(p with { Value = newValue });
        }
    }

    private void FilterByValue(object? parameter)
    {
        if (parameter is not FilterParams p) return;
        ApplyFilter(p);
    }

    private void UpdateRecordCounts()
    {
        OnPropertyChanged(nameof(TotalRecords));
        OnPropertyChanged(nameof(FilteredRecords));
    }

    private void ClearFilter()
    {
        ApplyFilterExpression(string.Empty);
        FilterValue = string.Empty;
    }

    private void SubscribeToDataTable()
    {
        _document.DataTable.RowChanged += OnTableRowChanged;
        _document.DataTable.RowDeleted += OnTableRowDeleted;
        _document.DataTable.TableNewRow += OnTableNewRow;
    }

    private void UnsubscribeFromDataTable()
    {
        _document.DataTable.RowChanged -= OnTableRowChanged;
        _document.DataTable.RowDeleted -= OnTableRowDeleted;
        _document.DataTable.TableNewRow -= OnTableNewRow;
    }

    private void OnTableRowChanged(object? sender, DataRowChangeEventArgs e)
    {
        if (e.Action is DataRowAction.Change or DataRowAction.Add or DataRowAction.Delete)
        {
            HasPendingChanges = true;
        }
    }

    private void OnTableRowDeleted(object? sender, DataRowChangeEventArgs e)
    {
        HasPendingChanges = true;
    }

    private void OnTableNewRow(object? sender, DataTableNewRowEventArgs e)
    {
        HasPendingChanges = true;
    }

    private void NotifyCommands()
    {
        (ApplyFilterCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ClearFilterCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private static bool OperatorNeedsValue(string filterOperator)
    {
        return filterOperator is "=" or "<>" or ">" or "<" or "CONTIENE";
    }

    private static string BuildFilterExpression(DataColumn column, string filterOperator, string rawValue)
    {
        var safeColumn = $"[{EscapeColumnName(column.ColumnName)}]";
        var value = rawValue ?? string.Empty;

        return filterOperator switch
        {
            "VACIO" => column.DataType == typeof(string)
                ? $"{safeColumn} IS NULL OR {safeColumn} = ''"
                : $"{safeColumn} IS NULL",
            "NO VACIO" => column.DataType == typeof(string)
                ? $"{safeColumn} IS NOT NULL AND {safeColumn} <> ''"
                : $"{safeColumn} IS NOT NULL",
            "CONTIENE" => $"Convert({safeColumn}, 'System.String') LIKE '%{EscapeStringLiteral(value)}%'",
            "=" or "<>" or ">" or "<" => $"{safeColumn} {filterOperator} {BuildLiteral(column.DataType, value)}",
            _ => string.Empty
        };
    }

    private static string BuildLiteral(Type dataType, string value)
    {
        if (dataType == typeof(string))
        {
            return $"'{EscapeStringLiteral(value)}'";
        }

        if (dataType == typeof(bool))
        {
            return bool.TryParse(value, out var boolValue)
                ? (boolValue ? "TRUE" : "FALSE")
                : throw new InvalidOperationException("Los valores booleanos deben ser true o false.");
        }

        if (dataType == typeof(DateTime))
        {
            if (!DateTime.TryParse(value, out var dateValue))
            {
                throw new InvalidOperationException("Las fechas deben ser validas (ejemplo: 2026-05-08).");
            }

            return $"#{dateValue:MM/dd/yyyy HH:mm:ss}#";
        }

        if (dataType == typeof(int))
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue)
                ? intValue.ToString(CultureInfo.InvariantCulture)
                : throw new InvalidOperationException("Se esperaba un numero entero.");
        }

        if (dataType == typeof(decimal))
        {
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue)
                ? decimalValue.ToString(CultureInfo.InvariantCulture)
                : throw new InvalidOperationException("Se esperaba un valor decimal. Usa punto como separador.");
        }

        if (dataType == typeof(double))
        {
            return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue)
                ? doubleValue.ToString(CultureInfo.InvariantCulture)
                : throw new InvalidOperationException("Se esperaba un valor numerico. Usa punto como separador.");
        }

        return $"'{EscapeStringLiteral(value)}'";
    }

    private static string EscapeColumnName(string input)
    {
        return input.Replace("]", "]]", StringComparison.Ordinal);
    }

    private static string EscapeStringLiteral(string input)
    {
        return input.Replace("'", "''", StringComparison.Ordinal);
    }
}
