namespace EditorDbf.App.Models;

public sealed class AppState
{
    public List<ConnectionProfile> Connections { get; set; } = [];

    public Guid? LastConnectionId { get; set; }
}
