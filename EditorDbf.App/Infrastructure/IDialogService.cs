using System.Windows;

namespace EditorDbf.App.Infrastructure;

public interface IDialogService
{
    /// <summary>
    /// Muestra un diálogo de confirmación con botones configurables.
    /// </summary>
    MessageBoxResult ShowConfirm(string message, string title, MessageBoxButton buttons = MessageBoxButton.YesNo);

    /// <summary>
    /// Muestra un diálogo de error.
    /// </summary>
    void ShowError(string message, string title);

    /// <summary>
    /// Muestra un diálogo informativo.
    /// </summary>
    void ShowInfo(string message, string title);

    /// <summary>
    /// Muestra un diálogo de entrada de texto.
    /// </summary>
    /// <returns>El texto ingresado, o null si se canceló.</returns>
    string? ShowInput(string prompt, string title, string defaultValue = "");

    /// <summary>
    /// Muestra un diálogo para abrir archivo(s).
    /// </summary>
    /// <returns>Array de rutas seleccionadas, o null si se canceló.</returns>
    string[]? ShowOpenFileDialog(string title, string filter, bool multiselect = false);
}
