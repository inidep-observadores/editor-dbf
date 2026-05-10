using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Input;
using EditorDbf.App.Infrastructure;
using EditorDbf.App.Services;

namespace EditorDbf.App.ViewModels;

public sealed class SqlConsoleViewModel : ObservableObject
{
    private readonly DbfSqlService _sqlService;
    private readonly string _folderPath;
    private readonly Action<SqlConsoleViewModel> _closeCallback;

    private string _sqlQuery = "SELECT * FROM ";
    private string _selectedText = string.Empty;
    private DataTable? _results;
    private string _statusMessage = "Escribe una consulta SQL y presiona Ejecutar.";
    private bool _isBusy;

    public SqlConsoleViewModel(DbfSqlService sqlService, string folderPath, Action<SqlConsoleViewModel> closeCallback)
    {
        _sqlService = sqlService;
        _folderPath = folderPath;
        _closeCallback = closeCallback;

        ExecuteCommand = new RelayCommand(async () => await ExecuteQueryAsync(), 
            () => (!string.IsNullOrWhiteSpace(SqlQuery) || !string.IsNullOrWhiteSpace(SelectedText)) && !IsBusy);
        CloseCommand = new RelayCommand(() => _closeCallback(this));
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
