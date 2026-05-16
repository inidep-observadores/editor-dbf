using System;
using System.IO;
using EditorDbf.App.Services;
using FluentAssertions;
using Xunit;
using DotNetDBF;

namespace EditorDbf.Tests.Services;

public class DbfTableServiceTests
{
    private readonly DbfTableService _service = new();

    [Fact]
    public void ListDbfFiles_NonexistentDirectory_ReturnsEmptyList()
    {
        var result = _service.ListDbfFiles("/nonexistent/path");
        result.Should().BeEmpty();
    }

    [Fact]
    public void ListDbfFiles_EmptyDirectory_ReturnsEmptyList()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);

        try
        {
            var result = _service.ListDbfFiles(tmpDir);
            result.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tmpDir);
        }
    }

    [Fact]
    public void ListDbfFiles_WithDbfFiles_ReturnsFileNamesOnly()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);

        try
        {
            File.Create(Path.Combine(tmpDir, "test1.dbf")).Close();
            File.Create(Path.Combine(tmpDir, "test2.DBF")).Close();
            File.Create(Path.Combine(tmpDir, "other.txt")).Close();

            var result = _service.ListDbfFiles(tmpDir);

            result.Should().HaveCount(2);
            result.Should().Contain(f => f.Equals("test1.dbf", StringComparison.OrdinalIgnoreCase));
            result.Should().Contain(f => f.Equals("test2.DBF", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void AreCompatibleStructures_DifferentLengths_ReturnsFalse()
    {
        var field1 = new DBFField("Name", NativeDbType.Char, 50, 0);
        var fields1 = new[] { field1 };

        var field2 = new DBFField("Name", NativeDbType.Char, 50, 0);
        var field3 = new DBFField("Age", NativeDbType.Numeric, 3, 0);
        var fields2 = new[] { field2, field3 };

        var result = _service.AreCompatibleStructures(fields1, fields2, out var reason);

        result.Should().BeFalse();
        reason.Should().NotBeEmpty();
    }

    [Fact]
    public void AreCompatibleStructures_SameStructure_ReturnsTrue()
    {
        var field1 = new DBFField("Name", NativeDbType.Char, 50, 0);
        var field2 = new DBFField("Age", NativeDbType.Numeric, 3, 0);
        var fields1 = new[] { field1, field2 };
        var fields2 = new[] { field1, field2 };

        var result = _service.AreCompatibleStructures(fields1, fields2, out var reason);

        result.Should().BeTrue();
        reason.Should().BeEmpty();
    }

    [Fact]
    public void AreCompatibleStructures_DifferentName_ReturnsFalse()
    {
        var field1 = new DBFField("Name", NativeDbType.Char, 50, 0);
        var field2 = new DBFField("Nombre", NativeDbType.Char, 50, 0);

        var result = _service.AreCompatibleStructures(new[] { field1 }, new[] { field2 }, out var reason);

        result.Should().BeFalse();
    }

    [Fact]
    public void AreCompatibleStructures_NameCaseInsensitive_ReturnsTrue()
    {
        var field1 = new DBFField("Name", NativeDbType.Char, 50, 0);
        var field2 = new DBFField("NAME", NativeDbType.Char, 50, 0);

        var result = _service.AreCompatibleStructures(new[] { field1 }, new[] { field2 }, out var reason);

        result.Should().BeTrue();
    }

    [Fact]
    public void GuessCodePageFromLanguageDriver_0x00_ReturnsNull()
    {
        var result = DbfTableService.GuessCodePageFromLanguageDriver(0x00);
        result.Should().BeNull();
    }

    [Fact]
    public void GuessCodePageFromLanguageDriver_0x03_Returns1252()
    {
        var result = DbfTableService.GuessCodePageFromLanguageDriver(0x03);
        result.Should().Be(1252);
    }

    [Fact]
    public void GuessCodePageFromLanguageDriver_0x02_Returns850()
    {
        var result = DbfTableService.GuessCodePageFromLanguageDriver(0x02);
        result.Should().Be(850);
    }

    [Fact]
    public void GuessCodePageFromLanguageDriver_UnknownByte_ReturnsNull()
    {
        var result = DbfTableService.GuessCodePageFromLanguageDriver(0xFF);
        result.Should().BeNull();
    }

    [Fact]
    public void DescribeFields_ProjectsFieldsCorrectly()
    {
        var fields = new[]
        {
            new DBFField("Name", NativeDbType.Char, 50, 0),
            new DBFField("Age", NativeDbType.Numeric, 3, 0),
            new DBFField("Salary", NativeDbType.Numeric, 10, 2)
        };

        var result = _service.DescribeFields(fields);

        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Name");
        result[0].Length.Should().Be(50);
        result[1].Name.Should().Be("Age");
        result[2].DecimalCount.Should().Be(2);
    }
}
