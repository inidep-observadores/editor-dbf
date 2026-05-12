using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace EditorDbf.App.Infrastructure;

public sealed class IconExtension : MarkupExtension
{
    public string? Text { get; set; }
    public double FontSize { get; set; } = 14;
    public string FontFamily { get; set; } = "Segoe MDL2 Assets";

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new TextBlock
        {
            Text = Text,
            FontFamily = new FontFamily(FontFamily),
            FontSize = FontSize,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        };
    }
}

public sealed class IconAttribute : Attribute { } // Not really needed, just for naming consistency if I wanted to use it elsewhere.
