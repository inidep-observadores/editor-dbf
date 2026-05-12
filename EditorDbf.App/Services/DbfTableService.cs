using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DotNetDBF;
using EditorDbf.App.Models;

namespace EditorDbf.App.Services;

public sealed class DbfTableService
{
    public IReadOnlyList<string> ListDbfFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return [];
        }

        return Directory.EnumerateFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList()!;
    }

    public DbfTableDocument LoadTable(string filePath, int? forcedCodePage = null)
    {
        using var reader = new DBFReader(filePath);
        if (forcedCodePage.HasValue)
        {
            reader.CharEncoding = TryGetEncoding(forcedCodePage.Value);
        }

        var fields = reader.Fields ?? [];
        var table = BuildDataTable(filePath, fields);

        for (var recordIndex = 0; recordIndex < reader.RecordCount; recordIndex++)
        {
            var record = reader.NextRecord();
            if (record is null)
            {
                continue;
            }

            var row = table.NewRow();
            for (var fieldIndex = 0; fieldIndex < fields.Length && fieldIndex < record.Length; fieldIndex++)
            {
                row[fieldIndex] = NormalizeIncomingValue(record[fieldIndex]);
            }

            table.Rows.Add(row);
        }

        table.AcceptChanges();

        var signature = ReadHeaderByte(filePath, 0);
        var languageDriver = ReadHeaderByte(filePath, 29);
        var headerInfo = ReadHeaderInfo(filePath, fields.Length, reader.RecordCount, signature, languageDriver);

        var effectiveEncoding = forcedCodePage.HasValue
            ? TryGetEncoding(forcedCodePage.Value)
            : reader.CharEncoding ?? CultureInfo.CurrentCulture.TextInfo.ANSICodePage switch
            {
                > 0 and var codePage => TryGetEncoding(codePage),
                _ => Encoding.GetEncoding(1252)
            };

        return new DbfTableDocument
        {
            FilePath = filePath,
            Signature = signature,
            LanguageDriver = languageDriver,
            Encoding = effectiveEncoding,
            EffectiveCodePage = effectiveEncoding.CodePage,
            Fields = fields,
            DataTable = table,
            HeaderInfo = headerInfo
        };
    }

    public void SaveTable(DbfTableDocument document)
    {
        var tempFilePath = Path.Combine(
            Path.GetDirectoryName(document.FilePath)!,
            $"{Path.GetFileNameWithoutExtension(document.FilePath)}.tmp{Path.GetExtension(document.FilePath)}");

        var hasMemo = document.Fields.Any(f => f.DataType == NativeDbType.Memo);

        using (var output = File.Open(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            using var writer = new DBFWriter
            {
                Fields = document.Fields,
                Signature = document.Signature == 0 ? DBFSignature.DBase3 : document.Signature,
                LanguageDriver = document.LanguageDriver
            };

            writer.CharEncoding = document.Encoding;

            if (hasMemo)
            {
                writer.DataMemoLoc = Path.ChangeExtension(tempFilePath, ".fpt");
            }

            foreach (DataRow row in document.DataTable.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                {
                    continue;
                }

                var values = new object[document.Fields.Length];
                for (var fieldIndex = 0; fieldIndex < document.Fields.Length; fieldIndex++)
                {
                    var value = row[fieldIndex];
                    values[fieldIndex] = NormalizeOutgoingValue(value, document.Fields[fieldIndex]);
                }

                writer.AddRecord(values);
            }

            writer.Write(output);
            output.Flush();
        }

        File.Move(tempFilePath, document.FilePath, true);
        if (hasMemo)
        {
            var tempMemoFile = Path.ChangeExtension(tempFilePath, ".fpt");
            var targetMemoFile = Path.ChangeExtension(document.FilePath, ".fpt");

            if (File.Exists(tempMemoFile))
            {
                File.Move(tempMemoFile, targetMemoFile, true);
            }
        }
    }


    public IReadOnlyList<DbfFieldDescriptor> DescribeFields(DBFField[] fields)
    {
        return fields.Select(field => new DbfFieldDescriptor
            {
                Name = field.Name,
                Type = field.DataType.ToString(),
                Length = field.FieldLength,
                DecimalCount = field.DecimalCount
            })
            .ToList();
    }

    public bool AreCompatibleStructures(DBFField[] targetFields, DBFField[] sourceFields, out string reason)
    {
        if (targetFields.Length != sourceFields.Length)
        {
            reason = $"la cantidad de campos difiere ({targetFields.Length} vs {sourceFields.Length}).";
            return false;
        }

        for (var index = 0; index < targetFields.Length; index++)
        {
            var target = targetFields[index];
            var source = sourceFields[index];

            if (!string.Equals(target.Name, source.Name, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"el nombre del campo #{index + 1} difiere ({target.Name} vs {source.Name}).";
                return false;
            }

            if (target.DataType != source.DataType)
            {
                reason = $"el tipo del campo '{target.Name}' difiere ({target.DataType} vs {source.DataType}).";
                return false;
            }

            if (target.FieldLength != source.FieldLength)
            {
                reason = $"la longitud del campo '{target.Name}' difiere ({target.FieldLength} vs {source.FieldLength}).";
                return false;
            }

            if (target.DecimalCount != source.DecimalCount)
            {
                reason = $"los decimales del campo '{target.Name}' difieren ({target.DecimalCount} vs {source.DecimalCount}).";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    public void UpdateLanguageDriverByte(string filePath, byte languageDriver)
    {
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        stream.Seek(29, SeekOrigin.Begin);
        stream.WriteByte(languageDriver);
        stream.Flush();
    }

    public void ExportTable(DbfTableDocument document, DataView view, string filePath, string format)
    {
        if (format.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ExportToCsv(view, filePath);
        }
        else if (format.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ExportToExcel(view, filePath);
        }
        else if (format.Equals(".dbf", StringComparison.OrdinalIgnoreCase))
        {
            ExportToDbf(document, view, filePath);
        }
        else
        {
            throw new NotSupportedException($"Formato {format} no soportado.");
        }
    }

    private void ExportToCsv(DataView view, string filePath)
    {
        var sb = new StringBuilder();
        if (view.Table == null) return;
        var columns = view.Table.Columns;

        // Header
        sb.AppendLine(string.Join(";", columns.Cast<DataColumn>().Select(c => c.ColumnName)));

        // Data
        foreach (DataRowView row in view)
        {
            var values = columns.Cast<DataColumn>().Select(c => 
                Convert.ToString(row[c.ColumnName]).Replace(";", " "));
            sb.AppendLine(string.Join(";", values));
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private void ExportToExcel(DataView view, string filePath)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Export");
        if (view.Table == null) return;
        var columns = view.Table.Columns;
        for (var i = 0; i < columns.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = columns[i].ColumnName;
        }

        var rowCount = 0;
        foreach (DataRowView row in view)
        {
            rowCount++;
            for (var i = 0; i < columns.Count; i++)
            {
                worksheet.Cell(rowCount + 1, i + 1).Value = Convert.ToString(row[i]);
            }
        }

        workbook.SaveAs(filePath);
    }

    private void ExportToDbf(DbfTableDocument document, DataView view, string filePath)
    {
        var tempFilePath = filePath + ".tmp";
        
        using (var output = File.Open(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            using var writer = new DBFWriter
            {
                Fields = document.Fields,
                Signature = document.Signature == 0 ? DBFSignature.DBase3 : document.Signature,
                LanguageDriver = document.LanguageDriver
            };

            writer.CharEncoding = Encoding.GetEncoding(850);

            var hasMemo = document.Fields.Any(f => f.DataType == NativeDbType.Memo);
            if (hasMemo)
            {
                writer.DataMemoLoc = Path.ChangeExtension(tempFilePath, ".fpt");
            }

            foreach (DataRowView rowView in view)
            {
                var row = rowView.Row;
                var values = new object[document.Fields.Length];
                for (var fieldIndex = 0; fieldIndex < document.Fields.Length; fieldIndex++)
                {
                    var value = row[fieldIndex];
                    values[fieldIndex] = NormalizeOutgoingValue(value, document.Fields[fieldIndex]);
                }
                writer.AddRecord(values);
            }

            writer.Write(output);
            output.Flush();
        }

        File.Move(tempFilePath, filePath, true);
        if (document.Fields.Any(f => f.DataType == NativeDbType.Memo))
        {
            var tempMemo = Path.ChangeExtension(tempFilePath, ".fpt");
            var targetMemo = Path.ChangeExtension(filePath, ".fpt");
            if (File.Exists(tempMemo))
            {
                File.Move(tempMemo, targetMemo, true);
            }
        }
    }


    public static int? GuessCodePageFromLanguageDriver(byte languageDriver)
    {
        return languageDriver switch
        {
            0x00 => null,
            0x01 => 437,
            0x02 => 850,
            0x03 => 1252,
            0x57 => 1252,
            0x58 => 1250,
            0x59 => 1251,
            0x64 => 852,
            0x65 => 866,
            0x66 => 865,
            0x67 => 861,
            0x6A => 737,
            0x6B => 857,
            0x78 => 950,
            0x79 => 949,
            0x7A => 936,
            0x7B => 932,
            0x7C => 874,
            _ => null
        };
    }

    private static Encoding TryGetEncoding(int codePage)
    {
        try
        {
            return Encoding.GetEncoding(codePage);
        }
        catch
        {
            return Encoding.GetEncoding(1252);
        }
    }

    private static DataTable BuildDataTable(string filePath, IEnumerable<DBFField> fields)
    {
        var table = new DataTable(Path.GetFileName(filePath));

        foreach (var field in fields)
        {
            var column = new DataColumn(field.Name, ResolveClrType(field.DataType))
            {
                AllowDBNull = true
            };

            table.Columns.Add(column);
        }

        return table;
    }

    private static Type ResolveClrType(NativeDbType dataType)
    {
        return dataType switch
        {
            NativeDbType.Logical => typeof(bool),
            NativeDbType.Date => typeof(DateTime),
            NativeDbType.Timestamp => typeof(DateTime),
            NativeDbType.Numeric => typeof(decimal),
            NativeDbType.Float => typeof(double),
            NativeDbType.Double => typeof(double),
            NativeDbType.Long => typeof(int),
            NativeDbType.Autoincrement => typeof(int),
            NativeDbType.Binary => typeof(byte[]),
            _ => typeof(string)
        };
    }

    private static object NormalizeIncomingValue(object? value)
    {
        if (value is null)
        {
            return DBNull.Value;
        }

        if (value is MemoValue memoValue)
        {
            return memoValue.Value;
        }

        return value;
    }

    private static object NormalizeOutgoingValue(object value, DBFField field)
    {
        if (value == DBNull.Value)
        {
            return DBNull.Value;
        }

        return field.DataType switch
        {
            NativeDbType.Char => Convert.ToString(value) ?? string.Empty,
            NativeDbType.Memo => Convert.ToString(value) ?? string.Empty,
            NativeDbType.Numeric => Convert.ToDecimal(value),
            NativeDbType.Float => Convert.ToDouble(value),
            NativeDbType.Double => Convert.ToDouble(value),
            NativeDbType.Long => Convert.ToInt32(value),
            NativeDbType.Autoincrement => Convert.ToInt32(value),
            NativeDbType.Logical => Convert.ToBoolean(value),
            NativeDbType.Date => value is DateTime date ? date.Date : Convert.ToDateTime(value).Date,
            NativeDbType.Timestamp => value is DateTime timestamp ? timestamp : Convert.ToDateTime(value),
            NativeDbType.Binary => value,
            _ => value
        };
    }

    private static byte ReadHeaderByte(string filePath, long offset)
    {
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(offset, SeekOrigin.Begin);
        var read = stream.ReadByte();
        return read < 0 ? (byte)0 : (byte)read;
    }

    private static DbfHeaderInfo ReadHeaderInfo(
        string filePath,
        int fieldCount,
        int recordCount,
        byte signature,
        byte languageDriver)
    {
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(stream);

        stream.Seek(1, SeekOrigin.Begin);
        var year = reader.ReadByte() + 1900;
        var month = reader.ReadByte();
        var day = reader.ReadByte();

        stream.Seek(8, SeekOrigin.Begin);
        var headerSize = reader.ReadUInt16();
        var recordSize = reader.ReadUInt16();

        DateTime? lastUpdated = null;
        if (month is >= 1 and <= 12 && day is >= 1 and <= 31)
        {
            try
            {
                lastUpdated = new DateTime(year, month, day);
            }
            catch
            {
                lastUpdated = null;
            }
        }

        return new DbfHeaderInfo
        {
            RecordCount = recordCount,
            FieldCount = fieldCount,
            HeaderSize = headerSize,
            RecordSize = recordSize,
            LastUpdated = lastUpdated,
            LanguageCodeDescription = DescribeLanguageDriver(languageDriver),
            FileTypeDescription = DescribeSignature(signature),
            FullPath = filePath
        };
    }

    private static string DescribeSignature(byte signature)
    {
        return signature switch
        {
            0x02 => "FoxBASE",
            0x03 => "dBASE III",
            0x30 => "Visual FoxPro",
            0x31 => "Visual FoxPro (autoincremento)",
            0x32 => "Visual FoxPro (varchar/varbinary)",
            0x43 => "dBASE IV SQL",
            0x63 => "dBASE IV con memo",
            0x83 => "dBASE III con memo",
            0x8B => "dBASE IV con memo",
            0xCB => "dBASE IV SQL con memo",
            0xF5 => "FoxPro con memo",
            _ => $"Desconocido (0x{signature:X2})"
        };
    }

    private static string DescribeLanguageDriver(byte languageDriver)
    {
        return languageDriver switch
        {
            0x00 => "Sin codepage definido (0x00)",
            0x01 => "DOS 437 USA (0x01)",
            0x02 => "DOS 850 Multilenguaje (0x02)",
            0x03 => "Windows ANSI 1252 (0x03)",
            0x57 => "Windows ANSI 1252 (0x57)",
            0x58 => "Windows ANSI 1250 (0x58)",
            0x59 => "Windows ANSI 1251 (0x59)",
            0x64 => "DOS 852 Europa Central (0x64)",
            0x65 => "DOS 866 Ruso (0x65)",
            0x66 => "DOS 865 Nórdico (0x66)",
            0x67 => "DOS 861 Islandés (0x67)",
            0x6A => "DOS 737 Griego (0x6A)",
            0x6B => "DOS 857 Turco (0x6B)",
            0x78 => "Windows ANSI 950 Chino Tradicional (0x78)",
            0x79 => "Windows ANSI 949 Coreano (0x79)",
            0x7A => "Windows ANSI 936 Chino Simplificado (0x7A)",
            0x7B => "Windows ANSI 932 Japonés (0x7B)",
            0x7C => "Windows ANSI 874 Tailandés (0x7C)",
            _ => $"Codepage no mapeado (0x{languageDriver:X2})"
        };
    }
}
