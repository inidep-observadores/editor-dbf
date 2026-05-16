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
    private static readonly string[] FilterOperatorsCatalog = ["=", "<>", ">", ">=", "<", "<=", "CONTIENE", "VACIO", "NO VACIO"];

    private DbfTableDocument _document;
    private DataRowView? _selectedRecord;
    private bool _hasPendingChanges;
    private string? _selectedFilterColumn;
    private string _selectedFilterOperator = "=";
    private string _filterValue = string.Empty;
    private string _currentFilterText = string.Empty;
    private string _sqlFilter = string.Empty;
    private readonly List<DataRowView> _selectedRecords = [];
    public Action? RequestRefreshFiles { get; set; }

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
        FilterCustomCommand = new RelayCommand<object>(p => FilterCustom(p as FilterParams ?? LastRightClickedCellInfo));
        FilterBetweenCommand = new RelayCommand<object>(p => FilterBetween(p as FilterParams ?? LastRightClickedCellInfo));
        ChangeValueCommand = new RelayCommand(ChangeValue);
        ChangeDatePartCommand = new RelayCommand<string>(ChangeDatePart);
        InvertSelectionCommand = new RelayCommand<object>(InvertSelection);

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
    public ICommand FilterBetweenCommand { get; }
    public ICommand ChangeValueCommand { get; }
    public ICommand ChangeDatePartCommand { get; }
    public ICommand InvertSelectionCommand { get; }

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
        set
        {
            if (SetProperty(ref _lastRightClickedCellInfo, value))
            {
                OnPropertyChanged(nameof(IsDateColumn));
            }
        }
    }
    private FilterParams? _lastRightClickedCellInfo;

    public bool IsDateColumn
    {
        get
        {
            if (LastRightClickedCellInfo == null) return false;
            var column = _document.DataTable.Columns[LastRightClickedCellInfo.ColumnName];
            return column?.DataType == typeof(DateTime);
        }
    }

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
        var hasSelection = _selectedRecords.Count > 0;
        var viewModel = new ExportViewModel(FilePath, hasSelection);
        var dialog = new ExportView(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            var path = viewModel.DestinationPath;
            var ext = Path.GetExtension(path).ToLower();
            
            var service = new EditorDbf.App.Services.DbfTableService();

            try
            {
                if (viewModel.Scope == ExportScope.Selected)
                {
                    // Exportar solo filas seleccionadas
                    var tempTable = _document.DataTable.Clone();
                    foreach (var rowView in _selectedRecords)
                    {
                        if (rowView.Row.RowState != DataRowState.Deleted)
                        {
                            tempTable.ImportRow(rowView.Row);
                        }
                    }
                    service.ExportTable(_document, tempTable.DefaultView, path, ext);
                }
                else
                {
                    // Exportar vista filtrada (CurrentTableView)
                    service.ExportTable(_document, CurrentTableView, path, ext);
                }

                System.Windows.MessageBox.Show($"Exportación completada con éxito en:\n{path}",
                    "Exportar", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                RequestRefreshFiles?.Invoke();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar: {ex.Message}", "Error de Exportación",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
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

    private void ChangeDatePart(string? part)
    {
        if (string.IsNullOrEmpty(part) || LastRightClickedCellInfo == null) return;

        var columnName = LastRightClickedCellInfo.ColumnName;
        var column = _document.DataTable.Columns[columnName];
        if (column == null || column.DataType != typeof(DateTime)) return;

        var partName = part.ToLower() switch
        {
            "day" => "día",
            "month" => "mes",
            "year" => "año",
            _ => part
        };

        var newValueStr = InputDialog.Show(
            $"Ingrese el nuevo valor para el {partName} de la columna [{columnName}]:",
            $"Cambiar {partName}",
            string.Empty);

        if (string.IsNullOrWhiteSpace(newValueStr) || !int.TryParse(newValueStr, out var newValue)) return;

        // Validar rangos básicos
        if (part.Equals("Day", StringComparison.OrdinalIgnoreCase) && (newValue < 1 || newValue > 31))
        {
            System.Windows.MessageBox.Show("El día debe estar entre 1 y 31.", "Valor Inválido", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        if (part.Equals("Month", StringComparison.OrdinalIgnoreCase) && (newValue < 1 || newValue > 12))
        {
            System.Windows.MessageBox.Show("El mes debe estar entre 1 y 12.", "Valor Inválido", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        if (part.Equals("Year", StringComparison.OrdinalIgnoreCase) && (newValue < 1000 || newValue > 9999))
        {
            System.Windows.MessageBox.Show("El año debe ser un valor de 4 dígitos (1000-9999).", "Valor Inválido", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

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
            ? $"¿Está seguro de que desea cambiar el {partName} a '{newValue}' en los {count} registros seleccionados?"
            : $"¿Está seguro de que desea cambiar el {partName} a '{newValue}'?";

        var result = System.Windows.MessageBox.Show(confirmMsg, "Confirmar Cambio",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        var errors = 0;
        try
        {
            foreach (var rowView in recordsToUpdate)
            {
                var currentVal = rowView.Row[columnName];
                if (currentVal == DBNull.Value || currentVal is not DateTime current) continue;

                try
                {
                    DateTime updated;
                    if (part.Equals("Day", StringComparison.OrdinalIgnoreCase))
                    {
                        updated = new DateTime(current.Year, current.Month, newValue, current.Hour, current.Minute, current.Second);
                    }
                    else if (part.Equals("Month", StringComparison.OrdinalIgnoreCase))
                    {
                        updated = new DateTime(current.Year, newValue, current.Day, current.Hour, current.Minute, current.Second);
                    }
                    else if (part.Equals("Year", StringComparison.OrdinalIgnoreCase))
                    {
                        updated = new DateTime(newValue, current.Month, current.Day, current.Hour, current.Minute, current.Second);
                    }
                    else continue;

                    rowView.Row[columnName] = updated;
                }
                catch
                {
                    errors++;
                }
            }

            HasPendingChanges = true;
            NotifyCommands();

            if (errors > 0)
            {
                System.Windows.MessageBox.Show($"Se completó la operación, pero {errors} registros no pudieron ser actualizados debido a que la fecha resultante sería inválida (ej. 31 de febrero).",
                    "Aviso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al cambiar parte de la fecha: {ex.Message}", "Error de Edición",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void InvertSelection(object? parameter)
    {
        if (parameter is System.Windows.Controls.DataGrid dataGrid)
        {
            var selectedItems = dataGrid.SelectedItems.Cast<object>().ToList();
            dataGrid.UnselectAll();
            foreach (var item in dataGrid.Items)
            {
                if (!selectedItems.Contains(item))
                {
                    dataGrid.SelectedItems.Add(item);
                }
            }
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

        string opDisplay = p.Operator switch
        {
            "=" => "es igual a",
            "<>" => "es distinto que",
            ">" => "es mayor que",
            ">=" => "es mayor o igual que",
            "<" => "es menor que",
            "<=" => "es menor o igual que",
            "CONTIENE" => "contiene",
            _ => p.Operator
        };

        var newValue = InputDialog.Show(
            $"Ingrese valor para el filtro [{p.ColumnName}] {opDisplay}:",
            "Filtro Personalizado",
            p.Value?.ToString() ?? string.Empty);

        if (newValue != null)
        {
            ApplyFilter(p with { Value = newValue });
        }
    }

    private void FilterBetween(FilterParams? p)
    {
        if (p == null) return;

        var val1 = InputDialog.Show(
            $"Ingrese el valor MÍNIMO para el rango de [{p.ColumnName}]:",
            "Filtro Entre (Límite Inferior)",
            string.Empty);

        if (val1 == null) return;

        var val2 = InputDialog.Show(
            $"Ingrese el valor MÁXIMO para el rango de [{p.ColumnName}]:",
            "Filtro Entre (Límite Superior)",
            string.Empty);

        if (val2 == null) return;

        var column = _document.DataTable.Columns[p.ColumnName];
        if (column == null) return;

        try
        {
            var expr1 = BuildFilterExpression(column, ">=", val1);
            var expr2 = BuildFilterExpression(column, "<=", val2);
            CombineAndApplyFilter($"({expr1}) AND ({expr2})");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Valores inválidos para el rango: {ex.Message}", "Error de Filtro", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
        return filterOperator is "=" or "<>" or ">" or ">=" or "<" or "<=" or "CONTIENE";
    }

    private static string BuildFilterExpression(DataColumn column, string filterOperator, string rawValue)
    {
        var safeColumn = $"[{EscapeColumnName(column.ColumnName)}]";
        var value = rawValue ?? string.Empty;

        // Manejo especial para fechas para ignorar la parte de la hora en comparaciones comunes
        if (column.DataType == typeof(DateTime) && filterOperator is "=" or "<>" or ">" or ">=" or "<" or "<=")
        {
            if (!DateTime.TryParse(value, out var dateValue))
                throw new InvalidOperationException("Las fechas deben ser válidas (ejemplo: 2026-05-08).");
            
            var dateOnly = dateValue.Date;
            var nextDay = dateOnly.AddDays(1);

            return filterOperator switch
            {
                "=" => $"({safeColumn} >= #{dateOnly:MM/dd/yyyy}# AND {safeColumn} < #{nextDay:MM/dd/yyyy}#)",
                "<>" => $"({safeColumn} < #{dateOnly:MM/dd/yyyy}# OR {safeColumn} >= #{nextDay:MM/dd/yyyy}#)",
                ">" => $"{safeColumn} >= #{nextDay:MM/dd/yyyy}#",
                ">=" => $"{safeColumn} >= #{dateOnly:MM/dd/yyyy}#",
                "<" => $"{safeColumn} < #{dateOnly:MM/dd/yyyy}#",
                "<=" => $"{safeColumn} < #{nextDay:MM/dd/yyyy}#",
                _ => string.Empty
            };
        }

        return filterOperator switch
        {
            "VACIO" => column.DataType == typeof(string)
                ? $"{safeColumn} IS NULL OR {safeColumn} = ''"
                : $"{safeColumn} IS NULL",
            "NO VACIO" => column.DataType == typeof(string)
                ? $"{safeColumn} IS NOT NULL AND {safeColumn} <> ''"
                : $"{safeColumn} IS NOT NULL",
            "CONTIENE" => column.DataType == typeof(string)
                ? $"{safeColumn} LIKE '%{EscapeStringLiteral(value)}%'"
                : $"Convert({safeColumn}, 'System.String') LIKE '%{EscapeStringLiteral(value)}%'",
            "=" or "<>" or ">" or ">=" or "<" or "<=" => $"{safeColumn} {filterOperator} {BuildLiteral(column.DataType, value)}",
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
