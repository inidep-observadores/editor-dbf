using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Media;
using EditorDbf.App.Models;
using EditorDbf.App.ViewModels;

namespace EditorDbf.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;

        if (e.NewValue is MainViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            ApplyTheme(newVm.IsDarkTheme);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsDarkTheme))
            ApplyTheme((sender as MainViewModel)!.IsDarkTheme);
    }

    private void ApplyTheme(bool isDark)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        dicts[0] = new System.Windows.ResourceDictionary
        {
            Source = new Uri(isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", UriKind.Relative)
        };
        ThemeIcon.Text = isDark ? "" : "";
    }

    private void DbfFilesList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox listBox || listBox.SelectedItem is null)
            return;

        if (DataContext is MainViewModel viewModel && viewModel.OpenTableCommand.CanExecute(null))
            viewModel.OpenTableCommand.Execute(null);
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
                binding.StringFormat = "dd/MM/yyyy";
        }
    }

    private void TableDataGrid_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid dataGrid) return;

        var hitTestResult = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
        if (hitTestResult?.VisualHit == null) return;

        var cell = FindVisualParent<DataGridCell>(hitTestResult.VisualHit);
        if (cell == null) return;

        if (dataGrid.DataContext is TableTabViewModel viewModel)
        {
            var columnName = cell.Column.Header?.ToString() ?? string.Empty;
            var rowView = cell.DataContext as DataRowView;
            var cellValue = rowView?[columnName];

            viewModel.LastRightClickedCellInfo = new FilterParams(columnName, "=", cellValue);
        }
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindVisualParent<T>(parentObject);
    }
}
