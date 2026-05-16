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
using EditorDbf.App.Views;

namespace EditorDbf.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ConnectionRepository _connectionRepository;
    private readonly DbfSqlService _dbfSqlService;
    private readonly DbfTableService _dbfTableService;
    private readonly AppState _state;
    private ConnectionProfile? _selectedConnection;
    private string? _selectedDbfFile;
    private object? _selectedOpenTable;
    private string _statusMessage = "Listo.";
    private bool _isDarkTheme;
    private CodePageOption? _selectedCodePageOption;

    public MainViewModel(ConnectionRepository connectionRepository, DbfTableService dbfTableService, DbfSqlService dbfSqlService)
    {
        _connectionRepository = connectionRepository;
        _dbfTableService = dbfTableService;
        _dbfSqlService = dbfSqlService;
        _state = _connectionRepository.Load();

        Connections = new ObservableCollection<ConnectionProfile>(_state.Connections);
        foreach (var conn in Connections)
        {
            conn.Exists = Directory.Exists(conn.FolderPath);
        }
        DbfFiles = new ObservableCollection<string>();
        OpenTables = new ObservableCollection<object>();
        AvailableCodePages = new ObservableCollection<CodePageOption>(CreateCodePageOptions());

        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        AddConnectionCommand = new RelayCommand(AddConnection);
        RemoveConnectionCommand = new RelayCommand<ConnectionProfile>(p => RemoveConnection(p), (p) => p is not null || SelectedConnection is not null);
        RenameConnectionCommand = new RelayCommand<ConnectionProfile>(p => RenameConnection(p), (p) => p is not null || SelectedConnection is not null);
        OpenConnectionFolderCommand = new RelayCommand<ConnectionProfile>(p => OpenConnectionFolder(p), (p) => (p ?? SelectedConnection)?.Exists == true);
        RefreshFilesCommand = new RelayCommand(RefreshFiles, () => SelectedConnection?.Exists == true);
        OpenTableCommand = new RelayCommand(OpenSelectedTable, () => SelectedConnection?.Exists == true && !string.IsNullOrWhiteSpace(SelectedDbfFile));
        AddRecordCommand = new RelayCommand(AddRecord, () => ActiveTableTab is not null);
        DeleteSelectedRecordCommand = new RelayCommand(DeleteSelectedRecord, () => ActiveTableTab?.HasSelectedRecords == true);
        ReloadTableCommand = new RelayCommand(ReloadCurrentTable, () => ActiveTableTab is not null);
        SaveTableCommand = new RelayCommand(SaveCurrentTable, () => ActiveTableTab?.HasPendingChanges == true);
        ImportFromTableCommand = new RelayCommand(ImportFromTable, () => ActiveTableTab is not null);
        ApplyCodePageCommand = new RelayCommand(ApplyCodePageForCurrentTable, () => ActiveTableTab is not null && SelectedCodePageOption is not null);
        SaveCodePageToTableCommand = new RelayCommand(SaveCodePageToTableHeader, () => ActiveTableTab is not null && SelectedCodePageOption?.LanguageDriver is not null);
        CopyStructureCommand = new RelayCommand(CopyStructureToClipboard, () => ActiveTableTab is not null);
        CloseSelectedTabCommand = new RelayCommand(CloseSelectedTab, () => SelectedOpenTable is not null);
        OpenSqlConsoleCommand = new RelayCommand(OpenSqlConsole, () => SelectedConnection?.Exists == true);
        DeleteFileCommand = new RelayCommand<string>(p => DeleteFile(p), (p) => SelectedConnection is not null && (!string.IsNullOrWhiteSpace(p) || !string.IsNullOrWhiteSpace(SelectedDbfFile)));
        ShowInExplorerCommand = new RelayCommand<string>(p => ShowInExplorer(p), (p) => SelectedConnection is not null && (!string.IsNullOrWhiteSpace(p) || !string.IsNullOrWhiteSpace(SelectedDbfFile)));

        TryRestoreLastConnection();
        IsDarkTheme = _state.IsDarkTheme;
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set => SetProperty(ref _isDarkTheme, value);
    }

    public ICommand ToggleThemeCommand { get; }

    public ObservableCollection<ConnectionProfile> Connections { get; }

    public ObservableCollection<string> DbfFiles { get; }

    public ObservableCollection<object> OpenTables { get; }

    public ObservableCollection<CodePageOption> AvailableCodePages { get; }

    public ICommand AddConnectionCommand { get; }

    public ICommand RemoveConnectionCommand { get; }
    public ICommand RenameConnectionCommand { get; }
    public ICommand OpenConnectionFolderCommand { get; }

    public ICommand RefreshFilesCommand { get; }

    public ICommand OpenTableCommand { get; }

    public ICommand AddRecordCommand { get; }

    public ICommand DeleteSelectedRecordCommand { get; }

    public ICommand ReloadTableCommand { get; }

    public ICommand SaveTableCommand { get; }

    public ICommand ImportFromTableCommand { get; }

    public ICommand ApplyCodePageCommand { get; }

    public ICommand SaveCodePageToTableCommand { get; }

    public ICommand CopyStructureCommand { get; }

    public ICommand CloseSelectedTabCommand { get; }
    public ICommand OpenSqlConsoleCommand { get; }
    public ICommand DeleteFileCommand { get; }
    public ICommand ShowInExplorerCommand { get; }

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

    public object? SelectedOpenTable
    {
        get => _selectedOpenTable;
        set
        {
            if (_selectedOpenTable == value)
            {
                return;
            }

            if (_selectedOpenTable is TableTabViewModel oldTab)
            {
                oldTab.PropertyChanged -= OnSelectedOpenTablePropertyChanged;
            }

            _selectedOpenTable = value;
            OnPropertyChanged();

            if (_selectedOpenTable is TableTabViewModel newTab)
            {
                newTab.PropertyChanged += OnSelectedOpenTablePropertyChanged;
            }

            OnPropertyChanged(nameof(ActiveTableTab));
            OnPropertyChanged(nameof(CurrentTableDisplayName));
            OnPropertyChanged(nameof(DirtyStatus));
            OnPropertyChanged(nameof(CurrentTableStructure));
            OnPropertyChanged(nameof(CurrentTableHeaderInfo));
            SyncSelectedCodePageFromActiveTable();
            NotifyCommands();
        }
    }

    public TableTabViewModel? ActiveTableTab => SelectedOpenTable as TableTabViewModel;

    public string CurrentTableDisplayName
    {
        get
        {
            if (ActiveTableTab is not null) return ActiveTableTab.FileName;
            if (SelectedOpenTable is SqlConsoleViewModel) return "Consola SQL";
            return "(ninguna)";
        }
    }

    public string DirtyStatus => ActiveTableTab?.HasPendingChanges == true ? "Cambios pendientes" : "Sin cambios";

    public IReadOnlyList<DbfFieldDescriptor> CurrentTableStructure => ActiveTableTab?.TableStructure ?? [];

    public DbfHeaderInfo? CurrentTableHeaderInfo => ActiveTableTab?.HeaderInfo;

    public CodePageOption? SelectedCodePageOption
    {
        get => _selectedCodePageOption;
        set
        {
            if (SetProperty(ref _selectedCodePageOption, value))
            {
                NotifyCommands();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        _state.IsDarkTheme = IsDarkTheme;
        PersistState();
    }

    private void AddConnection()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar carpeta (puede elegir uno o varios archivos de la carpeta)",
            Filter = "Archivos DBF (*.dbf)|*.dbf|Todos los archivos (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0)
        {
            return;
        }

        var folderPath = Path.GetDirectoryName(dialog.FileNames[0]);
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        var existing = Connections.FirstOrDefault(connection =>
            string.Equals(connection.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            SelectedConnection = existing;
            MessageBox.Show($"La carpeta '{folderPath}' ya está registrada como una conexión.", 
                "Conexión duplicada", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusMessage = "La conexión ya existe y fue seleccionada.";
            return;
        }

        var profile = new ConnectionProfile
        {
            Name = Path.GetFileName(folderPath),
            FolderPath = folderPath,
            Exists = true
        };

        Connections.Add(profile);
        _state.Connections = [.. Connections];
        PersistState();

        SelectedConnection = profile;
        StatusMessage = $"Conexión creada: {profile.DisplayName}";
    }

    public void ValidateConnections()
    {
        foreach (var conn in Connections)
        {
            conn.Exists = Directory.Exists(conn.FolderPath);
        }
        NotifyCommands();
    }

    private void RemoveConnection(ConnectionProfile? connection = null)
    {
        var target = connection ?? SelectedConnection;
        if (target is null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"¿Eliminar la conexion '{target.DisplayName}'?",
            "Confirmar eliminacion de conexion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        Connections.Remove(target);

        _state.Connections = [.. Connections];
        if (_state.LastConnectionId == target.Id)
        {
            _state.LastConnectionId = Connections.FirstOrDefault()?.Id;
        }

        PersistState();
        if (ReferenceEquals(SelectedConnection, target))
        {
            SelectedConnection = Connections.FirstOrDefault();
        }
        StatusMessage = "Conexion eliminada.";
    }
    
    private void RenameConnection(ConnectionProfile? connection = null)
    {
        var target = connection ?? SelectedConnection;
        if (target is null) return;

        var newName = Views.InputDialog.Show(
            $"Ingrese un nombre personalizado para la conexión '{target.Name}':",
            "Cambiar nombre de conexión",
            target.CustomName ?? string.Empty);

        if (newName != null)
        {
            target.CustomName = string.IsNullOrWhiteSpace(newName) ? null : newName.Trim();
            PersistState();
            StatusMessage = "Nombre de conexión actualizado.";
        }
    }

    private void OpenConnectionFolder(ConnectionProfile? connection = null)
    {
        var target = connection ?? SelectedConnection;
        if (target is null || !Directory.Exists(target.FolderPath))
        {
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{target.FolderPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al abrir el explorador: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
        var existingTab = OpenTables.OfType<TableTabViewModel>().FirstOrDefault(tab =>
            string.Equals(tab.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (existingTab is not null)
        {
            SelectedOpenTable = existingTab;
            StatusMessage = $"Pestana enfocada: {existingTab.FileName}";
            return;
        }

        try
        {
            var forcedCodePage = ResolvePreferredCodePage(filePath);
            var document = _dbfTableService.LoadTable(filePath, forcedCodePage);
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
        if (ActiveTableTab is null)
        {
            return;
        }

        ActiveTableTab.AddRecord();
        StatusMessage = "Registro agregado.";
        NotifyCommands();
        OnPropertyChanged(nameof(DirtyStatus));
    }

    private void DeleteSelectedRecord()
    {
        if (ActiveTableTab is null)
        {
            return;
        }

        var count = ActiveTableTab.SelectedRecordsCount > 0 
            ? ActiveTableTab.SelectedRecordsCount 
            : (ActiveTableTab.SelectedRecord != null ? 1 : 0);

        if (count == 0) return;

        var confirmMsg = count == 1
            ? "¿Está seguro de que desea borrar el registro seleccionado?"
            : $"¿Está seguro de que desea borrar los {count} registros seleccionados?";

        var result = MessageBox.Show(confirmMsg, "Confirmar Eliminación", 
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        var deleted = ActiveTableTab.DeleteSelectedRecords();
        if (deleted > 0)
        {
            StatusMessage = deleted == 1 ? "1 registro marcado para borrar." : $"{deleted} registros marcados para borrar.";
            NotifyCommands();
            OnPropertyChanged(nameof(DirtyStatus));
        }
    }

    private void ReloadCurrentTable()
    {
        if (ActiveTableTab is null) return;

        if (ActiveTableTab.HasPendingChanges)
        {
            var confirmReload = MessageBox.Show(
                "Hay cambios sin guardar en esta tabla. ¿Recargar y descartarlos?",
                "Confirmar recarga",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmReload != MessageBoxResult.Yes) return;
        }

        try
        {
            var forcedCodePage = ResolvePreferredCodePage(ActiveTableTab.FilePath);
            var document = _dbfTableService.LoadTable(ActiveTableTab.FilePath, forcedCodePage);
            var structure = _dbfTableService.DescribeFields(document.Fields);

            ActiveTableTab.ReplaceDocument(document, structure);
            OnPropertyChanged(nameof(CurrentTableStructure));
            OnPropertyChanged(nameof(CurrentTableHeaderInfo));
            OnPropertyChanged(nameof(DirtyStatus));
            SyncSelectedCodePageFromActiveTable();
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
        if (ActiveTableTab is null) return;
        
        if (SaveTab(ActiveTableTab))
        {
            // Verificar otras pestañas con cambios
            var otherDirtyTabs = OpenTables.OfType<TableTabViewModel>()
                .Where(t => t != ActiveTableTab && t.HasPendingChanges)
                .ToList();

            if (otherDirtyTabs.Any())
            {
                var fileNames = string.Join("\n- ", otherDirtyTabs.Select(t => t.FileName));
                var result = MessageBox.Show(
                    $"También hay cambios pendientes en:\n- {fileNames}\n\n¿Desea guardarlos ahora?",
                    "Cambios pendientes en otras tablas",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var savedCount = 0;
                    foreach (var tab in otherDirtyTabs)
                    {
                        if (SaveTab(tab)) savedCount++;
                    }
                    StatusMessage = $"Se guardaron {savedCount + 1} tablas en total.";
                }
            }
        }
    }

    private bool SaveTab(TableTabViewModel tab)
    {
        try
        {
            _dbfTableService.SaveTable(tab.Document);
            tab.MarkSaved();
            if (ReferenceEquals(tab, ActiveTableTab))
            {
                OnPropertyChanged(nameof(DirtyStatus));
                StatusMessage = "Cambios guardados en DBF.";
                NotifyCommands();
            }
            return true;
        }
        catch (Exception exception)
        {
            StatusMessage = $"Error al guardar: {exception.Message}";
            MessageBox.Show(exception.Message, "Error al guardar", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void DeleteFile(string? fileName = null)
    {
        var file = fileName ?? SelectedDbfFile;
        if (SelectedConnection is null || string.IsNullOrWhiteSpace(file)) return;
        
        var filePath = Path.Combine(SelectedConnection.FolderPath, file);
        
        var result = MessageBox.Show(
            $"¿Está seguro de que desea eliminar el archivo '{file}' permanentemente del disco?",
            "Confirmar eliminación de archivo",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            // Cerrar pestaña si está abierta
            var tab = OpenTables.OfType<TableTabViewModel>().FirstOrDefault(t => 
                string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            
            if (tab != null)
            {
                CloseTab(tab);
            }

            File.Delete(filePath);
            
            // Eliminar archivos de memo si existen
            var memoExtensions = new[] { ".fpt", ".dbt", ".FPT", ".DBT" };
            foreach (var ext in memoExtensions)
            {
                var memoPath = Path.ChangeExtension(filePath, ext);
                if (File.Exists(memoPath)) File.Delete(memoPath);
            }

            StatusMessage = $"Archivo eliminado: {file}";
            RefreshFiles();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al eliminar archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowInExplorer(string? fileName = null)
    {
        var file = fileName ?? SelectedDbfFile;
        if (SelectedConnection is null || string.IsNullOrWhiteSpace(file)) return;
        
        var filePath = Path.Combine(SelectedConnection.FolderPath, file);
        if (!File.Exists(filePath)) return;

        try
        {
            var argument = $"/select,\"{filePath}\"";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = argument,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al abrir el explorador: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportFromTable()
    {
        if (ActiveTableTab is null) return;

        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar DBF de origen para APPEND FROM",
            Filter = "Archivos DBF (*.dbf)|*.dbf",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var sourceForcedCodePage = ResolvePreferredCodePage(dialog.FileName);
            var sourceDocument = _dbfTableService.LoadTable(dialog.FileName, sourceForcedCodePage);
            if (!_dbfTableService.AreCompatibleStructures(
                    ActiveTableTab.Document.Fields,
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
            ActiveTableTab.AppendRowsFrom(sourceDocument.DataTable);
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

    private void ApplyCodePageForCurrentTable()
    {
        if (ActiveTableTab is null || SelectedCodePageOption is null) return;

        try
        {
            _state.TableCodePages[ActiveTableTab.FilePath] = SelectedCodePageOption.CodePage;
            PersistState();

            var document = _dbfTableService.LoadTable(ActiveTableTab.FilePath, SelectedCodePageOption.CodePage);
            var structure = _dbfTableService.DescribeFields(document.Fields);
            ActiveTableTab.ReplaceDocument(document, structure);

            OnPropertyChanged(nameof(CurrentTableStructure));
            OnPropertyChanged(nameof(CurrentTableHeaderInfo));
            StatusMessage = $"Tabla recargada con codepage {SelectedCodePageOption.CodePage}.";
            NotifyCommands();
        }
        catch (Exception exception)
        {
            StatusMessage = $"Error al aplicar codepage: {exception.Message}";
            MessageBox.Show(exception.Message, "Error de codepage", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveCodePageToTableHeader()
    {
        if (ActiveTableTab is null || SelectedCodePageOption?.LanguageDriver is null) return;

        try
        {
            _dbfTableService.UpdateLanguageDriverByte(ActiveTableTab.FilePath, SelectedCodePageOption.LanguageDriver.Value);
            _state.TableCodePages[ActiveTableTab.FilePath] = SelectedCodePageOption.CodePage;
            PersistState();

            var document = _dbfTableService.LoadTable(ActiveTableTab.FilePath, SelectedCodePageOption.CodePage);
            var structure = _dbfTableService.DescribeFields(document.Fields);
            ActiveTableTab.ReplaceDocument(document, structure);

            OnPropertyChanged(nameof(CurrentTableStructure));
            OnPropertyChanged(nameof(CurrentTableHeaderInfo));
            StatusMessage = $"Codepage guardado en cabecera: {SelectedCodePageOption.Label}.";
            NotifyCommands();
        }
        catch (Exception exception)
        {
            StatusMessage = $"Error al guardar codepage: {exception.Message}";
            MessageBox.Show(exception.Message, "Error de codepage", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyStructureToClipboard()
    {
        if (ActiveTableTab is null) return;

        var lines = new List<string> { "Nombre\tTipo\tLongitud\tDecimales" };

        foreach (var field in ActiveTableTab.TableStructure)
        {
            lines.Add($"{field.Name}\t{field.Type}\t{field.Length}\t{field.DecimalCount}");
        }

        Clipboard.SetText(string.Join(Environment.NewLine, lines));
        StatusMessage = "Estructura de tabla copiada al portapapeles.";
    }

    private void CloseSelectedTab()
    {
        if (SelectedOpenTable is not null)
        {
            TryCloseTab(SelectedOpenTable);
        }
    }

    private void OpenSqlConsole()
    {
        if (SelectedConnection is null) return;

        var console = new SqlConsoleViewModel(_dbfSqlService, SelectedConnection.FolderPath, (vm) => 
        {
            CloseAnyTab(vm);
            StatusMessage = "Consola SQL cerrada.";
        });
        OpenTables.Add(console);
        SelectedOpenTable = console;
        StatusMessage = "Consola SQL abierta.";
    }

    private void CloseTab(TableTabViewModel tab) => TryCloseTab(tab);

    public bool RequestCloseAll()
    {
        var tabs = OpenTables.ToList();
        foreach (var tab in tabs)
        {
            if (!TryCloseTab(tab))
            {
                return false;
            }
        }
        return true;
    }

    private bool TryCloseTab(object tab)
    {
        if (tab is TableTabViewModel tableTab && tableTab.HasPendingChanges)
        {
            var result = MessageBox.Show(
                $"La tabla '{tableTab.FileName}' tiene cambios pendientes. ¿Desea guardarlos antes de cerrar?",
                "Cambios pendientes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }

            if (result == MessageBoxResult.Yes)
            {
                if (!SaveTab(tableTab))
                {
                    return false;
                }
            }
        }

        CloseAnyTab(tab);
        return true;
    }

    private void CloseAnyTab(object tab)
    {
        var closingSelected = ReferenceEquals(SelectedOpenTable, tab);
        
        if (tab is TableTabViewModel tableTab)
        {
            tableTab.Dispose();
        }
        
        OpenTables.Remove(tab);

        if (closingSelected)
        {
            SelectedOpenTable = OpenTables.LastOrDefault();
        }

        OnPropertyChanged(nameof(CurrentTableStructure));
        OnPropertyChanged(nameof(CurrentTableHeaderInfo));
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
        (RemoveConnectionCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (RenameConnectionCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (OpenConnectionFolderCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (RefreshFilesCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (OpenTableCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (AddRecordCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (DeleteSelectedRecordCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (ReloadTableCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (SaveTableCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (ImportFromTableCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (ApplyCodePageCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (SaveCodePageToTableCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (CopyStructureCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (CloseSelectedTabCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (OpenSqlConsoleCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (DeleteFileCommand as IRelayCommand)?.RaiseCanExecuteChanged();
        (ShowInExplorerCommand as IRelayCommand)?.RaiseCanExecuteChanged();
    }

    private int? ResolvePreferredCodePage(string filePath)
    {
        return _state.TableCodePages.TryGetValue(filePath, out var preferredCodePage)
            ? preferredCodePage
            : null;
    }

    private void SyncSelectedCodePageFromActiveTable()
    {
        if (ActiveTableTab is null)
        {
            SelectedCodePageOption = null;
            return;
        }

        var codePage = ActiveTableTab.Document.EffectiveCodePage;
        SelectedCodePageOption = AvailableCodePages.FirstOrDefault(option => option.CodePage == codePage)
            ?? AvailableCodePages.FirstOrDefault();
    }

    private static IReadOnlyList<CodePageOption> CreateCodePageOptions()
    {
        return
        [
            new CodePageOption { Label = "Win 1252 (0x03)", CodePage = 1252, LanguageDriver = 0x03 },
            new CodePageOption { Label = "Win 1250 (0x58)", CodePage = 1250, LanguageDriver = 0x58 },
            new CodePageOption { Label = "Win 1251 (0x59)", CodePage = 1251, LanguageDriver = 0x59 },
            new CodePageOption { Label = "DOS 850 (0x02)", CodePage = 850, LanguageDriver = 0x02 },
            new CodePageOption { Label = "DOS 437 (0x01)", CodePage = 437, LanguageDriver = 0x01 },
            new CodePageOption { Label = "DOS 852 (0x64)", CodePage = 852, LanguageDriver = 0x64 },
            new CodePageOption { Label = "DOS 866 (0x65)", CodePage = 866, LanguageDriver = 0x65 },
            new CodePageOption { Label = "Shift-JIS 932 (0x7B)", CodePage = 932, LanguageDriver = 0x7B },
            new CodePageOption { Label = "GBK 936 (0x7A)", CodePage = 936, LanguageDriver = 0x7A },
            new CodePageOption { Label = "Korean 949 (0x79)", CodePage = 949, LanguageDriver = 0x79 },
            new CodePageOption { Label = "Big5 950 (0x78)", CodePage = 950, LanguageDriver = 0x78 },
            new CodePageOption { Label = "Thai 874 (0x7C)", CodePage = 874, LanguageDriver = 0x7C },
            new CodePageOption { Label = "UTF-8 (solo vista)", CodePage = 65001, LanguageDriver = null }
        ];
    }
}
