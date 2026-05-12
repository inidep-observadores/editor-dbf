using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using EditorDbf.App.Services;
using EditorDbf.App.ViewModels;

namespace EditorDbf.App;

public partial class App : Application
{
    private static Mutex? _mutex;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    protected override void OnStartup(StartupEventArgs e)
    {
        this.DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Error fatal no controlado:\n{args.Exception.Message}\n\nDetalles:\n{args.Exception.InnerException?.Message}", "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Shutdown();
        };

        _mutex = new Mutex(true, "EditorDbf_DebugInstanceMutex", out bool isNewInstance);

        if (!isNewInstance)
        {
            BringExistingInstanceToFront();
            Shutdown();
            return;
        }

        // Habilitar soporte para codificaciones legacy (CP1252, CP850, etc.)
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        base.OnStartup(e);

        var connectionRepository = new ConnectionRepository();
        var dbfTableService = new DbfTableService();
        var dbfSqlService = new DbfSqlService(dbfTableService);
        var mainViewModel = new MainViewModel(connectionRepository, dbfTableService, dbfSqlService);

        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        mainWindow.Show();
    }

    private static void BringExistingInstanceToFront()
    {
        var currentProcess = Process.GetCurrentProcess();
        var processes = Process.GetProcessesByName(currentProcess.ProcessName);

        foreach (var process in processes)
        {
            if (process.Id != currentProcess.Id)
            {
                var hWnd = process.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                {
                    ShowWindow(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd);
                }
                break;
            }
        }
    }
}

