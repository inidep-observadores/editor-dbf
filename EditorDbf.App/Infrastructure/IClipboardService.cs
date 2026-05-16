namespace EditorDbf.App.Infrastructure;

public interface IClipboardService
{
    /// <summary>
    /// Copia un texto al portapapeles.
    /// </summary>
    void SetText(string text);
}
