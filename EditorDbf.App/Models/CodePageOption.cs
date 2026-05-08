namespace EditorDbf.App.Models;

public sealed class CodePageOption
{
    public required string Label { get; init; }

    public required int CodePage { get; init; }

    public byte? LanguageDriver { get; init; }

    public override string ToString() => Label;
}
