using System;
using System.Collections.Generic;
using System.Data;
using EditorDbf.App.Infrastructure;
using EditorDbf.App.Models;
using EditorDbf.App.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;
using DotNetDBF;

namespace EditorDbf.Tests.ViewModels;

public class TableTabViewModelTests
{
    static TableTabViewModelTests()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    private IDialogService _dialogService = null!;
    private DbfTableDocument _document = null!;
    private TableTabViewModel _viewModel = null!;

    private void Setup()
    {
        _dialogService = Substitute.For<IDialogService>();

        var dt = new DataTable();
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Age", typeof(int));
        dt.Columns.Add("Salary", typeof(decimal));
        dt.Columns.Add("HireDate", typeof(DateTime));

        dt.Rows.Add("John", 30, 50000m, new DateTime(2020, 1, 15));

        var headerInfo = new DbfHeaderInfo
        {
            RecordCount = 1,
            FieldCount = 4,
            HeaderSize = 65,
            RecordSize = 50,
            LastUpdated = DateTime.Now,
            LanguageCodeDescription = "Test",
            FileTypeDescription = "DBF",
            FullPath = "/test.dbf"
        };

        _document = new DbfTableDocument
        {
            FilePath = "/test.dbf",
            Signature = 0x03,
            LanguageDriver = 0x03,
            Encoding = System.Text.Encoding.GetEncoding(1252),
            EffectiveCodePage = 1252,
            Fields = [],
            DataTable = dt,
            HeaderInfo = headerInfo
        };

        var structure = new List<DbfFieldDescriptor>
        {
            new() { Name = "Name", Type = "Char", Length = 50, DecimalCount = 0 },
            new() { Name = "Age", Type = "Numeric", Length = 3, DecimalCount = 0 },
            new() { Name = "Salary", Type = "Numeric", Length = 10, DecimalCount = 2 },
            new() { Name = "HireDate", Type = "Date", Length = 8, DecimalCount = 0 }
        };

        _viewModel = new TableTabViewModel(_document, structure, vm => { }, _dialogService);
    }

    [Fact]
    public void Constructor_SetsDocumentAndStructure()
    {
        Setup();
        _viewModel.FilePath.Should().Be("/test.dbf");
        _viewModel.FileName.Should().Be("test.dbf");
    }

    [Fact]
    public void Header_WithoutChanges_DoesNotIncludeAsterisk()
    {
        Setup();
        _viewModel.Header.Should().Be("test.dbf");
    }

    [Fact]
    public void Header_WithChanges_IncludesAsterisk()
    {
        Setup();
        _viewModel.AddRecord();
        _viewModel.Header.Should().Be("test.dbf *");
    }

    [Fact]
    public void AddRecord_IncreasesRecordCountAndMarksChanged()
    {
        Setup();
        var initialCount = _viewModel.CurrentTableView.Count;

        _viewModel.AddRecord();

        _viewModel.CurrentTableView.Count.Should().Be(initialCount + 1);
        _viewModel.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public void MarkSaved_ClearsHasPendingChanges()
    {
        Setup();
        _viewModel.AddRecord();
        _viewModel.HasPendingChanges.Should().BeTrue();

        _viewModel.MarkSaved();

        _viewModel.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public void BuildFilterExpression_StringEqual_GeneratesCorrectSQL()
    {
        Setup();
        var col = _document.DataTable.Columns["Name"];
        var expr = TableTabViewModel.BuildFilterExpression(col, "=", "John");

        expr.Should().Be("[Name] = 'John'");
    }

    [Fact]
    public void BuildFilterExpression_StringContains_GeneratesLikeSyntax()
    {
        Setup();
        var col = _document.DataTable.Columns["Name"];
        var expr = TableTabViewModel.BuildFilterExpression(col, "CONTIENE", "oh");

        expr.Should().Contain("LIKE");
        expr.Should().Contain("'%oh%'");
    }

    [Fact]
    public void BuildFilterExpression_NumericComparison_GeneratesCorrectLiteral()
    {
        Setup();
        var col = _document.DataTable.Columns["Age"];
        var expr = TableTabViewModel.BuildFilterExpression(col, ">", "25");

        expr.Should().Be("[Age] > 25");
    }

    [Fact]
    public void BuildFilterExpression_BoolTrue_GeneratesBoolean()
    {
        Setup();
        var col = _document.DataTable.Columns["Name"];
        var boolCol = _document.DataTable.Columns.Add("Active", typeof(bool));

        var expr = TableTabViewModel.BuildFilterExpression(boolCol, "=", "T");

        expr.Should().Contain("TRUE");
    }

    [Fact]
    public void BuildFilterExpression_StringEmpty_GeneratesISNULL()
    {
        Setup();
        var col = _document.DataTable.Columns["Name"];
        var expr = TableTabViewModel.BuildFilterExpression(col, "VACIO", "");

        expr.Should().Contain("IS NULL");
    }

    [Fact]
    public void BuildFilterExpression_StringNotEmpty_GeneratesISNOTNULL()
    {
        Setup();
        var col = _document.DataTable.Columns["Name"];
        var expr = TableTabViewModel.BuildFilterExpression(col, "NO VACIO", "");

        expr.Should().Contain("IS NOT NULL");
    }

    [Fact]
    public void DeleteSelectedRecords_WithoutSelection_ReturnsZero()
    {
        Setup();
        var deleted = _viewModel.DeleteSelectedRecords();
        deleted.Should().Be(0);
    }

    [Fact]
    public void AppendRowsFrom_AppendsValidRows()
    {
        Setup();
        var sourceTable = new DataTable();
        sourceTable.Columns.Add("Name", typeof(string));
        sourceTable.Columns.Add("Age", typeof(int));
        sourceTable.Columns.Add("Salary", typeof(decimal));
        sourceTable.Columns.Add("HireDate", typeof(DateTime));
        sourceTable.Rows.Add("Jane", 28, 55000m, new DateTime(2021, 5, 10));

        var initialCount = _viewModel.CurrentTableView.Count;
        _viewModel.AppendRowsFrom(sourceTable);

        _viewModel.CurrentTableView.Count.Should().Be(initialCount + 1);
    }

    [Fact]
    public void IsFilterActive_ReflectsRowFilterState()
    {
        Setup();
        _viewModel.IsFilterActive.Should().BeFalse();

        _viewModel.CurrentTableView.RowFilter = "[Name] = 'John'";

        _viewModel.IsFilterActive.Should().BeTrue();
    }

    [Fact]
    public void TotalRecords_ReturnsRowCount()
    {
        Setup();
        _viewModel.TotalRecords.Should().Be(1);
    }

    [Fact]
    public void FilteredRecords_ReflectsViewCount()
    {
        Setup();
        _viewModel.FilteredRecords.Should().Be(1);

        _viewModel.CurrentTableView.RowFilter = "[Name] = 'NotExists'";

        _viewModel.FilteredRecords.Should().Be(0);
    }
}
