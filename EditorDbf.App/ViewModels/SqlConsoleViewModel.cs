using System;
using System.IO;
using System.Linq;
using System.Data;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EditorDbf.App.Infrastructure;
using EditorDbf.App.Services;
using EditorDbf.App.Models;

namespace EditorDbf.App.ViewModels;

public sealed class SqlConsoleViewModel : ObservableObject
{
    private readonly DbfSqlService _sqlService;
    private readonly string _folderPath;
    private readonly Action<SqlConsoleViewModel> _closeCallback;

    private string _sqlQuery = "SELECT * FROM ";
    private string _selectedText = string.Empty;
    private int _caretIndex;
    private bool _triggerFocus;
    private DataTable? _results;
    private string _statusMessage = "Escribe una consulta SQL y presiona Ejecutar.";
    private bool _isBusy;
    private ObservableCollection<SqlSchemaItem> _schemaItems = new();

    public SqlConsoleViewModel(DbfSqlService sqlService, string folderPath, Action<SqlConsoleViewModel> closeCallback)
    {
        _sqlService = sqlService;
        _folderPath = folderPath;
        _closeCallback = closeCallback;

        ExecuteCommand = new RelayCommand(async () => await ExecuteQueryAsync(), 
            () => (!string.IsNullOrWhiteSpace(SqlQuery) || !string.IsNullOrWhiteSpace(SelectedText)) && !IsBusy);
        CloseCommand = new RelayCommand(() => _closeCallback(this));
        InsertItemCommand = new RelayCommand<SqlSchemaItem>(InsertItem);

        _ = LoadSchemaAsync();
    }

    public string Header => "Consola SQL";

    public string SqlQuery
    {
        get => _sqlQuery;
        set
        {
            if (SetProperty(ref _sqlQuery, value))
            {
                (ExecuteCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedText
    {
        get => _selectedText;
        set
        {
            if (SetProperty(ref _selectedText, value))
            {
                (ExecuteCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public int CaretIndex
    {
        get => _caretIndex;
        set => SetProperty(ref _caretIndex, value);
    }

    public bool TriggerFocus
    {
        get => _triggerFocus;
        set => SetProperty(ref _triggerFocus, value);
    }

    public ObservableCollection<SqlSchemaItem> SchemaItems
    {
        get => _schemaItems;
        private set => SetProperty(ref _schemaItems, value);
    }

    public DataTable? Results
    {
        get => _results;
        private set => SetProperty(ref _results, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                (ExecuteCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand ExecuteCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand InsertItemCommand { get; }

    private void InsertItem(SqlSchemaItem? item)
    {
        if (item == null) return;
        
        // Buscar todos los ítems marcados en el esquema
        var checkedItems = SchemaItems
            .SelectMany(GetFlatList)
            .Where(i => i.IsChecked && i.Children.Count == 0) // Solo campos marcados, no tablas
            .ToList();

        string textToInsert;
        if (checkedItems.Any())
        {
            textToInsert = string.Join(", ", checkedItems.Select(i => i.SqlName));
            // Limpiar selección después de insertar
            foreach (var i in checkedItems) i.IsChecked = false;
            // También limpiar el check de las tablas padres si se marcaron
            foreach (var table in SchemaItems) table.IsChecked = false;
        }
        else
        {
            textToInsert = item.SqlName;
        }

        string currentText = SqlQuery ?? string.Empty;
        
        // Capturamos el índice actual localmente para que no se pierda durante la actualización del texto
        int indexToInsertAt = CaretIndex;
        
        // Validación de seguridad por si el texto cambió externamente
        if (indexToInsertAt > currentText.Length) 
            indexToInsertAt = currentText.Length;
        if (indexToInsertAt < 0)
            indexToInsertAt = 0;

        // Insertar el texto en la posición capturada
        SqlQuery = currentText.Insert(indexToInsertAt, textToInsert);
        
        // Actualizar el CaretIndex al final de la inserción para posicionar el cursor tras el texto insertado
        CaretIndex = indexToInsertAt + textToInsert.Length;
        
        // Disparamos el foco de vuelta al editor
        TriggerFocus = true;
        TriggerFocus = false;
    }

    private IEnumerable<SqlSchemaItem> GetFlatList(SqlSchemaItem item)
    {
        yield return item;
        foreach (var child in item.Children)
        {
            foreach (var flatChild in GetFlatList(child))
            {
                flatChild.Parent = item; // Aprovechamos para asignar el padre si falta
                yield return flatChild;
            }
        }
    }

    private async Task LoadSchemaAsync()
    {
        try
        {
            var dbfFiles = Directory.GetFiles(_folderPath, "*.dbf", SearchOption.TopDirectoryOnly);
            var items = new List<SqlSchemaItem>();

            foreach (var file in dbfFiles)
            {
                var tableName = Path.GetFileNameWithoutExtension(file);
                var tableItem = new SqlSchemaItem
                {
                    Name = Path.GetFileName(file),
                    SqlName = tableName,
                    Icon = "\uE8B7", // Icono de tabla
                    Details = "Tabla"
                };

                // Cargar campos
                var schema = await _sqlService.GetTableSchemaAsync(file);
                foreach (var field in schema)
                {
                    tableItem.Children.Add(new SqlSchemaItem
                    {
                        Name = field.Name,
                        SqlName = field.Name,
                        Icon = "\uE8EF", // Icono de columna
                        Details = $"{field.Type}({field.FieldLength}{(field.DecimalCount > 0 ? $",{field.DecimalCount}" : "")})"
                    });
                }

                items.Add(tableItem);
            }

            SchemaItems = new ObservableCollection<SqlSchemaItem>(items.OrderBy(i => i.Name));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error cargando esquema: {ex.Message}";
        }
    }

    private async Task ExecuteQueryAsync()
    {
        string queryToRun = !string.IsNullOrWhiteSpace(SelectedText) ? SelectedText : SqlQuery;
        
        if (string.IsNullOrWhiteSpace(queryToRun)) return;

        IsBusy = true;
        StatusMessage = "Ejecutando consulta...";
        Results = null;

        try
        {
            var result = await _sqlService.ExecuteAsync(_folderPath, queryToRun);
            StatusMessage = result.Message ?? "Consulta ejecutada.";
            Results = result.Results;

            if (result.IsSuccess && result.ModifiedTables.Count > 0)
            {
                StatusMessage += $" Tablas modificadas: {string.Join(", ", result.ModifiedTables)}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
