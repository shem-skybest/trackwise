using Trackwise.Application.Modules.MasterData.Common.Import;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// The parser exists to answer one question the import report cannot do without: which line of the
/// steward's own file is this row? These pin that answer against the cases that break it - quoted
/// values that span lines, blank separator lines, and the separator Excel picked without asking.
/// </summary>
public class DelimitedTableTests
{
    [Fact]
    public void ReadsHeaderKeyedRows()
    {
        var (_, rows, error) = DelimitedTable.Read("Code,Name\nPHMNL,Manila\n");

        Assert.Null(error);
        var row = Assert.Single(rows);
        Assert.Equal("PHMNL", row["Code"]);
        Assert.Equal("Manila", row["Name"]);
    }

    [Theory]
    [InlineData("Country Code")]
    [InlineData("country_code")]
    [InlineData("COUNTRYCODE")]
    [InlineData("Country-Code")]
    public void HeaderSpellingDoesNotMatter(string header)
    {
        // Files arrive from six systems and a decade of spreadsheets. Insisting on one spelling
        // would fail the first import for a reason the steward cannot see in their own file.
        var (_, rows, _) = DelimitedTable.Read($"{header}\nPH\n");

        Assert.Equal("PH", rows[0]["CountryCode"]);
    }

    [Theory]
    [InlineData('\t')]
    [InlineData(';')]
    [InlineData('|')]
    public void DelimiterIsSniffedFromTheHeader(char delimiter)
    {
        var (_, rows, _) = DelimitedTable.Read($"Code{delimiter}Name\nPHMNL{delimiter}Manila\n");

        Assert.Equal("Manila", rows[0]["Name"]);
    }

    [Fact]
    public void QuotedFieldKeepsItsCommasAndEscapedQuotes()
    {
        var (_, rows, _) = DelimitedTable.Read("Code,Name\nPHMNL,\"Manila, Pier 3 \"\"North\"\"\"\n");

        Assert.Equal("Manila, Pier 3 \"North\"", rows[0]["Name"]);
    }

    [Fact]
    public void RowAfterAMultiLineValueKeepsTheRightLineNumber()
    {
        // The whole reason the parser is hand-rolled. If the embedded newline is not counted, every
        // issue reported after it points the steward at the wrong row of their spreadsheet.
        var (_, rows, _) = DelimitedTable.Read(
            "Code,Name\nPHMNL,\"Manila\nSouth Harbour\"\nPHCEB,Cebu\n");

        Assert.Equal(2, rows[0].Line);
        Assert.Equal("Manila\nSouth Harbour", rows[0]["Name"]);
        Assert.Equal(4, rows[1].Line);
    }

    [Fact]
    public void BlankLinesAreNotRowsButStillAdvanceTheCount()
    {
        var (_, rows, _) = DelimitedTable.Read("Code,Name\n\nPHMNL,Manila\n\n");

        var row = Assert.Single(rows);
        Assert.Equal(3, row.Line);
    }

    [Fact]
    public void PartiallyFilledRowIsStillARow()
    {
        // Dropping it would hide the error rather than report it: a row with only a code is a row
        // the steward meant to send and needs told about.
        var (_, rows, _) = DelimitedTable.Read("Code,Name\nPHMNL,\n");

        Assert.Single(rows);
        Assert.Null(rows[0]["Name"]);
    }

    [Fact]
    public void ShortRowReadsMissingColumnsAsAbsent()
    {
        var (_, rows, _) = DelimitedTable.Read("Code,Name,Country\nPHMNL,Manila\n");

        Assert.Null(rows[0]["Country"]);
    }

    [Fact]
    public void ByteOrderMarkDoesNotBecomePartOfTheFirstHeader()
    {
        // Excel writes one on every UTF-8 save, so without this the first column is never found.
        var (_, rows, _) = DelimitedTable.Read("﻿Code,Name\nPHMNL,Manila\n");

        Assert.Equal("PHMNL", rows[0]["Code"]);
    }

    [Fact]
    public void ValuesAreTrimmed()
    {
        var (_, rows, _) = DelimitedTable.Read("Code,Name\n  PHMNL , Manila \n");

        Assert.Equal("PHMNL", rows[0]["Code"]);
        Assert.Equal("Manila", rows[0]["Name"]);
    }

    [Fact]
    public void FileWithoutATrailingNewlineStillYieldsItsLastRow()
    {
        var (_, rows, _) = DelimitedTable.Read("Code,Name\nPHMNL,Manila");

        Assert.Single(rows);
    }

    [Fact]
    public void DuplicateHeaderDoesNotThrow()
    {
        var (_, rows, error) = DelimitedTable.Read("Name,Name\nManila,Cebu\n");

        Assert.Null(error);
        Assert.Equal("Manila", rows[0]["Name"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyFileIsRejectedWithAReason(string content)
    {
        var (_, _, error) = DelimitedTable.Read(content);

        Assert.NotNull(error);
    }
}
