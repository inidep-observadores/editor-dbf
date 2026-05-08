namespace EditorDbf.App.Models;

public sealed class DbfFieldDescriptor
{
    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public int Length { get; init; }

    public int DecimalCount { get; init; }
}
