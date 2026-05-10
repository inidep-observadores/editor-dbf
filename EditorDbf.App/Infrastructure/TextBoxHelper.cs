using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EditorDbf.App.Infrastructure;

public static class TextBoxHelper
{
    public static readonly DependencyProperty SelectedTextProperty =
        DependencyProperty.RegisterAttached(
            "SelectedText",
            typeof(string),
            typeof(TextBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static string GetSelectedText(DependencyObject obj) => (string)obj.GetValue(SelectedTextProperty);
    public static void SetSelectedText(DependencyObject obj, string value) => obj.SetValue(SelectedTextProperty, value);

    public static readonly DependencyProperty CaretIndexProperty =
        DependencyProperty.RegisterAttached(
            "CaretIndex",
            typeof(int),
            typeof(TextBoxHelper),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCaretIndexChanged));

    public static int GetCaretIndex(DependencyObject obj) => (int)obj.GetValue(CaretIndexProperty);
    public static void SetCaretIndex(DependencyObject obj, int value) => obj.SetValue(CaretIndexProperty, value);

    public static readonly DependencyProperty BindSelectionProperty =
        DependencyProperty.RegisterAttached(
            "BindSelection",
            typeof(bool),
            typeof(TextBoxHelper),
            new PropertyMetadata(false, OnBindSelectionChanged));

    public static bool GetBindSelection(DependencyObject obj) => (bool)obj.GetValue(BindSelectionProperty);
    public static void SetBindSelection(DependencyObject obj, bool value) => obj.SetValue(BindSelectionProperty, value);

    public static readonly DependencyProperty FocusTriggerProperty =
        DependencyProperty.RegisterAttached(
            "FocusTrigger",
            typeof(bool),
            typeof(TextBoxHelper),
            new PropertyMetadata(false, OnFocusTriggerChanged));

    public static bool GetFocusTrigger(DependencyObject obj) => (bool)obj.GetValue(FocusTriggerProperty);
    public static void SetFocusTrigger(DependencyObject obj, bool value) => obj.SetValue(FocusTriggerProperty, value);

    private static void OnFocusTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element && (bool)e.NewValue)
        {
            // Usar Dispatcher para asegurar que el foco se asigne después de que terminen los eventos de UI actuales
            element.Dispatcher.BeginInvoke(new Action(() =>
            {
                element.Focus();
                if (element is TextBox textBox)
                {
                    Keyboard.Focus(textBox);
                    // A veces es necesario forzar la actualización visual del cursor
                    var index = textBox.CaretIndex;
                    textBox.CaretIndex = index;
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private static void OnBindSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                textBox.SelectionChanged += OnSelectionChanged;
                
                // Sincronizar valores iniciales del VM al Control al activarse
                var initialCaret = GetCaretIndex(textBox);
                if (initialCaret > 0)
                {
                    textBox.CaretIndex = Math.Min(initialCaret, textBox.Text.Length);
                }
            }
            else
            {
                textBox.SelectionChanged -= OnSelectionChanged;
            }
        }
    }

    private static void OnCaretIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox && (int)e.NewValue != textBox.CaretIndex)
        {
            textBox.CaretIndex = (int)e.NewValue;
        }
    }

    private static void OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // Solo actualizar si el valor ha cambiado realmente para evitar bucles de bindeo
            var currentCaret = textBox.CaretIndex;
            if (GetCaretIndex(textBox) != currentCaret)
            {
                SetCaretIndex(textBox, currentCaret);
            }

            var currentSelection = textBox.SelectedText;
            if (GetSelectedText(textBox) != currentSelection)
            {
                SetSelectedText(textBox, currentSelection);
            }
        }
    }

    public static void InsertText(TextBox textBox, string text)
    {
        if (textBox == null) return;

        int index = textBox.CaretIndex;
        textBox.Text = textBox.Text.Insert(index, text);
        textBox.CaretIndex = index + text.Length;
        textBox.Focus();
    }
}
