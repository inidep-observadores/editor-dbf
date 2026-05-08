namespace EditorDbf.App.Models;

public sealed class DbfHeaderInfo
{
    public int RecordCount { get; init; }

    public int FieldCount { get; init; }

    public int HeaderSize { get; init; }

    public int RecordSize { get; init; }

    public DateTime? LastUpdated { get; init; }

    public string LanguageCodeDescription { get; init; } = string.Empty;

    public string FileTypeDescription { get; init; } = string.Empty;

    public string FullPath { get; init; } = string.Empty;
}
