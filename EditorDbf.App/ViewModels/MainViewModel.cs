using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EditorDbf.App.Infrastructure;
using EditorDbf.App.Models;
using EditorDbf.App.Services;
using Microsoft.Win32;

namespace EditorDbf.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ConnectionRepository _connectionRepository;
    private readonly DbfTableService _dbfTableService;
    private readonly AppState _state;

    private ConnectionProfile? _selectedConnection;
    private string? _selectedDbfFile;
    private TableTabViewModel? _selectedOpenTable;
    private string _statusMessage = "Listo.";
    private bool _isDarkTheme;

    public MainViewModel(ConnectionRepository connectionRepository, DbfTableService dbfTableService)
    {
        _connectionRepository = connectionRepository;
        _dbfTableService = dbfTableService;
        _state = _connectionRepository.Load();

        Connections = new ObservableCollection<ConnectionProfile>(_state.Connections);
        DbfFiles = new ObservableCollection<string>();
        OpenTables = new ObservableCollection<TableTabViewModel>();

        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        AddConnectionCommand = new RelayCommand(AddConnection);
        RemoveConnectionCommand = new RelayCommand(RemoveConnection, () => SelectedConnection is not null);
        RefreshFilesCommand = new RelayCommand(RefreshFiles, () => SelectedConnection is not null);
        OpenTableCommand = new RelayCommand(OpenSelectedTable, () => SelectedConnection is not null && !string.IsNullOrWhiteSpace(SelectedDbfFile));
        AddRecordCommand = new RelayCommand(AddRecord, () => SelectedOpenTable is not null);
        DeleteSelectedRecordCommand = new RelayCommand(DeleteSelectedRecord, () => SelectedOpenTable?.HasSelectedRecords == true);
        ReloadTableCommand = new RelayCommand(ReloadCurrentTable, () => SelectedOpenTable is not null);
        SaveTableCommand = new RelayCommand(SaveCurrentTable, () => SelectedOpenTable is not null);
        ImportFromTableCommand = new RelayCommand(ImportFromTable, () => SelectedOpenTable is not null);
        CopyStructureCommand = new RelayCommand(CopyStructureToClipboard, () => SelectedOpenTable is not null);
        CloseSelectedTabCommand = new RelayCommand(CloseSelectedTab, () => SelectedOpenTable is not null);

        TryRestoreLastConnection();
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set => SetProperty(ref _isDarkTheme, value);
    }

    public ICommand ToggleThemeCommand { get; }

    public ObservableCollection<ConnectionProfile> Connections { get; }

    public ObservableCollection<string> DbfFiles { get; }

    public ObservableCollection<TableTabViewModel> OpenTables { get; }

    public ICommand AddConnectionCommand { get; }

    public ICommand RemoveConnectionCommand { get; }

    public ICommand RefreshFilesCommand { get; }

    public ICommand OpenTableCommand { get; }

    public ICommand AddRecordCommand { get; }

    public ICommand DeleteSelectedRecordCommand { get; }

    public ICommand ReloadTableCommand { get; }

    public ICommand SaveTableCommand { get; }

    public ICommand ImportFromTableCommand { get; }

    public ICommand CopyStructureCommand { get; }

    public ICommand CloseSelectedTabCommand { get; }

    public ConnectionProfile? SelectedConnection
    {
        get => _selectedConnection;
        set
        {
            if (!SetProperty(ref _selectedConnection, value))
            {
                return;
            }

            _state.LastConnectionId = value?.Id;
            PersistState();
            RefreshFiles();
            NotifyCommands();
        }
    }

    public string? SelectedDbfFile
    {
        get => _selectedDbfFile;
        set
        {
            if (SetProperty(ref _selectedDbfFile, value))
            {
                NotifyCommands();
            }
        }
    }

    public TableTabViewModel? SelectedOpenTable
    {
        get => _selectedOpenTable;
        set
        {
            if (_selectedOpenTable == value)
            {
                return;
            }

            if (_selectedOpenTable is not null)
            {
                _selectedOpenTable.PropertyChanged -= OnSelectedOpenTablePropertyChanged;
            }

            _selectedOpenTable = value;
            OnPropertyChanged();

            if (_selectedOpenTable is not null)
            {
                _selectedOpenTable.PropertyChanged += OnSelectedOpenTablePropertyChanged;
            }

            OnPropertyChanged(nameof(CurrentTableDisplayName));
            OnPropertyChanged(nameof(DirtyStatus));
            OnPropertyChanged(nameof(CurrentTableStructure));
            NotifyCommands();
        }
    }

    public string CurrentTableDisplayName => SelectedOpenTable is null ? "(ninguna)" : SelectedOpenTable.FileName;

    public string DirtyStatus => SelectedOpenTable?.HasPendingChanges == true ? "Cambios pendientes" : "Sin cambios";

    public IReadOnlyList<DbfFieldDescriptor> CurrentTableStructure => SelectedOpenTable?.TableStructure ?? [];

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    private void AddConnection()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Seleccionar carpeta con archivos DBF",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var folderPath = dialog.FolderName.Trim();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        var existing = Connections.FirstOrDefault(connection =>
            string.Equals(connection.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            SelectedConnection = existing;
            StatusMessage = "La conexion ya existe y fue seleccionada.";
            return;
        }

        var connection = new ConnectionProfile
        {
            Name = Path.GetFileName(folderPath),
            FolderPath = folderPath
        };

        Connections.Add(connection);
        _state.Connections = [.. Connections];
        PersistState();

        SelectedConnection = connection;
        StatusMessage = $"Conexion creada: {connection.DisplayName}";
    }

    private void RemoveConnection()
    {
        if (SelectedConnection is null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"¿Eliminar la conexion '{SelectedConnection.DisplayName}'?",
            "Confirmar eliminacion de conexion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var connectionToRemove = SelectedConnection;
        Connections.Remove(connectionToRemove);

        _state.Connections = [.. Connections];
        if (_state.LastConnectionId == connectionToRemove.Id)
        {
            _state.LastConnectionId = Connections.FirstOrDefault()?.Id;
        }

        PersistState();
        SelectedConnection = Connections.FirstOrDefault();
        StatusMessage = "Conexion eliminada.";
    }

    private void RefreshFiles()
    {
        DbfFiles.Clear();
        SelectedDbfFile = null;

        if (SelectedConnection is null)
        {
            StatusMessage = "Selecciona una conexion para listar archivos DBF.";
            return;
        }

        foreach (var file in _dbfTableService.ListDbfFiles(SelectedConnection.FolderPath))
        {
            DbfFiles.Add(file);
        }

        StatusMessage = DbfFiles.Count == 0
            ? "No se encontraron archivos DBF en la carpeta seleccionada."
            : $"Se encontraron {DbfFiles.Count} archivos DBF.";

        NotifyCommands();
    }

    private void OpenSelectedTable()
    {
        if (SelectedConnection is null || string.IsNullOrWhiteSpace(SelectedDbfFile))
        {
            return;
        }

        var filePath = Path.Combine(SelectedConnection.FolderPath, SelectedDbfFile);
        var existingTab = OpenTables.FirstOrDefault(tab =>
            string.Equals(tab.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (existingTab is not null)
        {
            SelectedOpenTable = existingTab;
            StatusMessage = $"Pestana enfocada: {existingTab.FileName}";
            return;
        }

        try
        {
            var document = _dbfTableService.LoadTable(filePath);
            var structure = _dbfTableService.DescribeFields(document.Fields);
            var tab = new TableTabViewModel(document, structure, CloseTab);

            OpenTables.Add(tab);
            SelectedOpenTable = tab;
            StatusMessage = $"Tabla abierta: {Path.GetFileName(filePath)} ({document.DataTable.Rows.Count} registros).";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Error al abrir: {exception.Message}";
            MessageBox.Show(exception.Message, "Error al abrir DBF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddRecord()
    {
        if (SelectedOpenTable is null)
        {
            return;
        }

        SelectedOpenTable.AddRecord();
        StatusMessage = "Registro agregado.";
        NotifyCommands();
        OnPropertyChanged(nameof(DirtyStatus));
    }

    private void DeleteSelectedRecord()
    {
        if (SelectedOpenTable is null)
        {
            return;
        }

        var deleted = SelectedOpenTable.DeleteSelectedRecords();
        if (deleted > 0)
        {
            StatusMessage = deleted == 1 ? "1 registro marcado para borrar." : $"{deleted} registros marcados para borrar.";
            NotifyCommands();
            OnPropertyChanged(nameof(DirtyStatus));
        }
    }

    private void ReloadCurrentTable()
    {
        if (SelectedOpenTable is null)
        {
            return;
        }

        if (SelectedOpenTable.HasPendingChanges)
        {
            var confirmReload = MessageBox.Show(
                "Hay cambios sin guardar en esta tabla. ¿Recargar y descartarlos?",
                "Confirmar recarga",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmReload != MessageBoxResult.Yes)
            {
                return;
            }
        }

        try
        {
            var document = _dbfTableService.LoadTable(SelectedOpenTable.FilePath);
            var structure = _dbfTableService.DescribeFields(document.Fields);

            SelectedOpenTable.ReplaceDocument(document, structure);
            OnPropertyChanged(nameof(CurrentTableStructure));
            OnPropertyChanged(nameof(DirtyStatus));
            StatusMessage = "Tabla recargada desde disco.";
            NotifyCommands();
        }
        catch (Exception exception)
        {
            StatusMessage = $"Error al recargar: {exception.Message}";
            MessageBox.Show(exception.Message, "Error al recargar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveCurrentTable()
    {
        if (SelectedOpenTable is null)
        {
            return;
        }

        try
        {
            _dbfTableService.SaveTable(SelectedOpenTable.Document);
            SelectedOpenTable.MarkSaved();
            OnPropertyChanged(nameof(DirtyStatus));
            StatusMessage = "Cambios guardados en DBF.";
            NotifyCommands();
        }
        catch (Exception exception)
        {
            StatusMessage = $"Error al guardar: {exception.Message}";
            MessageBox.Show(exception.Message, "Error al guardar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportFromTable()
    {
        if (SelectedOpenTable is null)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar DBF de origen para APPEND FROM",
            Filter = "Archivos DBF (*.dbf)|*.dbf",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var sourceDocument = _dbfTableService.LoadTable(dialog.FileName);
            if (!_dbfTableService.AreCompatibleStructures(
                    SelectedOpenTable.Document.Fields,
                    sourceDocument.Fields,
                    out var mismatchReason))
            {
                MessageBox.Show(
                    $"Importacion rechazada. Estructura incompatible: {mismatchReason}",
                    "APPEND FROM",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StatusMessage = "Importacion rechazada por incompatibilidad de estructura.";
                return;
            }

            var sourceRows = sourceDocument.DataTable.Rows.Count;
            SelectedOpenTable.AppendRowsFrom(sourceDocument.DataTable);
            StatusMessage = sourceRows == 1
                ? "1 registro importado con APPEND FROM."
                : $"{sourceRows} registros importados con APPEND FROM.";

            OnPropertyChanged(nameof(DirtyStatus));
            NotifyCommands();
        }
        catch (Exception exception)
        {
            StatusMessage = $"Error al importar: {exception.Message}";
            MessageBox.Show(exception.Message, "Error de importacion", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyStructureToClipboard()
    {
        if (SelectedOpenTable is null)
        {
            return;
        }

        var lines = new List<string>
        {
            "Nombre\tTipo\tLongitud\tDecimales"
        };

        foreach (var field in SelectedOpenTable.TableStructure)
        {
            lines.Add($"{field.Name}\t{field.Type}\t{field.Length}\t{field.DecimalCount}");
        }

        Clipboard.SetText(string.Join(Environment.NewLine, lines));
        StatusMessage = "Estructura de tabla copiada al portapapeles.";
    }

    private void CloseSelectedTab()
    {
        if (SelectedOpenTable is null)
        {
            return;
        }

        CloseTab(SelectedOpenTable);
    }

    private void CloseTab(TableTabViewModel tab)
    {
        var closingSelected = ReferenceEquals(SelectedOpenTable, tab);
        tab.Dispose();
        OpenTables.Remove(tab);

        if (closingSelected)
        {
            SelectedOpenTable = OpenTables.LastOrDefault();
        }

        OnPropertyChanged(nameof(CurrentTableStructure));
        OnPropertyChanged(nameof(CurrentTableDisplayName));
        OnPropertyChanged(nameof(DirtyStatus));
        NotifyCommands();
    }

    private void OnSelectedOpenTablePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TableTabViewModel.HasPendingChanges))
        {
            OnPropertyChanged(nameof(DirtyStatus));
            NotifyCommands();
        }
        else if (e.PropertyName is nameof(TableTabViewModel.SelectedRecord) or nameof(TableTabViewModel.SelectedRecordsCount))
        {
            NotifyCommands();
        }
    }

    public void UpdateSelectedRows(TableTabViewModel? table, IEnumerable<DataRowView> selectedRows)
    {
        if (table is null)
        {
            return;
        }

        table.UpdateSelectedRecords(selectedRows);
        if (ReferenceEquals(table, SelectedOpenTable))
        {
            NotifyCommands();
        }
    }

    private void TryRestoreLastConnection()
    {
        var preferredConnection = _state.LastConnectionId.HasValue
            ? Connections.FirstOrDefault(connection => connection.Id == _state.LastConnectionId.Value)
            : null;

        SelectedConnection = preferredConnection ?? Connections.FirstOrDefault();
    }

    private void PersistState()
    {
        _state.Connections = [.. Connections];
        _connectionRepository.Save(_state);
    }

    private void NotifyCommands()
    {
        (RemoveConnectionCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RefreshFilesCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (OpenTableCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (AddRecordCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DeleteSelectedRecordCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ReloadTableCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SaveTableCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ImportFromTableCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CopyStructureCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CloseSelectedTabCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }
}
