using System.Collections.ObjectModel;
using EditorDbf.App.Infrastructure;

namespace EditorDbf.App.Models;

public class SqlSchemaItem : ObservableObject
{
    private bool _isChecked;
    private SqlSchemaItem? _parent;

    public string Name { get; set; } = string.Empty;
    public string SqlName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public ObservableCollection<SqlSchemaItem> Children { get; set; } = new();

    public SqlSchemaItem? Parent
    {
        get => _parent;
        set => SetProperty(ref _parent, value);
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (SetProperty(ref _isChecked, value))
            {
                // Propagación hacia los hijos (ej: marcar tabla marca todos los campos)
                foreach (var child in Children)
                {
                    child.IsChecked = value;
                }
            }
        }
    }
}
