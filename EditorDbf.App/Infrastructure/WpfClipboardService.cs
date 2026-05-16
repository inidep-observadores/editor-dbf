using System.Windows;

namespace EditorDbf.App.Infrastructure;

public sealed class WpfClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        Clipboard.SetText(text);
    }
}
