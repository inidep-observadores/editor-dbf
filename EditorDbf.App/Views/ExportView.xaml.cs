using System.Windows;
using EditorDbf.App.ViewModels;

namespace EditorDbf.App.Views;

public partial class ExportView : Window
{
    public ExportView(ExportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
