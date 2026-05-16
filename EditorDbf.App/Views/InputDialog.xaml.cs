using System.Windows;

namespace EditorDbf.App.Views;

public partial class InputDialog : Window
{
    public string Answer => InputTextBox.Text;

    public InputDialog(string question, string defaultAnswer = "")
    {
        InitializeComponent();
        PromptLabel.Text = question;
        InputTextBox.Text = defaultAnswer;
        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    public static string? Show(string question, string title = "Entrada de datos", string defaultAnswer = "", Window? owner = null)
    {
        var dialog = new InputDialog(question, defaultAnswer);
        dialog.Title = title;

        var actualOwner = owner ?? Application.Current.MainWindow;
        if (actualOwner != null && actualOwner != dialog)
        {
            dialog.Owner = actualOwner;
        }

        if (dialog.ShowDialog() == true)
        {
            return dialog.Answer;
        }

        return null;
    }
}
