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
using EditorDbf.App.Infrastructure;

namespace EditorDbf.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Establecer el lenguaje para que las conversiones de WPF (fechas, números) respeten el formato dd/MM/yyyy
        // Usamos es-AR como base para asegurar dd/MM/yyyy independientemente de la configuración regional del SO
        this.Language = System.Windows.Markup.XmlLanguage.GetLanguage("es-AR");
        DataContextChanged += OnDataContextChanged;
        Activated += OnWindowActivated;
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ValidateConnections();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            if (!viewModel.RequestCloseAll())
            {
                e.Cancel = true;
            }
        }
        base.OnClosing(e);
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

    private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.Column is DataGridTextColumn textColumn)
        {
            if (e.PropertyType == typeof(DateTime))
            {
                if (textColumn.Binding is Binding binding)
                {
                    binding.StringFormat = "dd/MM/yyyy";
                    // Asegurar que la edición use el formato dd/MM/yyyy para parsear la fecha ingresada
                    binding.ConverterCulture = System.Globalization.CultureInfo.GetCultureInfo("es-AR");
                }

                // Centrar fechas
                textColumn.ElementStyle = CreateTextAlignmentStyle(HorizontalAlignment.Center);
            }
            else if (IsNumericType(e.PropertyType))
            {
                // Alinear números a la derecha
                textColumn.ElementStyle = CreateTextAlignmentStyle(HorizontalAlignment.Right);
                EventBindingHelper.SetIsNumeric(textColumn, true);
            }

            // Resaltar celdas modificadas
            var cellStyle = new Style(typeof(DataGridCell));
            if (Application.Current.TryFindResource(typeof(DataGridCell)) is Style baseStyle)
                cellStyle.BasedOn = baseStyle;

            var multiBinding = new MultiBinding
            {
                Converter = (IMultiValueConverter)Application.Current.Resources["CellModifiedConverter"]
            };
            multiBinding.Bindings.Add(new Binding()); // DataRowView (values[0])
            multiBinding.Bindings.Add(new Binding { Path = new PropertyPath($"[{e.PropertyName}]") }); // Valor de la celda (values[1]) para disparar el refresco
            multiBinding.Bindings.Add(new Binding { Source = e.PropertyName }); // Nombre de la columna (values[2])

            var trigger = new DataTrigger
            {
                Binding = multiBinding,
                Value = true
            };
            trigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new DynamicResourceExtension("GridModifiedCellBrush")));

            cellStyle.Triggers.Add(trigger);
            textColumn.CellStyle = cellStyle;
        }
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(decimal) || type == typeof(double) ||
               type == typeof(float) || type == typeof(long) || type == typeof(short) ||
               type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort);
    }

    private Style CreateTextAlignmentStyle(HorizontalAlignment alignment)
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, alignment));
        
        // Añadir un pequeño margen lateral para que el texto no pegue contra el borde de la celda
        if (alignment == HorizontalAlignment.Right)
            style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(0, 0, 4, 0)));
        else if (alignment == HorizontalAlignment.Left)
            style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(4, 0, 0, 0)));
            
        return style;
    }

    private void TableDataGrid_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Solo nos interesa el punto del teclado numérico
        if (e.Key == Key.Decimal)
        {
            if (sender is DataGrid dg && dg.CurrentColumn != null && EventBindingHelper.GetIsNumeric(dg.CurrentColumn))
            {
                if (dg.IsReadOnly) return;

                // Si estamos en modo edición, el elemento enfocado debería ser un TextBox (o similar)
                if (Keyboard.FocusedElement is TextBox textBox)
                {
                    // Usamos la cultura configurada para la ventana (es-AR) para obtener el separador
                    var culture = this.Language.GetSpecificCulture();
                    var separator = culture.NumberFormat.NumberDecimalSeparator;

                    if (separator == ",")
                    {
                        e.Handled = true;

                        // Simulamos la entrada de texto de una coma. 
                        // Esto asegura que se respete el binding y eventos de cambio de texto.
                        var textArgs = new TextCompositionEventArgs(InputManager.Current.PrimaryKeyboardDevice,
                            new TextComposition(InputManager.Current, textBox, ","))
                        {
                            RoutedEvent = TextCompositionManager.TextInputEvent
                        };

                        textBox.RaiseEvent(textArgs);
                    }
                }
            }
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
