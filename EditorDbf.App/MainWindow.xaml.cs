using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using EditorDbf.App.ViewModels;

namespace EditorDbf.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void DbfFilesList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox listBox || listBox.SelectedItem is null)
        {
            return;
        }

        if (DataContext is MainViewModel viewModel && viewModel.OpenTableCommand.CanExecute(null))
        {
            viewModel.OpenTableCommand.Execute(null);
        }
    }

    private void TableDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid dataGrid ||
            dataGrid.DataContext is not TableTabViewModel table ||
            DataContext is not MainViewModel main)
        {
            return;
        }

        var selectedRows = dataGrid.SelectedItems
            .OfType<DataRowView>()
            .ToArray();

        main.UpdateSelectedRows(table, selectedRows);
    }

    private void TableDataGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyType == typeof(DateTime) && e.Column is DataGridTextColumn textColumn)
        {
            if (textColumn.Binding is Binding binding)
            {
                binding.StringFormat = "dd/MM/yyyy";
            }
        }
    }
}
