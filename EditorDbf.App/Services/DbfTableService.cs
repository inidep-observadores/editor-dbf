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

    public DbfTableDocument LoadTable(string filePath)
    {
        using var reader = new DBFReader(filePath);
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

        return new DbfTableDocument
        {
            FilePath = filePath,
            Signature = signature,
            LanguageDriver = languageDriver,
            Encoding = reader.CharEncoding ?? CultureInfo.CurrentCulture.TextInfo.ANSICodePage switch
            {
                > 0 and var codePage => Encoding.GetEncoding(codePage),
                _ => Encoding.GetEncoding(1252)
            },
            Fields = fields,
            DataTable = table
        };
    }

    public void SaveTable(DbfTableDocument document)
    {
        var tempFilePath = Path.Combine(
            Path.GetDirectoryName(document.FilePath)!,
            $"{Path.GetFileNameWithoutExtension(document.FilePath)}.tmp{Path.GetExtension(document.FilePath)}");

        using var output = File.Open(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var writer = new DBFWriter
        {
            Fields = document.Fields,
            Signature = document.Signature == 0 ? DBFSignature.DBase3 : document.Signature,
            LanguageDriver = document.LanguageDriver
        };

        writer.CharEncoding = document.Encoding;

        var hasMemo = document.Fields.Any(f => f.DataType == NativeDbType.Memo);
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

        File.Copy(tempFilePath, document.FilePath, true);
        if (hasMemo)
        {
            var tempMemoFile = Path.ChangeExtension(tempFilePath, ".fpt");
            var targetMemoFile = Path.ChangeExtension(document.FilePath, ".fpt");

            if (File.Exists(tempMemoFile))
            {
                File.Copy(tempMemoFile, targetMemoFile, true);
                File.Delete(tempMemoFile);
            }
        }

        File.Delete(tempFilePath);
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
}
