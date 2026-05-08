using System.Data;
using System.Text;
using DotNetDBF;

namespace EditorDbf.App.Models;

public sealed class DbfTableDocument
{
    public required string FilePath { get; init; }

    public required byte Signature { get; init; }

    public required byte LanguageDriver { get; init; }

    public required Encoding Encoding { get; init; }

    public required DBFField[] Fields { get; init; }

    public required DataTable DataTable { get; init; }
}
