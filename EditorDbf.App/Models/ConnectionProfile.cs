namespace EditorDbf.App.Models;

public sealed class ConnectionProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string FolderPath { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string DisplayName => $"{Name} ({FolderPath})";
}
