namespace EditorDbf.App.Infrastructure;

public interface IProcessService
{
    /// <summary>
    /// Abre una carpeta en el explorador de archivos.
    /// </summary>
    void OpenFolder(string folderPath);

    /// <summary>
    /// Abre el explorador de archivos y selecciona un archivo específico.
    /// </summary>
    void ShowFileInExplorer(string filePath);
}
