using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace EditorDbf.App.Infrastructure;

/// <summary>
/// Helper para bindeo de eventos a comandos (ej: MouseDoubleClick).
/// Soporta cualquier UIElement mediante la detección de ClickCount en MouseDown.
/// </summary>
public static class EventBindingHelper
{
    public static readonly DependencyProperty MouseDoubleClickCommandProperty =
        DependencyProperty.RegisterAttached(
            "MouseDoubleClickCommand",
            typeof(ICommand),
            typeof(EventBindingHelper),
            new PropertyMetadata(null, OnMouseDoubleClickCommandChanged));

    public static readonly DependencyProperty MouseDoubleClickCommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "MouseDoubleClickCommandParameter",
            typeof(object),
            typeof(EventBindingHelper),
            new PropertyMetadata(null));

    public static ICommand GetMouseDoubleClickCommand(DependencyObject obj) => (ICommand)obj.GetValue(MouseDoubleClickCommandProperty);
    public static void SetMouseDoubleClickCommand(DependencyObject obj, ICommand value) => obj.SetValue(MouseDoubleClickCommandProperty, value);

    public static object GetMouseDoubleClickCommandParameter(DependencyObject obj) => obj.GetValue(MouseDoubleClickCommandParameterProperty);
    public static void SetMouseDoubleClickCommandParameter(DependencyObject obj, object value) => obj.SetValue(MouseDoubleClickCommandParameterProperty, value);

    public static readonly DependencyProperty IsNumericProperty =
        DependencyProperty.RegisterAttached(
            "IsNumeric",
            typeof(bool),
            typeof(EventBindingHelper),
            new PropertyMetadata(false));

    public static bool GetIsNumeric(DependencyObject obj) => (bool)obj.GetValue(IsNumericProperty);
    public static void SetIsNumeric(DependencyObject obj, bool value) => obj.SetValue(IsNumericProperty, value);

    private static void OnMouseDoubleClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            // Usamos PreviewMouseDown para tener mayor control sobre el burbujeo si fuera necesario,
            // pero MouseDown es suficiente para el StackPanel dentro del TreeView.
            element.MouseDown -= OnMouseDown;
            if (e.NewValue is not null)
            {
                element.MouseDown += OnMouseDown;
            }
        }
    }

    private static void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Detectamos el doble clic mediante ClickCount
        if (e.ClickCount == 2 && sender is DependencyObject d)
        {
            var command = GetMouseDoubleClickCommand(d);
            var parameter = GetMouseDoubleClickCommandParameter(d);

            if (command?.CanExecute(parameter) == true)
            {
                command.Execute(parameter);
                // Marcamos como manejado para que el evento no burbujee a los contenedores padre (TreeViewItem padre)
                e.Handled = true;
            }
        }
    }
}
