using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace EditorDbf.App.Infrastructure;

/// <summary>
/// Helper para bindeo de eventos a comandos (ej: MouseDoubleClick).
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

    private static void OnMouseDoubleClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Control control)
        {
            control.MouseDoubleClick -= OnMouseDoubleClick;
            if (e.NewValue is not null)
            {
                control.MouseDoubleClick += OnMouseDoubleClick;
            }
        }
    }

    private static void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DependencyObject d)
        {
            var command = GetMouseDoubleClickCommand(d);
            var parameter = GetMouseDoubleClickCommandParameter(d);

            if (command?.CanExecute(parameter) == true)
            {
                command.Execute(parameter);
                e.Handled = true;
            }
        }
    }
}
