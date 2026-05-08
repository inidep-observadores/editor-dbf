using System.Windows;
using EditorDbf.App.Services;
using EditorDbf.App.ViewModels;

namespace EditorDbf.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var connectionRepository = new ConnectionRepository();
        var dbfTableService = new DbfTableService();
        var mainViewModel = new MainViewModel(connectionRepository, dbfTableService);

        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        mainWindow.Show();
    }
}
