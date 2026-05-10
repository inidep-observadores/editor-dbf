using System.Collections.ObjectModel;
using EditorDbf.App.Infrastructure;

namespace EditorDbf.App.Models;

public class SqlSchemaItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string SqlName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public ObservableCollection<SqlSchemaItem> Children { get; set; } = new();
}
