using System;
using System.Diagnostics;

namespace EditorDbf.App.Infrastructure;

public sealed class WpfProcessService : IProcessService
{
    public void OpenFolder(string folderPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folderPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al abrir el explorador: {ex.Message}", ex);
        }
    }

    public void ShowFileInExplorer(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al abrir el explorador: {ex.Message}", ex);
        }
    }
}
