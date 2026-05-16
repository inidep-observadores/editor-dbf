using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using EditorDbf.App.Infrastructure;
using Microsoft.Win32;

namespace EditorDbf.App.ViewModels;

public enum ExportScope
{
    All,
    Selected
}

public sealed class ExportViewModel : ObservableObject
{
    private string _destinationPath;
    private string _selectedFormat;
    private ExportScope _scope;
    private bool _canExportSelected;

    public ExportViewModel(string fullPath, bool hasSelection)
    {
        var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(fullPath);
        _destinationPath = Path.Combine(directory, fileName + "_export.dbf");
        _selectedFormat = ".dbf";
        _scope = hasSelection ? ExportScope.Selected : ExportScope.All;
        _canExportSelected = hasSelection;

        BrowseCommand = new RelayCommand(Browse);
    }

    public string DestinationPath
    {
        get => _destinationPath;
        set => SetProperty(ref _destinationPath, value);
    }

    public List<string> AvailableFormats { get; } = [".dbf", ".csv", ".xlsx"];

    public string SelectedFormat
    {
        get => _selectedFormat;
        set
        {
            if (SetProperty(ref _selectedFormat, value))
            {
                try
                {
                    var dir = Path.GetDirectoryName(DestinationPath);
                    var name = Path.GetFileNameWithoutExtension(DestinationPath);
                    DestinationPath = Path.Combine(dir ?? string.Empty, name + value);
                }
                catch { /* Ignorar rutas inválidas */ }
            }
        }
    }

    public ExportScope Scope
    {
        get => _scope;
        set => SetProperty(ref _scope, value);
    }

    public bool CanExportSelected => _canExportSelected;

    public ICommand BrowseCommand { get; }

    private void Browse()
    {
        var dialog = new SaveFileDialog
        {
            Filter = SelectedFormat switch
            {
                ".csv" => "CSV file (*.csv)|*.csv",
                ".xlsx" => "Excel file (*.xlsx)|*.xlsx",
                _ => "DBF file (*.dbf)|*.dbf"
            },
            FileName = Path.GetFileName(DestinationPath),
            InitialDirectory = Path.GetDirectoryName(DestinationPath)
        };

        if (dialog.ShowDialog() == true)
        {
            DestinationPath = dialog.FileName;
            // Actualizar formato si el usuario lo cambió en el diálogo
            var ext = Path.GetExtension(DestinationPath).ToLower();
            if (AvailableFormats.Contains(ext))
            {
                _selectedFormat = ext;
                OnPropertyChanged(nameof(SelectedFormat));
            }
        }
    }
}
