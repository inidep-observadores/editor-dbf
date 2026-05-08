namespace EditorDbf.App.Models;

public sealed class AppState
{
    public List<ConnectionProfile> Connections { get; set; } = [];

    public Guid? LastConnectionId { get; set; }

    public Dictionary<string, int> TableCodePages { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsDarkTheme { get; set; } = false;
}
