using System.Windows;
using System.Windows.Controls;

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

    private static void OnBindSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                textBox.SelectionChanged += OnSelectionChanged;
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
            SetSelectedText(textBox, textBox.SelectedText);
            SetCaretIndex(textBox, textBox.CaretIndex);
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
